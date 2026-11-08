namespace PaperTradingBot.Trading;

public enum TradeReason { Signal, StopLoss, TakeProfit, TrendReversal, Shutdown }

public sealed record Trade(
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    string Instrument,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal Quantity,
    decimal PnlUsd,
    decimal PnlFraction,
    decimal FeesUsd,
    TradeReason Reason);
