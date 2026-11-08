using System.Net.Http.Json;

namespace PaperTradingBot.MarketData;

public sealed class CryptoComClient
{
    private const string BaseUrl = "https://api.crypto.com/exchange/v1/public/";
    private readonly HttpClient _http;

    public CryptoComClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<List<Candle>> GetCandlesAsync(string instrument, string timeframe, int count, CancellationToken ct)
    {
        var url = $"get-candlestick?instrument_name={instrument}&timeframe={timeframe}&count={count}";
        var response = await _http.GetFromJsonAsync<CandlestickResponse>(url, ct)
            ?? throw new InvalidOperationException($"Risposta vuota per {instrument} candlestick");

        if (response.Code != 0 || response.Result is null)
            throw new InvalidOperationException($"Errore API candlestick per {instrument}: code={response.Code}");

        return response.Result.Data.OrderBy(c => c.TimestampMs).ToList();
    }

    public async Task<TickerEntry> GetTickerAsync(string instrument, CancellationToken ct)
    {
        var url = $"get-tickers?instrument_name={instrument}";
        var response = await _http.GetFromJsonAsync<TickersResponse>(url, ct)
            ?? throw new InvalidOperationException($"Risposta vuota per {instrument} ticker");

        if (response.Code != 0 || response.Result is null || response.Result.Data.Count == 0)
            throw new InvalidOperationException($"Errore API ticker per {instrument}: code={response.Code}");

        return response.Result.Data[0];
    }
}
