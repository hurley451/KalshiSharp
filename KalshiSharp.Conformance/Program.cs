using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using KalshiSharp.Models.Requests;
using KalshiSharp.Models.Responses;
using KalshiSharp.Models.WebSocket;
using KalshiSharp.Serialization;

var kalshiCoreRoot = ResolveKalshiCoreRoot(args);
var targetManifest = LoadJson(Path.Combine(kalshiCoreRoot, "targets", "csharp", "KalshiSharp.json"));
var fixtureIds = targetManifest["conformance"]?["fixtureIds"]?.AsArray()
    .Select(node => node?.GetValue<string>())
    .Where(id => !string.IsNullOrWhiteSpace(id))
    .Cast<string>()
    .ToArray() ?? [];

var fixtureIndex = Directory
    .EnumerateFiles(Path.Combine(kalshiCoreRoot, "fixtures"), "*.json", SearchOption.AllDirectories)
    .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}schemas{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
    .Select(path => (Path: path, Document: LoadJson(path)))
    .ToDictionary(item => item.Document["id"]?.GetValue<string>() ?? string.Empty, item => item);

var checks = new List<CheckResult>();
foreach (var fixtureId in fixtureIds)
{
    if (!fixtureIndex.TryGetValue(fixtureId, out var fixture))
    {
        checks.Add(CheckResult.Fail(fixtureId, $"Fixture {fixtureId} was not found."));
        continue;
    }

    checks.Add(RunFixture(fixtureId, fixture.Document));
}

var report = new ConformanceReport(
    TargetId: targetManifest["id"]?.GetValue<string>() ?? "csharp.KalshiSharp",
    KalshiCoreRoot: kalshiCoreRoot,
    GeneratedAtUtc: DateTimeOffset.UtcNow,
    Checks: checks);

var outputPath = ResolveOutputPath(args);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, JsonSerializer.Serialize(report, ReportSerialization.Options));

foreach (var check in checks)
{
    Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")} {check.Id}: {check.Message}");
}

Console.WriteLine($"Report: {outputPath}");

if (checks.Any(check => !check.Passed))
{
    Environment.ExitCode = 1;
}

static CheckResult RunFixture(string fixtureId, JsonObject fixture)
{
    try
    {
        return fixture["kind"]?.GetValue<string>() switch
        {
            "auth-signing" => RunAuthSigningFixture(fixtureId, fixture),
            "rest-request-serialization" => RunRestRequestFixture(fixtureId, fixture),
            "rest-response-shape" => RunRestResponseFixture(fixtureId, fixture),
            "websocket-replay" => RunWebSocketReplayFixture(fixtureId, fixture),
            var kind => CheckResult.Fail(fixtureId, $"Unsupported fixture kind {kind}.")
        };
    }
    catch (Exception ex)
    {
        return CheckResult.Fail(fixtureId, ex.Message);
    }
}

static CheckResult RunAuthSigningFixture(string fixtureId, JsonObject fixture)
{
    var input = fixture["input"]!.AsObject();
    var expected = fixture["expected"]!.AsObject();
    var timestampMs = input["timestampMs"]!.GetValue<string>();
    var method = input["method"]!.GetValue<string>().ToUpperInvariant();
    var path = input["path"]!.GetValue<string>();
    var canonicalPath = path.Split('?', 2)[0];
    var message = string.Concat(timestampMs, method, canonicalPath);

    ExpectEqual(expected["canonicalPath"]!.GetValue<string>(), canonicalPath, "canonical path");
    ExpectEqual(expected["signatureMessage"]!.GetValue<string>(), message, "signature message");
    ExpectEqual(expected["headers"]!["KALSHI-ACCESS-KEY"]!.GetValue<string>(), input["apiKeyId"]!.GetValue<string>(), "access key header");
    ExpectEqual(expected["headers"]!["KALSHI-ACCESS-TIMESTAMP"]!.GetValue<string>(), timestampMs, "timestamp header");

    return CheckResult.Pass(fixtureId, "RSA-PSS signing canonical message matched.");
}

