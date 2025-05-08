using PaymentGateway.Api.Features.Payments.Enums;

namespace PaymentGateway.Api.Features.Payments.Models.Responses;

public record PaymentResponse
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public string CardNumberLastFour { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }
}
