using System.Buffers.Text;
using System.Security.Cryptography;

namespace AutoMarket.BuildingBlocks.Security;

// SEC-AUTH-05/07: kriptoqrafik təsadüfi token (256 bit), base64url. Testdə determinist implementasiya ilə əvəz oluna bilər (NFR-TEST-08)
public interface ISecureTokenGenerator
{
    public string Generate();
}

internal sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeBytes));
}
