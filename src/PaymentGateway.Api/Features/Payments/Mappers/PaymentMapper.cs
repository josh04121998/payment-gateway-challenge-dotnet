using PaymentGateway.Api.Features.Payments.Enums;
using PaymentGateway.Api.Features.Payments.Models.BankService;
using PaymentGateway.Api.Features.Payments.Models.Requests;
using PaymentGateway.Api.Features.Payments.Models.Responses;

namespace PaymentGateway.Api.Features.Payments.Mappers
{
    public interface IPaymentMapper
    {
        BankPaymentRequest ToBankPaymentRequest(PostPaymentRequest request);
        PaymentResponse ToPaymentResponse(PostPaymentRequest request, BankPaymentResponse bankResponse);
    }

    public class PaymentMapper : IPaymentMapper
    {
        public BankPaymentRequest ToBankPaymentRequest(PostPaymentRequest request)
        {
            return new BankPaymentRequest(
                cardNumber: request.CardNumber,
                expiryMonth: request.ExpiryMonth,
                expiryYear: request.ExpiryYear,
                currency: request.Currency,
                amount: request.Amount,
                cvv: request.Cvv);
        }

        public PaymentResponse ToPaymentResponse(PostPaymentRequest request, BankPaymentResponse bankResponse)
        {
            return new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                CardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4, 4),
                Currency = request.Currency,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined
            };
        }
    }
}