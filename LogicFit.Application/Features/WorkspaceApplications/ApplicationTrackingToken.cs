using System.Security.Cryptography;
using System.Text;

namespace LogicFit.Application.Features.WorkspaceApplications;

internal static class ApplicationTrackingToken
{
    public static string CreateRaw() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