static CheckResult RunRestRequestFixture(string fixtureId, JsonObject fixture)
{
    var input = fixture["input"]!.AsObject();
    var query = input["query"]!.AsObject();
    var fillQuery = new FillQuery
    {
        Tickers = query["tickers"]?.AsArray().Select(node => node!.GetValue<string>()).ToArray(),
        Limit = query["limit"]?.GetValue<int>(),
        Cursor = query["cursor"]?.GetValue<string>()
    };

    var pathAndQuery = input["path"]!.GetValue<string>() + fillQuery.ToQueryString();
    var expected = fixture["expected"]!.AsObject();

    ExpectEqual(expected["method"]!.GetValue<string>(), input["method"]!.GetValue<string>(), "HTTP method");
    ExpectEqual(expected["pathAndQuery"]!.GetValue<string>(), pathAndQuery, "path and query");
    if (expected["body"] is not null && expected["body"]!.GetValueKind() != JsonValueKind.Null)
    {
        throw new InvalidOperationException("Expected body must be null for portfolio fills query serialization.");
    }

    return CheckResult.Pass(fixtureId, "Portfolio fills request serialization matched.");
}

static CheckResult RunRestResponseFixture(string fixtureId, JsonObject fixture)
{
    var body = fixture["input"]!["body"]!.ToJsonString();
    var response = JsonSerializer.Deserialize<FillsResponse>(body, KalshiJsonOptions.Default)
        ?? throw new InvalidOperationException("Fills response deserialized to null.");
    var expected = fixture["expected"]!.AsObject();
    if (response.Fills.Count == 0)
    {
        throw new InvalidOperationException("Fills response did not contain a first fill.");
    }

    var firstFill = response.Fills[0];
    var expectedFirstFill = expected["firstFill"]!.AsObject();

    ExpectEqual(expected["fillsCount"]!.GetValue<int>(), response.Fills.Count, "fills count");
    ExpectEqual(expectedFirstFill["fillId"]!.GetValue<string>(), firstFill.FillId, "fill id");
    ExpectEqual(expectedFirstFill["exchangeIndex"]!.GetValue<int>(), firstFill.ExchangeIndex, "exchange index");
    ExpectEqual(expectedFirstFill["tradeId"]!.GetValue<string>(), firstFill.TradeId, "trade id");
    ExpectEqual(expectedFirstFill["orderId"]!.GetValue<string>(), firstFill.OrderId, "order id");
    ExpectEqual(expectedFirstFill["ticker"]!.GetValue<string>(), firstFill.Ticker, "ticker");
    ExpectEqual(expectedFirstFill["marketTicker"]!.GetValue<string>(), firstFill.MarketTicker, "market ticker");
    ExpectEqual(expectedFirstFill["outcomeSide"]!.GetValue<string>(), firstFill.OutcomeSide?.ToString().ToLowerInvariant(), "outcome side");
    ExpectEqual(expectedFirstFill["bookSide"]!.GetValue<string>(), firstFill.BookSide?.ToString().ToLowerInvariant(), "book side");
    ExpectEqual(expectedFirstFill["countFp"]!.GetValue<string>(), firstFill.CountFp, "count fp");
    ExpectEqual(expectedFirstFill["yesPriceDollars"]!.GetValue<string>(), firstFill.YesPriceDollars, "yes price dollars");
    ExpectEqual(expectedFirstFill["noPriceDollars"]!.GetValue<string>(), firstFill.NoPriceDollars, "no price dollars");
    ExpectEqual(expectedFirstFill["feeCost"]!.GetValue<string>(), firstFill.FeeCost, "fee cost");
    ExpectEqual(expectedFirstFill["side"]!.GetValue<string>(), firstFill.Side.ToString().ToLowerInvariant(), "side");
    ExpectEqual(expectedFirstFill["action"]!.GetValue<string>(), firstFill.Action, "action");
    ExpectEqual(expectedFirstFill["isTaker"]!.GetValue<bool>(), firstFill.IsTaker, "is taker");
    ExpectEqual(expectedFirstFill["subaccountNumber"]!.GetValue<int>(), firstFill.SubaccountNumber, "subaccount number");
    ExpectEqual(expectedFirstFill["ts"]!.GetValue<long>(), firstFill.Ts, "ts");
    ExpectEqual(expected["cursor"]!.GetValue<string>(), response.Cursor, "cursor");

    return CheckResult.Pass(fixtureId, "Portfolio fills response shape deserialized.");
}

