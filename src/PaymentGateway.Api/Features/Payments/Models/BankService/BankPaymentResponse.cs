using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Features.Payments.Models.BankService
{
    public record BankPaymentResponse
    {
        [JsonPropertyName("authorized")]
        public bool Authorized { get; set; }

        [JsonPropertyName("authorization_code")]
        public string AuthorizationCode { get; set; }
    }
}
