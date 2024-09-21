using System.Text.RegularExpressions;

namespace PaymentGateway.Api.Helpers;

public class ValidationHelper
{
    private static readonly List<string> ValidIsoCodes = new() { "GBP", "USD", "EUR" };

    public static bool IsCardNumberValid(string cardNumber) 
    {
        var isNumeric = Regex.IsMatch(cardNumber, @"^\d+$");
        var isBetween14And19CharsLong = cardNumber.Length >= 14 && cardNumber.Length <= 19;

        return isNumeric && isBetween14And19CharsLong;
    }

    public static bool IsExpiryMonthValid(int expiryMonth)
        => expiryMonth >= 1 && expiryMonth <= 12;

    public static bool IsExpiryValid(int expiryMonth, int expiryYear, DateTime currentDt)
    {
        var expiryDate = new DateTime(expiryYear, expiryMonth, 1)
            .AddMonths(1)
            .AddDays(-1);

        return expiryDate > currentDt;
    }

    public static bool IsCurrencyValid(string currency)
        => currency.Length == 3 && ValidIsoCodes.Contains(currency.ToUpper());

    public static bool IsCvvValid(int cvv)
    {
        var cvvLength = cvv.ToString().Length;
        return (cvvLength == 3 || cvvLength == 4) && cvv > 0;
    }
}
