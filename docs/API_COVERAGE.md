# KalshiSharp API Coverage

KalshiSharp tracks Kalshi's public Trade API with typed .NET 8 clients, request models, response
models, and focused compatibility tests. Coverage is intentionally additive and source-compatible
where optional clients are introduced.

## REST Coverage

| API surface | SDK coverage |
|-------------|--------------|
| Exchange | Status and schedule |
| Account | Endpoint and token-cost discovery |
| Markets | Listing, details, order books, trades, candlesticks, localized responses, settlement bounds, dynamic price ranges |
| Events | Listing, details, nested markets, categories, lifecycle-related market metadata |
| Series | List and get with discovery categories, product metadata, and volume |
| Structured targets and milestones | List/get endpoints for live-data and event discovery |
| Multivariate collections | List/get collections with associated events, price-level structures, and price ranges |
| Orders V2 | Create, amend, decrease, cancel, cancel all, batch create/cancel, queue positions, and multiple order books |
| Order groups | List, get, create, update limit, trigger, reset, and delete |
| Portfolio | Balance, positions, fills, settlements, subaccount transfers, intra-account transfers, target allocation, and resting order value |
| Historical data | Cutoff, markets, candlesticks, trades, fills, orders, and positions |
| Incentive programs | Liquidity, margin maker-volume, and margin taker-volume program shapes |
| Live data | Generic, legacy, batch, event, game-stat, weather index, and weather calibration endpoints |
| API keys | List, create, generate, and delete API keys |
| Communications | Communications identity, block-trade proposals, RFQ list/create/read/delete, quote list/create/read/delete/accept/confirm |

## WebSocket Coverage

| Channel family | SDK coverage |
|----------------|--------------|
| Public market streams | Order book, ticker, trades, lifecycle, and multivariate lifecycle messages |
| Private account streams | Fills, positions, user orders, order groups, and communications RFQ/quote notifications |
| Connection behavior | Typed message dispatch, reconnect policy, replay support, cancellation, and unknown-message preservation |

## Notable Tracked Gaps

- CF Benchmarks remains blocked on a stale PR with unresolved replay and correlation findings.
- Ed25519 key support is security-sensitive and needs explicit signing design before implementation.

Use GitHub issues for the current authoritative backlog.
