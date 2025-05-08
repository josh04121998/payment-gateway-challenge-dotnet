using PaymentGateway.Api.Features.Payments.Enums;

namespace PaymentGateway.Api.Features.Payments.Models.Responses
{
    public record PostPaymentFailedValidationResponse
    {
        public PaymentStatus Status => PaymentStatus.Rejected;
        public ValidationError[] Errors { get; set; }
    }

    public record ValidationError
    {
        public string PropertyName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
