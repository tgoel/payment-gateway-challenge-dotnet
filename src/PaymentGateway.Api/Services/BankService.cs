using System.Text.Json;
using System.Text;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public interface IBankService
{
    Task<PaymentStatus> ProcessPayment(BankPaymentRequest request);
}

public class BankService : IBankService
{
    private readonly HttpClient _httpClient;

    private const string BankApiUrl = "http://localhost:8080/payments";

    public BankService(HttpClient httpClient) 
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentStatus> ProcessPayment(BankPaymentRequest request)
    {
        try
        {
            var requestJson = JsonSerializer.Serialize(request);
            var content = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");

            var bankResponse = await _httpClient.PostAsync(BankApiUrl, content);
            if (!bankResponse.IsSuccessStatusCode)
                return PaymentStatus.Rejected;

            var result = await bankResponse.Content.ReadFromJsonAsync<BankPaymentResponse>();
            var isAuthorized = result?.Authorized ?? false;

            return isAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
        }
        catch (Exception ex)
        {
            return PaymentStatus.Rejected;
        }
    }
}
