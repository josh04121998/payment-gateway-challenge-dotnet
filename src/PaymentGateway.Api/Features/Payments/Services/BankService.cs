using System.Text.Json;

using PaymentGateway.Api.Features.Payments.Models.BankService;

namespace PaymentGateway.Api.Features.Payments.Services
{
    public interface IAcquiringBankService
    {
        Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request);
    }

    public class BankService : IAcquiringBankService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BankService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            using var httpClient = _httpClientFactory.CreateClient("BankApi");

            var response = await httpClient.PostAsJsonAsync("/payments", request);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Bank API returned unsuccessful status: {response.StatusCode}");
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
            if (bankResponse == null)
            {
                throw new JsonException("Failed to deserialize bank response: response content is null or invalid.");
            }

            return bankResponse;
        }
    }
}