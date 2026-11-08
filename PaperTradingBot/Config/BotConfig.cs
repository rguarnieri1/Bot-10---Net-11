namespace PaperTradingBot.Config;

public sealed class BotConfig
{
    public decimal InitialCapitalUsd { get; init; } = 100m;
    public string[] Pairs { get; init; } =
    {
        "BTC_USDT", "ETH_USDT", "SOL_USDT", "ADA_USDT", "XRP_USDT"
    };

    public string CandleTimeframe { get; init; } = "15m";
    public int CandleCount { get; init; } = 100;

    public int EmaFastPeriod { get; init; } = 12;
    public int EmaSlowPeriod { get; init; } = 26;
    public int RsiPeriod { get; init; } = 14;
    public decimal RsiMin { get; init; } = 40m;
    public decimal RsiMax { get; init; } = 70m;

    public decimal MaxPositionFraction { get; init; } = 0.20m;
    public decimal StopLossFraction { get; init; } = 0.03m;
    public decimal TakeProfitFraction { get; init; } = 0.06m;

    /// <summary>Guadagno minimo in USD richiesto prima di chiudere una posizione in utile.</summary>
    public decimal MinProfitUsd { get; init; } = 3m;

    /// <summary>Commissione simulata per lato (acquisto e vendita), es. 0.004 = 0.4%.</summary>
    public decimal FeeFraction { get; init; } = 0.004m;

    public int PollIntervalSeconds { get; init; } = 60;
    public decimal WeeklyTargetFraction { get; init; } = 0.02m;

    public string StateFilePath { get; init; } = "state.json";
    public string TradeLogPath { get; init; } = "trades.csv";
    public string PositionsLogPath { get; init; } = "posizioni.txt";
}
