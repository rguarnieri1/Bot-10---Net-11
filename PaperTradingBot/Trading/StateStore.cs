using System.Text.Json;

namespace PaperTradingBot.Trading;

public static class StateStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static BotState LoadOrCreate(string path, decimal initialCapitalUsd)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<BotState>(json, Options);
            if (loaded is not null) return loaded;
        }

        var now = DateTimeOffset.UtcNow;
        return new BotState
        {
            CashUsd = initialCapitalUsd,
            WeekStartedAt = now,
            WeekStartEquityUsd = initialCapitalUsd
        };
    }

    public static void Save(BotState state, string path)
    {
        var json = JsonSerializer.Serialize(state, Options);
        var tmpPath = path + ".tmp";
        File.WriteAllText(tmpPath, json);
        File.Copy(tmpPath, path, overwrite: true);
        File.Delete(tmpPath);
    }
}
