using System.Text.Json.Serialization;

namespace PaperTradingBot.MarketData;

public sealed record Candle
{
    [JsonPropertyName("o")] public decimal Open { get; init; }
    [JsonPropertyName("h")] public decimal High { get; init; }
    [JsonPropertyName("l")] public decimal Low { get; init; }
    [JsonPropertyName("c")] public decimal Close { get; init; }
    [JsonPropertyName("v")] public decimal Volume { get; init; }
    [JsonPropertyName("t")] public long TimestampMs { get; init; }

    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(TimestampMs);
}

public sealed record CandlestickResult
{
    [JsonPropertyName("interval")] public string Interval { get; init; } = "";
    [JsonPropertyName("data")] public List<Candle> Data { get; init; } = new();
    [JsonPropertyName("instrument_name")] public string InstrumentName { get; init; } = "";
}

public sealed record CandlestickResponse
{
    [JsonPropertyName("code")] public int Code { get; init; }
    [JsonPropertyName("result")] public CandlestickResult? Result { get; init; }
}

public sealed record TickerEntry
{
    [JsonPropertyName("i")] public string Instrument { get; init; } = "";
    [JsonPropertyName("h")] public decimal High24h { get; init; }
    [JsonPropertyName("l")] public decimal Low24h { get; init; }
    [JsonPropertyName("a")] public decimal LastTradePrice { get; init; }
    [JsonPropertyName("b")] public decimal BestBid { get; init; }
    [JsonPropertyName("k")] public decimal BestAsk { get; init; }
    [JsonPropertyName("v")] public decimal Volume24h { get; init; }
    [JsonPropertyName("t")] public long TimestampMs { get; init; }
}

public sealed record TickersResult
{
    [JsonPropertyName("data")] public List<TickerEntry> Data { get; init; } = new();
}

public sealed record TickersResponse
{
    [JsonPropertyName("code")] public int Code { get; init; }
    [JsonPropertyName("result")] public TickersResult? Result { get; init; }
}
