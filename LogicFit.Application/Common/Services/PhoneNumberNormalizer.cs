namespace LogicFit.Application.Common.Services;

/// <summary>Normalizes contact phone values without making phone a credential.</summary>
public static class PhoneNumberNormalizer
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Phone number is required.");

        if (!value.TrimStart().StartsWith('+'))
            throw new FormatException("Phone number must use E.164 format.");

        var normalized = new string(value.Where(char.IsDigit).ToArray());
        if (normalized.Length < 8 || normalized.Length > 15)
            throw new FormatException("Phone number must contain 8 to 15 digits.");

        return $"+{normalized}";
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}
