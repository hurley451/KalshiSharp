using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using KalshiSharp.Configuration;
using KalshiSharp.Errors;
using KalshiSharp.Http;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Rest;
using KalshiSharp.Rest.Events;
using KalshiSharp.Rest.Exchange;
using KalshiSharp.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KalshiSharp.Tests.Rest;

public sealed class FeeMetadataContractTests
{
    private const string SeriesItem = """{"id":"s1","series_ticker":"S","fee_type":"quadratic","fee_multiplier":1.234567890123456789,"scheduled_ts":"2026-10-17T10:20:30.1234567-04:00"}""";
    private const string EventItem = """{"id":"e1","event_ticker":"E","series_ticker":"S","fee_type_override":null,"fee_multiplier_override":null,"scheduled_ts":"2026-10-17T10:20:30.1234567-04:00"}""";

    [Theory]
    [InlineData(null, "")]
    [InlineData(false, "?show_historical=false")]
    [InlineData(true, "?show_historical=true")]
    public async Task SeriesQueryPreservesTriStateAndUsesPublicBodylessGet(bool? historical, string suffix)
    {
        using var f = new Fixture("""{"series_fee_change_arr":[]}""");
        var result = await f.Client.Exchange.GetSeriesFeeChangesAsync(historical is null ? null : new() { ShowHistorical = historical });
        Assert.Empty(result.SeriesFeeChanges);
        Assert.Equal("/trade-api/v2/series/fee_changes" + suffix, f.Transport.Path);
        Assert.Equal(HttpMethod.Get, f.Transport.Method); Assert.Null(f.Transport.Body);
        Assert.False(f.Transport.HasCredentials); Assert.Equal(1, f.Transport.Calls);
    }

    [Fact]
    public async Task QueriesEscapeOpaqueValuesAndDoNotAddUnsupportedFilters()
    {
        const string opaque = "S /?&=+%\u00e9";
        using var f = new Fixture("""{"series_fee_change_arr":[]}""");
        await f.Client.Exchange.GetSeriesFeeChangesAsync(new() { SeriesTicker = opaque, ShowHistorical = true });
        Assert.Equal("/trade-api/v2/series/fee_changes?series_ticker=" + Uri.EscapeDataString(opaque) + "&show_historical=true", f.Transport.Path);
        f.Transport.Json = """{"event_fee_changes":[],"cursor":""}""";
        await f.Client.Events.GetEventFeeChangesAsync(new() { EventTicker = opaque, Cursor = opaque, Limit = 1000 });
        Assert.Equal("/trade-api/v2/events/fee_changes?limit=1000&cursor=" + Uri.EscapeDataString(opaque) + "&event_ticker=" + Uri.EscapeDataString(opaque), f.Transport.Path);
        Assert.Null(f.Transport.Body);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task InvalidEventLimitNeverSends(int limit)
    {
        using var f = new Fixture("{}");
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => f.Client.Events.GetEventFeeChangesAsync(new() { Limit = limit }));
        Assert.Equal(0, f.Transport.Calls);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(1, "?limit=1")]
    [InlineData(1000, "?limit=1000")]
    public async Task EventPagePreservesCursorEvenWithNoRows(int? limit, string suffix)
    {
        using var f = new Fixture("""{"event_fee_changes":[],"cursor":"opaque/+=="}""");
        var page = await f.Client.Events.GetEventFeeChangesAsync(limit is null ? null : new() { Limit = limit });
        Assert.Equal("/trade-api/v2/events/fee_changes" + suffix, f.Transport.Path);
        Assert.Empty(page.EventFeeChanges); Assert.Equal("opaque/+==", page.Cursor);
        Assert.Equal(HttpMethod.Get, f.Transport.Method); Assert.Null(f.Transport.Body); Assert.False(f.Transport.HasCredentials);
    }

