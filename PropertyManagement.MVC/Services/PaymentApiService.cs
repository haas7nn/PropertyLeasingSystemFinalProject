using PropertyManagement.MVC.Models;
using System.Net.Http.Json;

namespace PropertyManagement.MVC.Services
{
    public class PaymentApiService
    {
        private readonly HttpClient _http;
        public PaymentApiService(HttpClient http) => _http = http;

        public async Task<List<PaymentViewModel>> GetAllAsync(string? status = null)
        {
            var url = string.IsNullOrEmpty(status) ? "api/payments" : $"api/payments?status={status}";
            return await _http.GetFromJsonAsync<List<PaymentViewModel>>(url) ?? new();
        }

        public async Task<List<PaymentViewModel>> GetOverdueAsync() =>
            await _http.GetFromJsonAsync<List<PaymentViewModel>>("api/payments/overdue") ?? new();

        public async Task<PaymentViewModel?> GetByIdAsync(int id) =>
            await _http.GetFromJsonAsync<PaymentViewModel>($"api/payments/{id}");

        public async Task<bool> CreateAsync(PaymentViewModel model)
        {
            // Matches CreatePaymentDto in API
            var response = await _http.PostAsJsonAsync("api/payments", new
            {
                model.LeaseId,
                model.DueDate,
                model.AmountDue
            });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RecordPaymentAsync(int id, RecordPaymentRequest request)
        {
            var response = await _http.PostAsJsonAsync($"api/payments/{id}/record", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/payments/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}