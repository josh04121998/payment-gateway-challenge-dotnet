using FluentValidation.Results;

using PaymentGateway.Api.Features.Payments.Models.Responses;

namespace PaymentGateway.Api.Common.Helpers
{
    public class ValidationHelper
    {
        public static ValidationError[] ValidationErrors(ValidationResult validationResult)
        {
            return validationResult.Errors
                .Select(x => new ValidationError { PropertyName = x.PropertyName, ErrorMessage = x.ErrorMessage })
                .ToArray();
        }
    }
}