    [Theory]
    [InlineData("quadratic")]
    [InlineData("quadratic_with_maker_fees")]
    [InlineData("quadratic_with_combo_maker_fees")]
    [InlineData("flat")]
    [InlineData("future_fee_type")]
    public async Task FeeTypesDecimalsAndScheduledOffsetsRemainFaithful(string type)
    {
        using var f = new Fixture("{\"series_fee_change_arr\":[" + SeriesItem.Replace("quadratic", type, StringComparison.Ordinal) + "]}");
        var row = Assert.Single((await f.Client.Exchange.GetSeriesFeeChangesAsync()).SeriesFeeChanges);
        Assert.Equal(type, row.FeeType); Assert.Equal(1.234567890123456789m, row.FeeMultiplier);
        Assert.Equal(TimeSpan.FromHours(-4), row.ScheduledTs.Offset);
        var roundTrip = JsonSerializer.Serialize(row, KalshiJsonOptions.Default);
        using var json = JsonDocument.Parse(roundTrip);
        Assert.Equal(JsonValueKind.Number, json.RootElement.GetProperty("fee_multiplier").ValueKind);
        Assert.Equal(JsonValueKind.String, json.RootElement.GetProperty("scheduled_ts").ValueKind);
        Assert.Equal(row, JsonSerializer.Deserialize<SeriesFeeChange>(roundTrip, KalshiJsonOptions.Default));
    }

