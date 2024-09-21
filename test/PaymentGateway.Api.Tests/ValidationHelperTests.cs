using PaymentGateway.Api.Helpers;

namespace PaymentGateway.Api.Tests;
public class ValidationHelperTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("abc", false)]
    [InlineData("abcdefghijklmn", false)]
    [InlineData("123", false)]
    [InlineData("123456789123456789123456789", false)]
    [InlineData("12345678912345", true)]
    [InlineData("123456789123456789", true)]
    public void TestIsCardNumberValid(string cardNumber, bool expected)
    {
        var isValid = ValidationHelper.IsCardNumberValid(cardNumber);

        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(24, false)]
    [InlineData(5, true)]
    [InlineData(12, true)]
    public void TestIsExpiryMonthValid(int month, bool expected)
    {
        var isValid = ValidationHelper.IsExpiryMonthValid(month);

        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData(1, 1970, "2024-01-01", false)]
    [InlineData(2, 2024, "2024-01-01", true)]
    [InlineData(1, 2024, "2024-01-31", false)]
    [InlineData(5, 2025, "2024-01-01", true)]
    public void TestIsExpiryValid(int month, int year, string currentDate, bool expected)
    {
        var dt = DateTime.Parse(currentDate);
        var isValid = ValidationHelper.IsExpiryValid(month, year, dt);

        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData("gbp", true)]
    [InlineData("USD", true)]
    [InlineData("abcd", false)]
    public void TestIsCurrencyValid(string currency, bool expected)
    {
        var isValid = ValidationHelper.IsCurrencyValid(currency);

        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData(-123, false)]
    [InlineData(12345, false)]
    [InlineData(1234, true)]
    [InlineData(123, true)]
    public void TestIsCvvValid(int cvv, bool expected)
    {
        var isValid = ValidationHelper.IsCvvValid(cvv);

        Assert.Equal(expected, isValid);
    }
}