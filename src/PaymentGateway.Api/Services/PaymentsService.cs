using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Helpers;

namespace PaymentGateway.Api.Services;

public interface IPaymentsService
{
    PostPaymentResponse? GetPayment(Guid id);
    Task<PostPaymentResponse> ProcessPayment(PostPaymentRequest request);
}

public class PaymentsService : IPaymentsService
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IBankService _bankService;

    public PaymentsService(PaymentsRepository paymentsRepository, IBankService bankService)
    {
        _paymentsRepository = paymentsRepository;
        _bankService = bankService;
    }

    public PostPaymentResponse? GetPayment(Guid id)
    {
        return _paymentsRepository.Get(id);
    }

    public async Task<PostPaymentResponse> ProcessPayment(PostPaymentRequest request)
    {
        if (!IsRequestValid(request))
            return RejectedResponse();

        var bankRequest = CreateBankRequest(request);
        var paymentStatus = await _bankService.ProcessPayment(bankRequest);

        var paymentResponse = ProcessedPaymentResponse(request, paymentStatus);
        _paymentsRepository.Add(paymentResponse);

        return paymentResponse;
    }

    private bool IsRequestValid(PostPaymentRequest request)
    {
        return request != null
            && ValidationHelper.IsCardNumberValid(request.CardNumber)
            && ValidationHelper.IsExpiryMonthValid(request.ExpiryMonth)
            && ValidationHelper.IsExpiryValid(request.ExpiryMonth, request.ExpiryYear, DateTime.Now)
            && ValidationHelper.IsCvvValid(request.Cvv)
            && ValidationHelper.IsCurrencyValid(request.Currency)
            && ValidationHelper.IsNotNegative(request.Amount);
    }

    private BankPaymentRequest CreateBankRequest(PostPaymentRequest request)
    {
        var month = request.ExpiryMonth;
        var year = request.ExpiryYear;
        var expiryDateFormatted = month < 10 ? $"0{month}/{year}" : $"{month}/{year}";

        return new()
        {
            CardNumber = request.CardNumber,
            ExpiryDate = expiryDateFormatted,
            Amount = request.Amount,
            Currency = request.Currency,
            Cvv = request.Cvv
        };
    }

    private PostPaymentResponse ProcessedPaymentResponse(PostPaymentRequest request, PaymentStatus status)
    {
        if (status == PaymentStatus.Rejected)
            return RejectedResponse();

        var cardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4);

        return new()
        {
            Id = Guid.NewGuid(),
            Status = status,
            CardNumberLastFour = int.Parse(cardNumberLastFour),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Amount = request.Amount,
            Currency = request.Currency
        };
    }

    private PostPaymentResponse RejectedResponse()
        => new() { Status = PaymentStatus.Rejected };
}
