using System.Globalization;

namespace PaperTradingBot.Trading;

public sealed class TradeLogger
{
    private readonly string _csvPath;
    private readonly string _positionsLogPath;

    public TradeLogger(string csvPath, string positionsLogPath)
    {
        _csvPath = csvPath;
        _positionsLogPath = positionsLogPath;
        if (!File.Exists(_csvPath))
            File.WriteAllText(_csvPath, "OpenedAt,ClosedAt,Instrument,EntryPrice,ExitPrice,Quantity,PnlUsd,PnlFraction,FeesUsd,Reason\n");
    }

    public void LogInfo(string message) =>
        Console.WriteLine($"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}Z] {message}");

    /// <summary>Registra un evento di apertura/chiusura posizione sia a console che in posizioni.txt.</summary>
    public void LogPositionEvent(string message)
    {
        var line = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}Z] {message}";
        Console.WriteLine(line);
        File.AppendAllText(_positionsLogPath, line + Environment.NewLine);
    }

    public void LogTrade(Trade trade)
    {
        var line = string.Join(",",
            trade.OpenedAt.ToString("O"),
            trade.ClosedAt.ToString("O"),
            trade.Instrument,
            trade.EntryPrice.ToString(CultureInfo.InvariantCulture),
            trade.ExitPrice.ToString(CultureInfo.InvariantCulture),
            trade.Quantity.ToString(CultureInfo.InvariantCulture),
            trade.PnlUsd.ToString(CultureInfo.InvariantCulture),
            trade.PnlFraction.ToString(CultureInfo.InvariantCulture),
            trade.FeesUsd.ToString(CultureInfo.InvariantCulture),
            trade.Reason);

        File.AppendAllText(_csvPath, line + "\n");

        var sign = trade.PnlUsd >= 0 ? "+" : "";
        LogPositionEvent($"CHIUSA {trade.Instrument} | entry {trade.EntryPrice:F4} -> exit {trade.ExitPrice:F4} | " +
                          $"PnL netto {sign}{trade.PnlUsd:F4} USD ({sign}{trade.PnlFraction:P2}) | " +
                          $"commissioni {trade.FeesUsd:F4} USD | motivo: {trade.Reason}");
    }
}
