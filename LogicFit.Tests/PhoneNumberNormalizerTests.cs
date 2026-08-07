using LogicFit.Application.Common.Services;
using Xunit;

namespace LogicFit.Tests;

public sealed class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("01015819700", "+201015819700")]
    [InlineData("010-158-19700", "+201015819700")]
    [InlineData("001015819700", "+1015819700")]
    [InlineData("+201015819700", "+201015819700")]
    public void NormalizeAdminInput_accepts_local_and_e164_formats(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.NormalizeAdminInput(input));
    }

    [Fact]
    public void Normalize_rejects_invalid_non_e164_non_egyptian_values()
    {
        var exception = Assert.Throws<FormatException>(() => PhoneNumberNormalizer.Normalize("12345"));

        Assert.Contains("E.164", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Normalize_keeps_the_strict_e164_contract_for_environment_values()
    {
        var exception = Assert.Throws<FormatException>(() => PhoneNumberNormalizer.Normalize("01015819700"));

        Assert.Contains("E.164", exception.Message, StringComparison.Ordinal);
    }
}
