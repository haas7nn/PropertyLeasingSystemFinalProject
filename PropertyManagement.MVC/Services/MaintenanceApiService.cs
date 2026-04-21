using PropertyManagement.MVC.Models;
using System.Text.Json;

namespace PropertyManagement.MVC.Services
{
    public class MaintenanceApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MaintenanceApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<MaintenanceLookupDto?> LookupMaintenanceRequest(string ticketNumber, string phoneNumber)
        {
            var client = _httpClientFactory.CreateClient();
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            var url = $"{apiBaseUrl}/api/Maintenance/lookup?ticketNumber={ticketNumber}&phoneNumber={phoneNumber}";

            try
            {
                var response = await client.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MaintenanceLookupDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (HttpRequestException)
            {
                // API is not reachable
                return null;
            }
        }
    }
}