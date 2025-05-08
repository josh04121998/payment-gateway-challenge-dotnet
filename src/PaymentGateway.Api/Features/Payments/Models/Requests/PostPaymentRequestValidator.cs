using FluentValidation;

namespace PaymentGateway.Api.Features.Payments.Models.Requests
{
    public class PaymentRequestValidator : AbstractValidator<PostPaymentRequest>
    {
        const string OnlyNumericMessage = "Must only contain numeric characters.";
        const string ValidCurrencyMessage = "Must be a valid ISO currency code.";
        const string FutureExpiryDateMessage = "The expiry date must be in the future.";
        private static readonly string[] CurrencyCodes = { "GBP", "EUR", "USD" };

        public PaymentRequestValidator()
        {
            RuleFor(p => p.CardNumber)
                .NotEmpty()
                .MinimumLength(14)
                .MaximumLength(19);

            RuleFor(p => p.CardNumber)
                .Must(x => x.All(char.IsDigit))
                .WithMessage(OnlyNumericMessage);

            RuleFor(p => p.ExpiryMonth).NotEmpty().InclusiveBetween(1, 12);
            RuleFor(p => p.ExpiryYear).NotEmpty().GreaterThanOrEqualTo(DateTime.Today.Year);
            RuleFor(p => p)
                .Must(p => BeFutureExpiryDate(p.ExpiryMonth, p.ExpiryYear))
                .WithName($"{nameof(PostPaymentRequest.ExpiryMonth)}/{nameof(PostPaymentRequest.ExpiryYear)}")
                .WithMessage(FutureExpiryDateMessage);

            RuleFor(p => p.Currency)
                .Must(BeValidCurrencyCode)
                .WithMessage(ValidCurrencyMessage);

            RuleFor(p => p.Amount).NotEmpty().GreaterThanOrEqualTo(0);

            RuleFor(p => p.Cvv).NotEmpty().Length(3, 4);
            RuleFor(p => p.Cvv)
                .Must(x => x.All(char.IsDigit))
                .WithMessage(OnlyNumericMessage);
        }

        private static bool BeValidCurrencyCode(string currency)
        {
            if (string.IsNullOrEmpty(currency))
                return false;

            var currencyUp = currency.ToUpperInvariant();
            return CurrencyCodes.Contains(currencyUp);
        }

        private static bool BeFutureExpiryDate(int month, int year)
        {
            var today = DateTime.Today;

            if (year > today.Year)
                return true;
            return year == today.Year && month > today.Month;
        }
    }
}
