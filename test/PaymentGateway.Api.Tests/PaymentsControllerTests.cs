using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GivenAValidRequestAndAuthorizedCard_ReturnsAuthorizedStatus()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 4,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = 123
        };
        var requestJson = JsonSerializer.Serialize(request);
        var stringContent = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");

        var paymentsRepository = new PaymentsRepository();
        var client = CreateSut(paymentsRepository);

        // Act
        var response = await client.PostAsync($"/api/Payments", stringContent);
        var result = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        var payment = paymentsRepository.Get(result.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK , response.StatusCode);
        Assert.Equal(PaymentStatus.Authorized, result?.Status);
        Assert.NotNull(payment);
    }

    [Fact]
    public async Task GivenAValidRequestAndUnauthorizedCard_ReturnsDeclinedStatus()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248112",
            ExpiryMonth = 1,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 60000,
            Cvv = 456
        };
        var requestJson = JsonSerializer.Serialize(request);
        var stringContent = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");

        var paymentsRepository = new PaymentsRepository();
        var client = CreateSut(paymentsRepository);

        // Act
        var response = await client.PostAsync($"/api/Payments", stringContent);
        var result = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        var payment = paymentsRepository.Get(result.Id);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PaymentStatus.Declined, result?.Status);
        Assert.NotNull(payment);
    }

    [Fact]
    public async Task GivenAValidRequestAndUnknownCard_ReturnsDeclinedStatus()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248113",
            ExpiryMonth = 1,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 60000,
            Cvv = 456
        };
        var requestJson = JsonSerializer.Serialize(request);
        var stringContent = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");

        var paymentsRepository = new PaymentsRepository();
        var client = CreateSut(paymentsRepository);

        // Act
        var response = await client.PostAsync($"/api/Payments", stringContent);
        var result = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PaymentStatus.Rejected, result?.Status);
    }

    [Fact]
    public async Task GivenAnInvalidRequest_ReturnsRejectedStatus()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "123",
            ExpiryMonth = 4,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = 123
        };
        var requestJson = JsonSerializer.Serialize(request);
        var stringContent = new StringContent(requestJson, UnicodeEncoding.UTF8, "application/json");

        var paymentsRepository = new PaymentsRepository();
        var client = CreateSut(paymentsRepository);

        // Act
        var response = await client.PostAsync($"/api/Payments", stringContent);
        var result = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PaymentStatus.Rejected, result?.Status);
    }

    private HttpClient CreateSut(PaymentsRepository paymentsRepository)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        return webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)
                .AddTransient<IPaymentsService, PaymentsService>()
                .AddHttpClient<IBankService, BankService>()))
            .CreateClient();
    }
}