    [Theory]
    [InlineData("null", "null")]
    [InlineData("\"flat\"", "0")]
    [InlineData("\"future_fee_type\"", "1.234567890123456789")]
    [InlineData("null", "1e-6")]
    public async Task EventOverridesPreserveNullClearsAndNumericZero(string type, string multiplier)
    {
        var item = EventItem.Replace("\"fee_type_override\":null", "\"fee_type_override\":" + type, StringComparison.Ordinal)
            .Replace("\"fee_multiplier_override\":null", "\"fee_multiplier_override\":" + multiplier, StringComparison.Ordinal);
        using var f = new Fixture("{\"event_fee_changes\":[" + item + "],\"cursor\":\"\"}");
        var row = Assert.Single((await f.Client.Events.GetEventFeeChangesAsync()).EventFeeChanges);
        Assert.Equal(type == "null" ? null : type.Trim('"'), row.FeeTypeOverride);
        Assert.Equal(multiplier == "null" ? (decimal?)null : decimal.Parse(multiplier, NumberStyles.Float, CultureInfo.InvariantCulture), row.FeeMultiplierOverride);
        using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(row, KalshiJsonOptions.Default));
        Assert.Equal(type == "null" ? JsonValueKind.Null : JsonValueKind.String, roundTrip.RootElement.GetProperty("fee_type_override").ValueKind);
        Assert.Equal(multiplier == "null" ? JsonValueKind.Null : JsonValueKind.Number, roundTrip.RootElement.GetProperty("fee_multiplier_override").ValueKind);
    }

    [Theory]
    [InlineData(false, "{}")]
    [InlineData(false, "{\"series_fee_change_arr\":null}")]
    [InlineData(false, "{\"series_fee_change_arr\":[null]}")]
    [InlineData(true, "{}")]
    [InlineData(true, "{\"cursor\":\"\"}")]
    [InlineData(true, "{\"event_fee_changes\":[]}")]
    [InlineData(true, "{\"event_fee_changes\":[],\"cursor\":null}")]
    [InlineData(true, "{\"event_fee_changes\":null,\"cursor\":\"\"}")]
    [InlineData(true, "{\"event_fee_changes\":[null],\"cursor\":\"\"}")]
    [InlineData(false, "not-json")]
    public async Task MalformedPagesCannotMasqueradeAsEmptySuccess(bool events, string body)
    {
        using var f = new Fixture(body);
        var error = await Assert.ThrowsAsync<KalshiException>(() => Read(f, events));
        Assert.Equal(HttpStatusCode.OK, error.StatusCode); Assert.IsType<JsonException>(error.InnerException);
        Assert.Equal(1, f.Transport.Calls);
    }

    [Theory]
    [InlineData("fee_type_override")]
    [InlineData("fee_multiplier_override")]
    [InlineData("scheduled_ts")]
    [InlineData("event_ticker")]
    public async Task MissingEventMembersAreNotOverrideClears(string missing)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(EventItem)!.AsObject(); node.Remove(missing);
        using var f = new Fixture("{\"event_fee_changes\":[" + node.ToJsonString() + "],\"cursor\":\"\"}");
        await Assert.ThrowsAsync<KalshiException>(() => f.Client.Events.GetEventFeeChangesAsync());
    }

    [Theory]
    [InlineData("id")]
    [InlineData("series_ticker")]
    [InlineData("fee_type")]
    [InlineData("fee_multiplier")]
    [InlineData("scheduled_ts")]
    public async Task MissingSeriesMembersCannotBecomeDefaultFeeValues(string missing)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(SeriesItem)!.AsObject(); node.Remove(missing);
        using var f = new Fixture("{\"series_fee_change_arr\":[" + node.ToJsonString() + "]}");
        await Assert.ThrowsAsync<KalshiException>(() => f.Client.Exchange.GetSeriesFeeChangesAsync());
    }

    [Theory]
    [InlineData("id", "null")]
    [InlineData("series_ticker", "null")]
    [InlineData("fee_type", "null")]
    [InlineData("fee_multiplier", "null")]
    [InlineData("fee_multiplier", "\"1.25\"")]
    [InlineData("fee_multiplier", "1e100")]
    [InlineData("scheduled_ts", "null")]
    [InlineData("scheduled_ts", "\"invalid\"")]
    [InlineData("scheduled_ts", "123")]
    public async Task MalformedSeriesValuesProduceSdkDeserializationErrors(string field, string value)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(SeriesItem)!.AsObject(); node[field] = System.Text.Json.Nodes.JsonNode.Parse(value);
        using var f = new Fixture("{\"series_fee_change_arr\":[" + node.ToJsonString() + "]}");
        var error = await Assert.ThrowsAsync<KalshiException>(() => f.Client.Exchange.GetSeriesFeeChangesAsync());
        Assert.IsType<JsonException>(error.InnerException);
    }

    [Theory]
    [InlineData(false, 400)]
    [InlineData(false, 500)]
    [InlineData(true, 400)]
    [InlineData(true, 500)]
    [InlineData(true, 401)]
    [InlineData(false, 429)]
    public async Task HttpErrorsRetainExistingSdkSemanticsAndAreNotRetried(bool events, int status)
    {
        using var f = new Fixture("""{"code":"fee_error","message":"Synthetic fee error"}""", (HttpStatusCode)status);
        var error = await Assert.ThrowsAnyAsync<KalshiException>(() => Read(f, events));
        Assert.Equal((HttpStatusCode)status, error.StatusCode); Assert.Equal("fee_error", error.ErrorCode);
        Assert.Equal(1, f.Transport.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreCanceledReadsNeverReachTransport(bool events)
    {
        using var f = new Fixture("{}"); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Read(f, events, cts.Token));
        Assert.Equal(0, f.Transport.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightCancellationPropagatesWithoutRetry(bool events)
    {
        using var f = new Fixture("{}"); using var cts = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Transport.BeforeReply = async token => { entered.SetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, token); };
        var request = Read(f, events, cts.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.Equal(1, f.Transport.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportFailuresPropagateWithoutRetry(bool events)
    {
        using var f = new Fixture("{}");
        var failure = new HttpRequestException("Synthetic offline failure");
        f.Transport.BeforeReply = _ => Task.FromException(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<HttpRequestException>(() => Read(f, events)));
        Assert.Equal(1, f.Transport.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("\"2026-10-17T10:20:30.1234567-04:00\"")]
    public async Task MarketWaiverIsNullableAndPreservedThroughTypedMarketReads(string? waiver)
    {
        var market = "{\"ticker\":\"M\",\"event_ticker\":\"E\",\"title\":\"Market\",\"status\":\"active\"" +
            (waiver is null ? "" : ",\"fee_waiver_expiration_time\":" + waiver) + "}";
        using var f = new Fixture("{\"market\":" + market + "}");
        var row = await f.Client.Markets.GetMarketAsync("M");
        if (waiver is null or "null") Assert.Null(row.FeeWaiverExpirationTime);
        else
        {
            Assert.Equal(DateTimeOffset.Parse(waiver.Trim('"'), CultureInfo.InvariantCulture), row.FeeWaiverExpirationTime);
            Assert.Equal(TimeSpan.FromHours(-4), row.FeeWaiverExpirationTime!.Value.Offset);
            var json = JsonSerializer.Serialize(row, KalshiJsonOptions.Default);
            Assert.Contains("2026-10-17T10:20:30.1234567-04:00", json, StringComparison.Ordinal);
        }
        f.Transport.Json = "{\"markets\":[" + market + "],\"cursor\":\"\"}";
        Assert.Equal(row.FeeWaiverExpirationTime, Assert.Single((await f.Client.Markets.ListMarketsAsync()).Markets).FeeWaiverExpirationTime);
        f.Transport.Json = "{\"event\":{\"event_ticker\":\"E\",\"title\":\"Event\",\"category\":\"Test\",\"collateral_return_type\":\"binary\",\"markets\":[" + market + "]}}";
        Assert.Equal(row.FeeWaiverExpirationTime, Assert.Single((await f.Client.Events.GetEventAsync("E", true)).Markets!).FeeWaiverExpirationTime);
    }

    [Theory]
    [InlineData("\"not-a-date\"")]
    [InlineData("\"2026-10-17T10:20:30\"")]
    [InlineData("123")]
    [InlineData("\"\"")]
    public async Task InvalidWaiverMetadataIsAnSdkDeserializationError(string value)
    {
        using var f = new Fixture("{\"market\":{\"ticker\":\"M\",\"event_ticker\":\"E\",\"title\":\"Market\",\"status\":\"active\",\"fee_waiver_expiration_time\":" + value + "}}");
        var error = await Assert.ThrowsAsync<KalshiException>(() => f.Client.Markets.GetMarketAsync("M"));
        Assert.IsType<JsonException>(error.InnerException);
    }

    [Fact]
    public async Task LegacyClientImplementationsRemainSourceCompatible()
    {
        IExchangeClient exchange = new LegacyExchange(); IEventClient events = new LegacyEvents();
        await Assert.ThrowsAsync<NotSupportedException>(() => exchange.GetSeriesFeeChangesAsync());
        await Assert.ThrowsAsync<NotSupportedException>(() => events.GetEventFeeChangesAsync());
    }
    private static async Task Read(Fixture f, bool events, CancellationToken ct = default)
    {
        if (events) await f.Client.Events.GetEventFeeChangesAsync(cancellationToken: ct);
        else await f.Client.Exchange.GetSeriesFeeChangesAsync(cancellationToken: ct);
    }
    private sealed class LegacyExchange : IExchangeClient
    {
        public Task<ExchangeStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExchangeScheduleResponse> GetScheduleAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class LegacyEvents : IEventClient
    {
        public Task<EventResponse> GetEventAsync(string eventTicker, bool? withNestedMarkets = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EventsResponse> ListEventsAsync(EventQuery? query = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Fixture : IDisposable
    {
        public Handler Transport { get; }
        private readonly HttpClient _http;
        public KalshiClient Client { get; }
        public Fixture(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            Transport = new(json, status); _http = new(Transport);
            Client = new(new KalshiHttpClient(_http, Options.Create(new KalshiClientOptions { BaseUri = new Uri("https://offline.invalid") }), NullLogger<KalshiHttpClient>.Instance));
        }
        public void Dispose() { Client.Dispose(); _http.Dispose(); }
    }
    private sealed class Handler(string json, HttpStatusCode status) : HttpMessageHandler
    {
        public string Json { get; set; } = json;
        public string? Path { get; private set; }
        public HttpMethod? Method { get; private set; }
        public HttpContent? Body { get; private set; }
        public bool HasCredentials { get; private set; }
        public int Calls { get; private set; }
        public Func<CancellationToken, Task>? BeforeReply { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); Calls++; Path = request.RequestUri!.PathAndQuery; Method = request.Method; Body = request.Content;
            HasCredentials = request.Headers.Any(header => header.Key.StartsWith("KALSHI-ACCESS-", StringComparison.OrdinalIgnoreCase));
            if (BeforeReply is not null) await BeforeReply(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(Json, Encoding.UTF8, "application/json") };
        }
    }
}
