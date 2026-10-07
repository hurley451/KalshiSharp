using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Communications;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class CommunicationsClientTests
{
    [Fact]
    public async Task GetCommunicationsIdAsync_UsesDocumentedEndpointAndParsesResponse()
    {
        var httpClient = new RecordingHttpClient
        {
            Response = new CommunicationsIdResponse { CommunicationsId = "comm-123" }
        };
        var client = new CommunicationsClient(httpClient);

        var result = await client.GetCommunicationsIdAsync();

        httpClient.LastRequest!.Method.Should().Be(HttpMethod.Get);
        httpClient.LastRequest.Path.Should().Be("/trade-api/v2/communications/id");
        result.CommunicationsId.Should().Be("comm-123");
    }

    private sealed class RecordingHttpClient : IKalshiHttpClient
    {
        public required object Response { get; init; }

        public KalshiRequest? LastRequest { get; private set; }

        public Task<TResponse> SendAsync<TResponse>(
            KalshiRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            LastRequest = request;
            return Task.FromResult((TResponse)Response);
        }

        public Task SendAsync(KalshiRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
