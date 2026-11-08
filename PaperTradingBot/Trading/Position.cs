namespace PaperTradingBot.Trading;

public sealed class Position
{
    public required string Instrument { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal Quantity { get; init; }

    /// <summary>Capitale investito, comprensivo della commissione pagata in apertura.</summary>
    public required decimal CostUsd { get; init; }
    public required decimal OpenFeeUsd { get; init; }
    public required DateTimeOffset OpenedAt { get; init; }
    public required decimal StopLossPrice { get; init; }
    public required decimal TakeProfitPrice { get; init; }

    public decimal MarketValue(decimal currentPrice) => Quantity * currentPrice;
}
