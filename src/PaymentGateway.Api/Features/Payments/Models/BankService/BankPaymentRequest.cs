using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Features.Payments.Models.BankService
{
    public record BankPaymentRequest
    {

        [JsonPropertyName("card_number")]
        public string CardNumber { get; set; }

        [JsonPropertyName("expiry_date")]
        public string ExpiryDate { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("cvv")]
        public string Cvv { get; set; }

        public BankPaymentRequest(string cardNumber, int expiryMonth, int expiryYear, string currency, int amount, string cvv)
        {
            CardNumber = cardNumber;
            ExpiryDate = $"{expiryMonth:00}/{expiryYear}";
            Currency = currency;
            Amount = amount;
            Cvv = cvv;
        }
    }
}
