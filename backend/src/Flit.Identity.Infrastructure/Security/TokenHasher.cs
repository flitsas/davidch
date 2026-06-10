using System.Security.Cryptography;
using System.Text;

namespace Flit.Identity.Infrastructure.Security;

public static class TokenHasher
{
    public static string Sha256(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
}
