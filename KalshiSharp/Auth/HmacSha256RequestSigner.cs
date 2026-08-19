using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using KalshiSharp.Configuration;
using Microsoft.Extensions.Options;

namespace KalshiSharp.Auth;

/// <summary>
/// Legacy HMAC-SHA256 signer retained for compatibility with KalshiSharp 1.0.1.
/// Current Kalshi credentials use <see cref="RsaPssRequestSigner"/>.
/// </summary>
[Obsolete("Kalshi now requires RSA-PSS request signing. Use RsaPssRequestSigner instead.")]
public sealed class HmacSha256RequestSigner : IKalshiRequestSigner, IDisposable
{
    /// <summary>Header name for the API key.</summary>
    public const string AccessKeyHeader = "KALSHI-ACCESS-KEY";

    /// <summary>Header name for the timestamp.</summary>
    public const string AccessTimestampHeader = "KALSHI-ACCESS-TIMESTAMP";

    /// <summary>Header name for the signature.</summary>
    public const string AccessSignatureHeader = "KALSHI-ACCESS-SIGNATURE";

    private readonly string _apiKey;
    private readonly HMACSHA256 _hmac;
    private bool _disposed;

    /// <summary>Initializes the signer from client options.</summary>
    public HmacSha256RequestSigner(IOptions<KalshiClientOptions> options)
        : this(
            options?.Value.ApiKey ?? throw new ArgumentNullException(nameof(options)),
            options.Value.ApiSecret)
    {
    }

    /// <summary>Initializes the signer from an API key and legacy shared secret.</summary>
    public HmacSha256RequestSigner(string apiKey, string apiSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiSecret);

        _apiKey = apiKey;
        _hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
    }

    /// <inheritdoc />
    public void Sign(HttpRequestMessage request, ReadOnlySpan<byte> body, DateTimeOffset timestamp)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        var timestampMs = timestamp.ToUnixTimeMilliseconds();
        var signature = ComputeSignature(CanonicalRequestBuilder.Build(timestampMs, request, body));

        request.Headers.Remove(AccessKeyHeader);
        request.Headers.Remove(AccessTimestampHeader);
        request.Headers.Remove(AccessSignatureHeader);
        request.Headers.TryAddWithoutValidation(AccessKeyHeader, _apiKey);
        request.Headers.TryAddWithoutValidation(AccessTimestampHeader, timestampMs.ToString(CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation(AccessSignatureHeader, signature);
    }

    /// <summary>Computes the legacy Base64-encoded HMAC-SHA256 signature.</summary>
    internal string ComputeSignature(byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Convert.ToBase64String(_hmac.ComputeHash(data));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _hmac.Dispose();
        _disposed = true;
    }
}
