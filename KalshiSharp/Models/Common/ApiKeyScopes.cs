namespace KalshiSharp.Models.Common;

/// <summary>Documented API key scope values.</summary>
public static class ApiKeyScopes
{
    /// <summary>Broad read access.</summary>
    public const string Read = "read";

    /// <summary>Broad write access. Requests with this scope must also include <see cref="Read"/>.</summary>
    public const string Write = "write";

    /// <summary>Read access for block-trade acceptance.</summary>
    public const string ReadBlockTradeAccept = "read::block_trade_accept";

    /// <summary>Read access for portfolio balance.</summary>
    public const string ReadPortfolioBalance = "read::portfolio_balance";

    /// <summary>Write access for trade endpoints.</summary>
    public const string WriteTrade = "write::trade";

    /// <summary>Write access for transfer endpoints.</summary>
    public const string WriteTransfer = "write::transfer";

    /// <summary>Write access for block-trade acceptance.</summary>
    public const string WriteBlockTradeAccept = "write::block_trade_accept";
}
