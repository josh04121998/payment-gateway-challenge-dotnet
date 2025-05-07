using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly ILogger _logger;

    public PaymentsController(PaymentsRepository paymentsRepository,
        ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);
        if (payment == null)
        {
            _logger.LogWarning($"Payment '{id}' not found.");
            return NotFound();
        }
        return new OkObjectResult(payment);
    }
}