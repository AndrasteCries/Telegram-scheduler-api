using System.Security.Cryptography;
using System.Text;

namespace TelegramScheduler.Services;

public class SignedUrlService
{
    private readonly string _secret;
    private readonly string _baseUrl;

    public SignedUrlService(IConfiguration config)
    {
        _baseUrl = config["MediaStorage:BaseUrl"]
                   ?? throw new InvalidOperationException(
                       "MediaStorage:BaseUrl is not configured.");

        _secret = config["MediaStorage:SigningSecret"]
                  ?? throw new InvalidOperationException(
                      "MediaStorage:SigningSecret is not configured.");
    }

    public string GenerateSignedUrl(string resourcePath, TimeSpan validFor)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(validFor).ToUnixTimeSeconds();
        var payload = $"{resourcePath}:{expiresAt}";
        var signature = ComputeHmac(payload);

        return $"{_baseUrl}{resourcePath}?expires={expiresAt}&sig={signature}";
    }

    public bool ValidateSignedUrl(string resourcePath, long expires, string signature)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
            return false;

        var payload = $"{resourcePath}:{expires}";
        var expectedSignature = ComputeHmac(payload);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expectedSignature));
    }

    private string ComputeHmac(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}