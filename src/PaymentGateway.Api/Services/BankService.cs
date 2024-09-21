using System.Text.Json;
using System.Text;

using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public interface IBankService
{
    Task<PostPaymentResponse> ProcessPayment(PostPaymentRequest request);
}

public class BankService : IBankService
{
    private readonly HttpClient _httpClient;

    private const string BankApiUrl = "http://localhost:8080/payments";

    public BankService(HttpClient httpClient) 
    {
        _httpClient = httpClient;
    }

    public async Task<PostPaymentResponse> ProcessPayment(PostPaymentRequest request)
    {
        if (!IsRequestValid(request))
            return RejectedResponse();

        try
        {
            var bankRequest = CreateBankRequest(request);
            var bankRequestJson = JsonSerializer.Serialize(bankRequest);
            var content = new StringContent(bankRequestJson, UnicodeEncoding.UTF8, "application/json");

            var bankResponse = await _httpClient.PostAsync(BankApiUrl, content);
            if (!bankResponse.IsSuccessStatusCode)
                return RejectedResponse();

            var result = await bankResponse.Content.ReadFromJsonAsync<BankPaymentResponse>();

            return ProcessedPaymentResponse(request, result?.Authorized ?? false);
        }
        catch (Exception ex)
        {
            return RejectedResponse();
        }
    }

    private bool IsRequestValid(PostPaymentRequest request)
    {
        return request != null
            && ValidationHelper.IsCardNumberValid(request.CardNumber)
            && ValidationHelper.IsExpiryMonthValid(request.ExpiryMonth)
            && ValidationHelper.IsExpiryValid(request.ExpiryMonth, request.ExpiryYear)
            && ValidationHelper.IsCvvValid(request.Cvv)
            && ValidationHelper.IsCurrencyValid(request.Currency);
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

    private PostPaymentResponse ProcessedPaymentResponse(PostPaymentRequest request, bool isAuthorized)
    {
        var cardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4);

        return new()
        {
            Id = Guid.NewGuid(),
            Status = isAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
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
