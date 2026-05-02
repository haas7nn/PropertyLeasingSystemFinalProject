using PropertyManagement.MVC.Models;
using System.Text;
using System.Text.Json;

namespace PropertyManagement.MVC.Services
{
    public class PropertyApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PropertyApiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private string BaseUrl => _configuration["ApiSettings:BaseUrl"];



        public async Task<List<BuildingViewModel>> GetBuildingsAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"{BaseUrl}/api/Buildings");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<BuildingViewModel>>(json, _jsonOptions)
                   ?? new List<BuildingViewModel>();
        }

        public async Task<BuildingViewModel?> GetBuildingByIdAsync(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"{BaseUrl}/api/Buildings/{id}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<BuildingViewModel>(json, _jsonOptions);
        }

        public async Task<bool> CreateBuildingAsync(BuildingViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl}/api/Buildings", content);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateBuildingAsync(int id, BuildingViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{BaseUrl}/api/Buildings/{id}", content);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteBuildingAsync(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.DeleteAsync($"{BaseUrl}/api/Buildings/{id}");

            return response.IsSuccessStatusCode;
        }

        //Units

        // GET ALL UNITS
        public async Task<List<UnitViewModel>> GetUnitsAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"{BaseUrl}/api/Units");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<List<UnitViewModel>>(json, _jsonOptions)
                   ?? new List<UnitViewModel>();
        }

        // GET UNIT BY ID
        public async Task<UnitViewModel?> GetUnitByIdAsync(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"{BaseUrl}/api/Units/{id}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<UnitViewModel>(json, _jsonOptions);
        }

        // CREATE UNIT
        public async Task<bool> CreateUnitAsync(UnitViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl}/api/Units", content);

            return response.IsSuccessStatusCode;
        }

        // UPDATE UNIT
        public async Task<bool> UpdateUnitAsync(int id, UnitViewModel model)
        {
            var client = _httpClientFactory.CreateClient();

            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{BaseUrl}/api/Units/{id}", content);

            return response.IsSuccessStatusCode;
        }

        // DELETE UNIT
        public async Task<bool> DeleteUnitAsync(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.DeleteAsync($"{BaseUrl}/api/Units/{id}");

            return response.IsSuccessStatusCode;
        }

        public Task<List<object>> GetTenantsAsync()
        {
            return Task.FromResult(new List<object>());
        }

    }
}