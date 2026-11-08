namespace PaperTradingBot.Trading;

public sealed class BotState
{
    public decimal CashUsd { get; set; }
    public Dictionary<string, Position> OpenPositions { get; set; } = new();
    public List<Trade> ClosedTrades { get; set; } = new();
    public DateTimeOffset WeekStartedAt { get; set; }
    public decimal WeekStartEquityUsd { get; set; }
    public decimal CumulativeFeesUsd { get; set; }
}
