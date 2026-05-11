using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PropertyManagement.Reporting.Models;

namespace PropertyManagement.Reporting.Services
{
    /// <summary>
    /// Handles all communication with the Web API.
    /// Authenticates via JWT (POST /api/Auth/login) and attaches Bearer token to every request.
    /// No direct database access — purely API-driven, as required by the rubric.
    /// </summary>
    public class ApiService : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Token cache: instance-level (ApiService is Scoped) with a SemaphoreSlim for thread safety
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ApiService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory    = httpClientFactory;
            _configuration        = configuration;
            _logger               = logger;
            _httpContextAccessor  = httpContextAccessor;
        }

        private string BaseUrl => _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7168";

        // ── JWT Authentication ──────────────────────────────────────────────

        /// <summary>
        /// Called by the Login action to validate credentials against the API.
        /// Returns the JWT token + roles on success so the controller can gate the session.
        /// </summary>
        public async Task<LoginAttemptResult> TryLoginAsync(string email, string password)
        {
            try
            {
                var client   = _httpClientFactory.CreateClient();
                var payload  = new { Email = email, Password = password };
                var response = await client.PostAsync(
                    $"{BaseUrl}/api/Auth/login",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return new LoginAttemptResult { Success = false, ErrorMessage = "Invalid email or password." };

                var json          = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, JsonOptions);

                if (loginResponse?.Token == null)
                    return new LoginAttemptResult { Success = false, ErrorMessage = "API did not return a token." };

                // Cache for auto-refresh on subsequent report requests
                _cachedToken = loginResponse.Token;
                _tokenExpiry = loginResponse.Expiration.AddMinutes(-2);

                return new LoginAttemptResult
                {
                    Success = true,
                    Token   = loginResponse.Token,
                    Roles   = loginResponse.Roles ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API login");
                return new LoginAttemptResult { Success = false, ErrorMessage = "Could not reach the reporting API." };
            }
        }

        /// <summary>
        /// Internal: gets a valid JWT token, either from cache, session, or by re-authenticating.
        /// </summary>
        private async Task<string?> GetTokenAsync()
        {
            // Fast path: cached token still valid
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
                return _cachedToken;

            // Mid path: session has a token from the login step
            var sessionToken = _httpContextAccessor.HttpContext?.Session.GetString("ApiToken");
            if (!string.IsNullOrEmpty(sessionToken))
            {
                _cachedToken = sessionToken;
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55); // assume ~60 min token
                return _cachedToken;
            }

            // Slow path: re-authenticate using configured credentials
            await _tokenLock.WaitAsync();
            try
            {
                if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry) return _cachedToken;

                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    Email    = _configuration["ApiSettings:ManagerEmail"],
                    Password = _configuration["ApiSettings:ManagerPassword"]
                };

                var response = await client.PostAsync(
                    $"{BaseUrl}/api/Auth/login",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("JWT re-authentication failed: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json          = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, JsonOptions);
                _cachedToken      = loginResponse?.Token;
                _tokenExpiry      = loginResponse?.Expiration.AddMinutes(-2) ?? DateTime.MinValue;
                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during JWT re-authentication");
                return null;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // ── HTTP Helper ─────────────────────────────────────────────────────

        private async Task<T?> GetAsync<T>(string endpoint)
        {
            var token = await GetTokenAsync();
            if (token == null) return default;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync($"{BaseUrl}{endpoint}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API call failed: {Endpoint} → {StatusCode}", endpoint, response.StatusCode);
                    return default;
                }
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API endpoint: {Endpoint}", endpoint);
                return default;
            }
        }

        // ── Report endpoints ────────────────────────────────────────────────

        public async Task<OccupancyReport?> GetOccupancyReportAsync()
            => await GetAsync<OccupancyReport>("/api/Reports/occupancy");

        public async Task<MaintenanceStatsReport?> GetMaintenanceStatsAsync()
            => await GetAsync<MaintenanceStatsReport>("/api/Reports/maintenance-stats");

        public async Task<PaymentSummaryReport?> GetPaymentSummaryAsync()
            => await GetAsync<PaymentSummaryReport>("/api/Reports/payment-summary");

        public async Task<List<BuildingReportItem>?> GetBuildingsAsync()
            => await GetAsync<List<BuildingReportItem>>("/api/Buildings");

        public async Task<bool> IsApiReachableAsync()
        {
            var token = await GetTokenAsync();
            return token != null;
        }

        public void Dispose() => _tokenLock.Dispose();
    }

    /// <summary>Result of a TryLoginAsync call from the controller's Login action.</summary>
    public class LoginAttemptResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
