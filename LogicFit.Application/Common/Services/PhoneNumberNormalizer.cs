namespace LogicFit.Application.Common.Services;

/// <summary>Normalizes contact phone values without making phone a credential.</summary>
public static class PhoneNumberNormalizer
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Phone number is required.");

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('+'))
            throw new FormatException("Phone number must use E.164 format.");

        var normalized = new string(trimmed.Where(char.IsDigit).ToArray());
        if (normalized.Length < 8 || normalized.Length > 15)
            throw new FormatException("Phone number must contain 8 to 15 digits.");

        return $"+{normalized}";
    }

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);

    /// <summary>
    /// Normalizes contact values entered by operators in the regional admin forms. The
    /// underlying stored value remains E.164, while existing environment/bootstrap inputs
    /// continue to use the strict <see cref="Normalize"/> contract.
    /// </summary>
    public static string NormalizeAdminInput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("Phone number is required.");

        var compact = new string(value.Trim().Where(character => char.IsDigit(character) || character == '+').ToArray());
        if (compact.Length == 11 && compact.StartsWith("01", StringComparison.Ordinal))
            compact = $"+20{compact[1..]}";
        else if (compact.StartsWith("00", StringComparison.Ordinal))
            compact = $"+{compact[2..]}";

        return Normalize(compact);
    }

    public static string? NormalizeOptionalAdminInput(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : NormalizeAdminInput(value);
}
