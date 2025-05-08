using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Common.Helpers;
using PaymentGateway.Api.Features.Payments.Handler;
using PaymentGateway.Api.Features.Payments.Models.Requests;
using PaymentGateway.Api.Features.Payments.Models.Responses;

namespace PaymentGateway.Api.Features.Payments
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IValidator<PostPaymentRequest> _postPaymentRequestValidator;
        private readonly PaymentHandler _paymentHandler;

        public PaymentsController(
            IValidator<PostPaymentRequest> postPaymentRequestValidator,
            PaymentHandler paymentHandler)
        {
            _postPaymentRequestValidator = postPaymentRequestValidator;
            _paymentHandler = paymentHandler;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentResponse?>> GetPaymentAsync(Guid id)
        {
            var payment = _paymentHandler.GetPaymentAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        [HttpPost]
        [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(PostPaymentFailedValidationResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentResponse>> PostPaymentAsync([FromBody] PostPaymentRequest request)
        {
            var validationResult = await _postPaymentRequestValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new PostPaymentFailedValidationResponse
                {
                    Errors = ValidationHelper.ValidationErrors(validationResult)
                });
            }

            try
            {
                var paymentResponse = await _paymentHandler.ProcessPaymentAsync(request);
                return Ok(paymentResponse);
            }
            catch (PaymentProcessingException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}