static CheckResult RunWebSocketReplayFixture(string fixtureId, JsonObject fixture)
{
    var messages = fixture["input"]!["messages"]!.AsArray();
    var dispatchedTypes = new List<string>();
    OrderBookUpdate? lastBookUpdate = null;

    foreach (var messageNode in messages)
    {
        var message = JsonSerializer.Deserialize<WebSocketMessage>(messageNode!.ToJsonString(), KalshiJsonOptions.Default)
            ?? throw new InvalidOperationException("WebSocket message deserialized to null.");

        dispatchedTypes.Add(message.Type);
        if (message is OrderBookUpdate orderBookUpdate)
        {
            lastBookUpdate = orderBookUpdate;
        }
    }

    var expected = fixture["expected"]!.AsObject();
    var expectedTypes = expected["dispatchedTypes"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
    ExpectEqual(string.Join(",", expectedTypes), string.Join(",", dispatchedTypes), "dispatched types");

    var patch = expected["finalBookPatch"]!.AsObject();
    var expectedTicker = patch["ticker"]!.GetValue<string>();
    var expectedYes = patch["yes"]!.AsObject();
    var expectedPrice = expectedYes.Single().Key;
    var expectedDelta = expectedYes.Single().Value!.GetValue<int>();

    if (lastBookUpdate is null)
    {
        throw new InvalidOperationException("Replay did not dispatch an order book update.");
    }

    ExpectEqual(expectedTicker, lastBookUpdate.Message.MarketTicker, "order book ticker");
    ExpectEqual(int.Parse(expectedPrice, CultureInfo.InvariantCulture), lastBookUpdate.Message.Price, "order book price");
    ExpectEqual(expectedDelta, lastBookUpdate.Message.Delta, "order book delta");
    ExpectEqual(true, lastBookUpdate.Message.IsYesSide, "order book yes side");

    return CheckResult.Pass(fixtureId, "WebSocket replay dispatched expected messages.");
}

static JsonObject LoadJson(string path)
{
    using var stream = File.OpenRead(path);
    return JsonNode.Parse(stream)?.AsObject()
        ?? throw new InvalidOperationException($"Unable to parse JSON object at {path}.");
}

static string ResolveKalshiCoreRoot(string[] args)
{
    var fromArg = ReadOption(args, "--kalshicore-root");
    if (!string.IsNullOrWhiteSpace(fromArg))
    {
        return Path.GetFullPath(fromArg);
    }

    var fromEnvironment = Environment.GetEnvironmentVariable("KALSHICORE_ROOT");
    if (!string.IsNullOrWhiteSpace(fromEnvironment))
    {
        return Path.GetFullPath(fromEnvironment);
    }

    throw new InvalidOperationException("Set KALSHICORE_ROOT or pass --kalshicore-root <path>.");
}

static string ResolveOutputPath(string[] args)
{
    var fromArg = ReadOption(args, "--report");
    return Path.GetFullPath(fromArg ?? Path.Combine("artifacts", "kalshicore-conformance", "report.json"));
}

static string? ReadOption(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void ExpectEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name} mismatch. Expected {expected}, got {actual}.");
    }
}

internal sealed record ConformanceReport(
    string TargetId,
    string KalshiCoreRoot,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<CheckResult> Checks);

internal sealed record CheckResult(string Id, bool Passed, string Message)
{
    public static CheckResult Pass(string id, string message) => new(id, true, message);

    public static CheckResult Fail(string id, string message) => new(id, false, message);
}

internal static class ReportSerialization
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
