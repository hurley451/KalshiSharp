using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest.Account;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class AccountClientTests
{
    [Fact]
    public async Task GetApiLimitsAsync_UsesCurrentEndpoint()
    {
        var http = new RecordingHttpClient(new AccountApiLimitsResponse
        {
            UsageTier = "expert",
            Read = new AccountTokenBucketResponse { RefillRate = 100, BucketCapacity = 200 },
            Write = new AccountTokenBucketResponse { RefillRate = 50, BucketCapacity = 100 }
        });
        var client = new AccountClient(http);

        var result = await client.GetApiLimitsAsync();

        http.LastRequest!.Method.Should().Be(HttpMethod.Get);
        http.LastRequest.Path.Should().Be("/trade-api/v2/account/limits");
        result.Read.RefillRate.Should().Be(100);
    }

    [Fact]
    public async Task GetEndpointCostsAsync_UsesCurrentEndpoint()
    {
        var http = new RecordingHttpClient(new EndpointCostsResponse
        {
            DefaultCost = 10,
            EndpointCosts = [new EndpointCostResponse { Method = "DELETE", Path = "/portfolio/events/orders/{id}", Cost = 2 }]
        });
        var client = new AccountClient(http);

        var result = await client.GetEndpointCostsAsync();

        http.LastRequest!.Path.Should().Be("/trade-api/v2/account/endpoint_costs");
        result.EndpointCosts.Should().ContainSingle().Which.Cost.Should().Be(2);
    }

    private sealed class RecordingHttpClient(object response) : IKalshiHttpClient
    {
        public KalshiRequest? LastRequest { get; private set; }

        public Task<TResponse> SendAsync<TResponse>(KalshiRequest request, CancellationToken cancellationToken = default)
            where TResponse : class
        {
            LastRequest = request;
            return Task.FromResult((TResponse)response);
        }

        public Task SendAsync(KalshiRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
