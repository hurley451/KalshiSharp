using FluentAssertions;
using KalshiSharp.Http;
using KalshiSharp.Rest;
using KalshiSharp.Rest.Events;
using KalshiSharp.Rest.Exchange;
using KalshiSharp.Rest.Markets;
using KalshiSharp.Rest.Orders;
using KalshiSharp.Rest.Portfolio;
using KalshiSharp.Rest.Users;
using Xunit;

namespace KalshiSharp.Tests.Compatibility;

public sealed class SeriesClientCompatibilityTests
{
    [Fact]
    public void ExistingRootImplementations_DefaultSeriesCapabilityToNull()
    {
        IKalshiClient client = new LegacyRootClient();

        client.Series.Should().BeNull();
        client.StructuredTargets.Should().BeNull();
        client.Milestones.Should().BeNull();
    }

    [Fact]
    public void CurrentRootClient_ProvidesReferenceDataCapabilities()
    {
        using var client = new KalshiClient(new NoOpHttpClient());

        client.Series.Should().NotBeNull();
        client.StructuredTargets.Should().NotBeNull();
        client.Milestones.Should().NotBeNull();
    }

    private sealed class LegacyRootClient : IKalshiClient
    {
        public IExchangeClient Exchange => null!;

        public IMarketClient Markets => null!;

        public IEventClient Events => null!;

        public IOrderClient Orders => null!;

        public IPortfolioClient Portfolio => null!;

        public IUserClient Users => null!;

        public void Dispose()
        {
        }
    }

    private sealed class NoOpHttpClient : IKalshiHttpClient
    {
        public Task<TResponse> SendAsync<TResponse>(
            KalshiRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class =>
            throw new NotSupportedException();

        public Task SendAsync(KalshiRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
