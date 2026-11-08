using System.Text.Json;

namespace IbFtseMibBot;

/// Persiste l'ultimo bar_time notificato per ogni simbolo, per evitare di
/// stampare più volte lo stesso incrocio (equivalente di notified_state.json
/// nella versione Python).
internal static class StateStore
{
    public static Dictionary<string, string> Load()
    {
        if (!File.Exists(Config.StateFile)) return [];
        var json = File.ReadAllText(Config.StateFile);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    public static void Save(Dictionary<string, string> state)
    {
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Config.StateFile, json);
    }
}
