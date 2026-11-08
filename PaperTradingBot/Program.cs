using PaperTradingBot.Config;
using PaperTradingBot.MarketData;
using PaperTradingBot.Strategy;
using PaperTradingBot.Trading;

var config = new BotConfig();
var logger = new TradeLogger(config.TradeLogPath, config.PositionsLogPath);
var state = StateStore.LoadOrCreate(config.StateFilePath, config.InitialCapitalUsd);
var portfolio = new PortfolioManager(config, state, logger);
var strategy = new EmaCrossoverStrategy(config);
var client = new CryptoComClient(new HttpClient());

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

logger.LogInfo("=== Paper Trading Bot avviato ===");
logger.LogInfo($"Capitale iniziale: {config.InitialCapitalUsd:F2} USD | Coppie: {string.Join(", ", config.Pairs)}");
logger.LogInfo($"Strategia: EMA({config.EmaFastPeriod}/{config.EmaSlowPeriod}) crossover + filtro RSI({config.RsiPeriod}) [{config.RsiMin}-{config.RsiMax}]");
logger.LogInfo($"Risk: max {config.MaxPositionFraction:P0} equity/posizione | stop-loss {config.StopLossFraction:P0} | take-profit {config.TakeProfitFraction:P0} (min {config.MinProfitUsd:F2} USD netti) | commissione simulata {config.FeeFraction:P2} per lato");
logger.LogInfo("NOTA: questo e' un bot di simulazione (paper trading). Nessun ordine reale viene inviato all'exchange.");
logger.LogInfo("Il 2% settimanale e' un obiettivo di monitoraggio, non una garanzia: il risultato dipende dal mercato.");

while (!cts.IsCancellationRequested)
{
    var currentPrices = new Dictionary<string, decimal>();

    foreach (var instrument in config.Pairs)
    {
        try
        {
            var ticker = await client.GetTickerAsync(instrument, cts.Token);
            var price = ticker.LastTradePrice;
            currentPrices[instrument] = price;

            var riskExit = portfolio.CheckRiskExits(instrument, price);
            if (riskExit is not null)
            {
                portfolio.ClosePosition(instrument, price, riskExit.Value);
                continue;
            }

            var candles = await client.GetCandlesAsync(instrument, config.CandleTimeframe, config.CandleCount, cts.Token);
            var signal = strategy.Evaluate(candles);

            switch (signal.Type)
            {
                case SignalType.Enter:
                    portfolio.TryOpenPosition(instrument, price, currentPrices);
                    break;
                case SignalType.ExitTrend when portfolio.HasOpenPosition(instrument):
                    if (portfolio.MeetsMinProfit(instrument, price))
                        portfolio.ClosePosition(instrument, price, TradeReason.TrendReversal);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            logger.LogInfo($"ERRORE su {instrument}: {ex.Message}");
        }
    }

    if (currentPrices.Count > 0)
    {
        portfolio.PrintStatus(currentPrices);
        portfolio.ReportWeeklyPerformanceIfDue(currentPrices);
        StateStore.Save(state, config.StateFilePath);
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(config.PollIntervalSeconds), cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

logger.LogInfo("Arresto richiesto: salvataggio stato in corso...");
StateStore.Save(state, config.StateFilePath);
logger.LogInfo("=== Paper Trading Bot arrestato ===");
