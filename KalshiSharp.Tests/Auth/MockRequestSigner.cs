using System.Globalization;
using KalshiSharp.Auth;

namespace KalshiSharp.Tests.Auth;

/// <summary>
/// Mock request signer for testing. Adds valid headers but signature is not verified.
/// </summary>
public sealed class MockRequestSigner : IKalshiRequestSigner
{
    public const string AccessKeyHeader = "KALSHI-ACCESS-KEY";
    public const string AccessTimestampHeader = "KALSHI-ACCESS-TIMESTAMP";
    public const string AccessSignatureHeader = "KALSHI-ACCESS-SIGNATURE";

    private readonly string _apiKey;

    public MockRequestSigner(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        // apiSecret is ignored in mock - no actual signing
    }

    public void Sign(HttpRequestMessage request, ReadOnlySpan<byte> body, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(request);

        var timestampMs = timestamp.ToUnixTimeMilliseconds();

        request.Headers.Remove(AccessKeyHeader);
        request.Headers.Remove(AccessTimestampHeader);
        request.Headers.Remove(AccessSignatureHeader);

        request.Headers.TryAddWithoutValidation(AccessKeyHeader, _apiKey);
        request.Headers.TryAddWithoutValidation(AccessTimestampHeader, timestampMs.ToString(CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation(AccessSignatureHeader, "mock-signature");
    }
}
