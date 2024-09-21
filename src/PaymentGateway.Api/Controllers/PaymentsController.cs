using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IBankService _paymentsService;


    public PaymentsController(PaymentsRepository paymentsRepository, IBankService paymentsService)
    {
        _paymentsRepository = paymentsRepository;
        _paymentsService = paymentsService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
            return new NotFoundObjectResult(payment);

        return new OkObjectResult(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync([FromBody] PostPaymentRequest request)
    {
        var response = await _paymentsService.ProcessPayment(request);

        if (response.Status == PaymentStatus.Rejected)
            return new BadRequestObjectResult(response);

        _paymentsRepository.Add(response);

        return new OkObjectResult(response);
    }
}