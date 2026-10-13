# Settlement And Orderbook Contracts

Scope: SDK-only read contracts; no exchange orders, credentials, merge or publication are part of this change.

## Explicit Orderbook Pricing

`OrderBookSubscription.UseYesPrice` is nullable. Null preserves the prior subscription wire shape. True explicitly requests YES-leg pricing on both sides; false explicitly requests the currently documented legacy outcome-leg convention. The selected value survives reconnect replay and is not sent with ticker subscriptions or unsubscribe/update commands. The SDK preserves received snapshot/delta values; consumers must normalize according to their selected convention.

The [official direction contract](https://docs.kalshi.com/getting_started/order_direction.md) defines this option and warns that the default will change in a future announced migration. This patch does not change the SDK default or implement unannounced future behavior.

## Settlement Outcomes

The existing typed `ListSettlementsAsync` endpoint already implements the complete query and response fields from [Get Settlements](https://docs.kalshi.com/api-reference/portfolio/get-settlements.md). The old `OrderSide? MarketResult` representation could not deserialize `scalar`, causing the whole page to fail. New `SettlementOutcome? Outcome` supports all three documented outcomes and uses the existing `market_result` wire field.

`MarketResult` remains a getter/init compatibility projection for YES/NO callers. Initializing it sets the canonical `Outcome`; scalar returns null through the projection. When callers assign both properties, the last initializer/with-expression assignment wins. There is no obsolete warning. SDK JSON serialization emits only one `market_result` field, and omitted outcomes remain null. Unknown future strings still fail deserialization; no forward-compatible tolerance is claimed.

Count and money strings remain unmodified, including fractional quantities and six-decimal USD costs/fees. `Revenue` is total proceeds in cents; `Value` is per-YES-contract payout in cents, not total revenue. Settlement rows contain no subaccount identifier: callers needing ownership must set the request subaccount. Continue pagination on the returned cursor, including empty pages; this patch does not change the shared `PagedResponse.HasMore` behavior across unrelated endpoints.

## Fractional Metadata Audit

Current complete [Get Market](https://docs.kalshi.com/api-reference/market/get-market.md), [multivariate event](https://docs.kalshi.com/api-reference/events/get-multivariate-events.md), and [fixed-point guide](https://docs.kalshi.com/getting_started/fixed_point_migration.md) sources were compared. Price ranges and fixed-point count strings already exist in the SDK. The current full schemas omit `fractional_trading_enabled`, despite older cached examples containing it. No invented capability flag is added. The guide specifies 0.01-contract precision; that is not a substitute for account/market authorization, fee/buying-power checks or a credentialed demo acceptance gate.

Verification assets: SDK mock WebSocket subscribe/reconnect/unsubscribe tests; mixed YES/NO/scalar settlement page, legacy initializers/with expressions, exact decimal strings, null value, explicit primary-subaccount filter and empty-page cursor tests. Actual exchange mutation testing remains disabled.
