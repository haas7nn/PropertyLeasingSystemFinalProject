using PropertyManagement.MVC.Models;
using System.Net.Http.Json;

namespace PropertyManagement.MVC.Services
{
    public class LeaseApiService
    {
        private readonly HttpClient _http;
        public LeaseApiService(HttpClient http) => _http = http;

        public async Task<List<LeaseViewModel>> GetAllAsync(string? status = null) =>
            await _http.GetFromJsonAsync<List<LeaseViewModel>>($"api/leases?status={status}") ?? new();

        public async Task<LeaseViewModel?> GetByIdAsync(int id) =>
            await _http.GetFromJsonAsync<LeaseViewModel>($"api/leases/{id}");

        public async Task<bool> CreateAsync(LeaseViewModel model)
        {
            var createDto = new
            {
                UnitId = model.UnitId,
                TenantId = model.TenantId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MonthlyRent = model.MonthlyRent,
                SecurityDeposit = model.SecurityDeposit,
                ScreeningNotes = model.ScreeningNotes
            };

            var response = await _http.PostAsJsonAsync("api/leases", createDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(int id, LeaseViewModel model)
        {
            var updateDto = new
            {
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MonthlyRent = model.MonthlyRent,
                SecurityDeposit = model.SecurityDeposit,
                Status = model.Status,
                RejectionReason = model.RejectionReason,
                ScreeningNotes = model.ScreeningNotes
            };

            var response = await _http.PutAsJsonAsync($"api/leases/{id}", updateDto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error: {error}");
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ApproveAsync(int id)
        {
            var response = await _http.PutAsync($"api/leases/{id}/approve", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> TerminateAsync(int id, string reason)
        {
            var response = await _http.PutAsJsonAsync($"api/leases/{id}/terminate", new { Reason = reason });
            return response.IsSuccessStatusCode;
        }

        
    }
}