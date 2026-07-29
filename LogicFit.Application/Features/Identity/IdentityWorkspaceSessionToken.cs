using System.Security.Cryptography;
using System.Text;

namespace LogicFit.Application.Features.Identity;

internal static class IdentityWorkspaceSessionToken
{
    public static string CreateRaw() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
