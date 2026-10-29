namespace Zayun.Domain.Shared.Text;

/// <summary>A loose check for phone numbers as people write them: "+44 7881 186690", "(0) 802-317-6049".</summary>
public static class PhoneNumbers
{
    /// <summary>Only digits, spaces, +, (, ), - and dots, with 7 to 15 digits.</summary>
    /// <param name="value">The number.</param>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Any(c => !(char.IsAsciiDigit(c) || c is ' ' or '+' or '(' or ')' or '-' or '.'))) return false;
        var digits = value.Count(char.IsAsciiDigit);
        return digits is >= 7 and <= 15;
    }

    /// <summary>The number for a tel: link: "+447881186690".</summary>
    /// <param name="value">The number as written.</param>
    public static string ForLink(string value) =>
        (value.TrimStart().StartsWith('+') ? "+" : "") + new string(value.Where(char.IsAsciiDigit).ToArray());
}
