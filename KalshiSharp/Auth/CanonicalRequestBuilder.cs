using System.Globalization;
using System.Text;

namespace KalshiSharp.Auth;

/// <summary>
/// Builds canonical request strings for legacy HMAC-SHA256 request signing.
/// </summary>
public static class CanonicalRequestBuilder
{
    private const byte NewlineByte = (byte)'\n';

    /// <summary>Builds a canonical request as UTF-8 bytes.</summary>
    public static byte[] Build(long timestampMs, string method, string pathAndQuery, ReadOnlySpan<byte> body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(pathAndQuery);

        var timestamp = timestampMs.ToString(CultureInfo.InvariantCulture);
        var upperMethod = method.ToUpperInvariant();
        var totalSize = Encoding.UTF8.GetByteCount(timestamp)
            + Encoding.UTF8.GetByteCount(upperMethod)
            + Encoding.UTF8.GetByteCount(pathAndQuery)
            + body.Length
            + 3;

        var result = new byte[totalSize];
        var offset = 0;

        offset += Encoding.UTF8.GetBytes(timestamp, result.AsSpan(offset));
        result[offset++] = NewlineByte;
        offset += Encoding.UTF8.GetBytes(upperMethod, result.AsSpan(offset));
        result[offset++] = NewlineByte;
        offset += Encoding.UTF8.GetBytes(pathAndQuery, result.AsSpan(offset));
        result[offset++] = NewlineByte;
        body.CopyTo(result.AsSpan(offset));

        return result;
    }

    /// <summary>Builds a canonical request from an HTTP request message.</summary>
    public static byte[] Build(long timestampMs, HttpRequestMessage request, ReadOnlySpan<byte> body)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Build(
            timestampMs,
            request.Method.Method,
            request.RequestUri?.PathAndQuery ?? "/",
            body);
    }
}
