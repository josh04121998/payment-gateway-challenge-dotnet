using System.Text.Json;

using PaymentGateway.Api.Features.Payments.Mappers;
using PaymentGateway.Api.Features.Payments.Models.Requests;
using PaymentGateway.Api.Features.Payments.Models.Responses;
using PaymentGateway.Api.Features.Payments.Services;

namespace PaymentGateway.Api.Features.Payments.Handler
{
    public class PaymentHandler
    {
        private readonly IAcquiringBankService _acquiringBankService;
        private readonly IPaymentsRepository _paymentsRepository;
        private readonly IPaymentMapper _paymentMapper;
        private readonly ILogger<PaymentHandler> _logger;

        public PaymentHandler(
            IAcquiringBankService acquiringBankService,
            IPaymentsRepository paymentsRepository,
            IPaymentMapper paymentMapper,
            ILogger<PaymentHandler> logger)
        {
            _acquiringBankService = acquiringBankService ?? throw new ArgumentNullException(nameof(acquiringBankService));
            _paymentsRepository = paymentsRepository ?? throw new ArgumentNullException(nameof(paymentsRepository));
            _paymentMapper = paymentMapper ?? throw new ArgumentNullException(nameof(paymentMapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PostPaymentRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Generate a correlation ID for this request
            var correlationId = Guid.NewGuid().ToString();
            var cardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4);
            var amount = request.Amount;
            var currency = request.Currency;

            var bankRequest = _paymentMapper.ToBankPaymentRequest(request);

            try
            {
                var bankResponse = await _acquiringBankService.ProcessPaymentAsync(bankRequest);

                if (bankResponse == null)
                {
                    throw new PaymentProcessingException("Bank service returned an invalid response.");
                }

                var paymentResponse = _paymentMapper.ToPaymentResponse(request, bankResponse);
                _paymentsRepository.Add(paymentResponse);

                _logger.LogInformation("Payment processed successfully, CorrelationId: {CorrelationId}, PaymentId: {PaymentId}, Status: {Status}, CardNumberLastFour: {CardNumberLastFour}, Amount: {Amount} {Currency}",
                    correlationId, paymentResponse.Id, paymentResponse.Status, cardNumberLastFour, amount, currency);

                return paymentResponse;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or PaymentProcessingException)
            {
                _logger.LogError(ex, "Failed to process payment, CorrelationId: {CorrelationId}, CardNumberLastFour: {CardNumberLastFour}, Amount: {Amount} {Currency}, Error: {ErrorMessage}",
                    correlationId, cardNumberLastFour, amount, currency, ex.Message);

                string userMessage = ex switch
                {
                    HttpRequestException => "Failed to communicate with the bank service.",
                    JsonException => "Invalid response format from the bank service.",
                    TaskCanceledException => "Bank service request timed out.",
                    _ => "An error occurred while processing the payment."
                };

                throw new PaymentProcessingException(userMessage, ex);
            }
        }

        public PaymentResponse? GetPaymentAsync(Guid id)
        {
            var payment = _paymentsRepository.Get(id);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found, PaymentId: {PaymentId}", id);
            }

            return payment;
        }
    }

    public class PaymentProcessingException : Exception
    {
        public PaymentProcessingException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}