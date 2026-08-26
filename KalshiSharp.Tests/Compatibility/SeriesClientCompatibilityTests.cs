using FluentAssertions;
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
}
