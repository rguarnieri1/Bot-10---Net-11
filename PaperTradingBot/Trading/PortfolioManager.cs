using PaperTradingBot.Config;

namespace PaperTradingBot.Trading;

public sealed class PortfolioManager
{
    private readonly BotConfig _config;
    private readonly BotState _state;
    private readonly TradeLogger _logger;

    public PortfolioManager(BotConfig config, BotState state, TradeLogger logger)
    {
        _config = config;
        _state = state;
        _logger = logger;
    }

    public bool HasOpenPosition(string instrument) => _state.OpenPositions.ContainsKey(instrument);

    public decimal Equity(IReadOnlyDictionary<string, decimal> currentPrices)
    {
        var positionsValue = _state.OpenPositions
            .Where(kv => currentPrices.ContainsKey(kv.Key))
            .Sum(kv => kv.Value.MarketValue(currentPrices[kv.Key]));
        return _state.CashUsd + positionsValue;
    }

    public void TryOpenPosition(string instrument, decimal price, IReadOnlyDictionary<string, decimal> currentPrices)
    {
        if (HasOpenPosition(instrument)) return;

        var equity = Equity(currentPrices);
        var targetNotional = equity * _config.MaxPositionFraction;
        // Il cash disponibile deve coprire sia il nozionale che la commissione di apertura.
        var maxAffordableNotional = _state.CashUsd / (1 + _config.FeeFraction);
        var sizeUsd = Math.Min(targetNotional, maxAffordableNotional);

        if (sizeUsd < 10m)
        {
            _logger.LogInfo($"{instrument}: segnale di ingresso ignorato, liquidità insufficiente ({_state.CashUsd:F2} USD disponibili)");
            return;
        }

        var quantity = sizeUsd / price;
        var openFee = sizeUsd * _config.FeeFraction;
        var totalCost = sizeUsd + openFee;

        // Il take-profit deve garantire almeno MinProfitUsd di guadagno NETTO (dopo commissione
        // di apertura gia' pagata e commissione di vendita al momento della chiusura), oltre
        // alla soglia percentuale configurata (vince la piu' alta delle due).
        var takeProfitByFraction = price * (1 + _config.TakeProfitFraction);
        var netProceedsNeeded = totalCost + _config.MinProfitUsd;
        var grossProceedsNeeded = netProceedsNeeded / (1 - _config.FeeFraction);
        var takeProfitByMinProfit = grossProceedsNeeded / quantity;
        var takeProfitPrice = Math.Max(takeProfitByFraction, takeProfitByMinProfit);

        var position = new Position
        {
            Instrument = instrument,
            EntryPrice = price,
            Quantity = quantity,
            CostUsd = totalCost,
            OpenFeeUsd = openFee,
            OpenedAt = DateTimeOffset.UtcNow,
            StopLossPrice = price * (1 - _config.StopLossFraction),
            TakeProfitPrice = takeProfitPrice
        };

        _state.CashUsd -= totalCost;
        _state.CumulativeFeesUsd += openFee;
        _state.OpenPositions[instrument] = position;

        _logger.LogPositionEvent($"APERTA {instrument} | prezzo {price:F4} | size {sizeUsd:F2} USD | qty {quantity:F6} | " +
                                  $"commissione {openFee:F4} USD | SL {position.StopLossPrice:F4} | TP {position.TakeProfitPrice:F4}");
    }

    public void ClosePosition(string instrument, decimal exitPrice, TradeReason reason)
    {
        if (!_state.OpenPositions.TryGetValue(instrument, out var position)) return;

        var grossProceeds = position.MarketValue(exitPrice);
        var closeFee = grossProceeds * _config.FeeFraction;
        var netProceeds = grossProceeds - closeFee;
        var pnlUsd = netProceeds - position.CostUsd;
        var pnlFraction = pnlUsd / position.CostUsd;
        var totalFees = position.OpenFeeUsd + closeFee;

        _state.CashUsd += netProceeds;
        _state.CumulativeFeesUsd += closeFee;
        _state.OpenPositions.Remove(instrument);

        var trade = new Trade(position.OpenedAt, DateTimeOffset.UtcNow, instrument,
            position.EntryPrice, exitPrice, position.Quantity, pnlUsd, pnlFraction, totalFees, reason);
        _state.ClosedTrades.Add(trade);
        _logger.LogTrade(trade);
    }

    /// <summary>Controlla stop-loss e take-profit su una posizione aperta. Ritorna il motivo di chiusura se scattato.</summary>
    public TradeReason? CheckRiskExits(string instrument, decimal currentPrice)
    {
        if (!_state.OpenPositions.TryGetValue(instrument, out var position)) return null;

        if (currentPrice <= position.StopLossPrice) return TradeReason.StopLoss;
        if (currentPrice >= position.TakeProfitPrice) return TradeReason.TakeProfit;
        return null;
    }

    /// <summary>
    /// Una chiusura "discrezionale" (es. cambio di trend) e' ammessa solo se realizza almeno
    /// MinProfitUsd di guadagno NETTO (commissione di vendita gia' sottratta). Lo stop-loss
    /// resta indipendente da questa regola: protegge il capitale e deve poter scattare anche
    /// sotto la soglia di guadagno minimo.
    /// </summary>
    public bool MeetsMinProfit(string instrument, decimal currentPrice)
    {
        if (!_state.OpenPositions.TryGetValue(instrument, out var position)) return false;

        var grossProceeds = position.MarketValue(currentPrice);
        var closeFee = grossProceeds * _config.FeeFraction;
        var netPnl = grossProceeds - closeFee - position.CostUsd;
        return netPnl >= _config.MinProfitUsd;
    }

    public void CloseAll(IReadOnlyDictionary<string, decimal> currentPrices, TradeReason reason)
    {
        foreach (var instrument in _state.OpenPositions.Keys.ToList())
        {
            if (currentPrices.TryGetValue(instrument, out var price))
                ClosePosition(instrument, price, reason);
        }
    }

    public void ReportWeeklyPerformanceIfDue(IReadOnlyDictionary<string, decimal> currentPrices)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _state.WeekStartedAt < TimeSpan.FromDays(7)) return;

        var equity = Equity(currentPrices);
        var pnlFraction = (equity - _state.WeekStartEquityUsd) / _state.WeekStartEquityUsd;
        var targetMet = pnlFraction >= _config.WeeklyTargetFraction;

        _logger.LogInfo("==== RIEPILOGO SETTIMANALE ====");
        _logger.LogInfo($"Equity inizio settimana: {_state.WeekStartEquityUsd:F2} USD | Equity attuale: {equity:F2} USD");
        _logger.LogInfo($"Rendimento settimanale: {pnlFraction:P2} (obiettivo: {_config.WeeklyTargetFraction:P0}) -> " +
                         (targetMet ? "OBIETTIVO RAGGIUNTO" : "obiettivo NON raggiunto"));
        _logger.LogInfo("================================");

        _state.WeekStartedAt = now;
        _state.WeekStartEquityUsd = equity;
    }

    public void PrintStatus(IReadOnlyDictionary<string, decimal> currentPrices)
    {
        var equity = Equity(currentPrices);
        var totalPnl = equity - _config.InitialCapitalUsd;
        var totalPnlPct = totalPnl / _config.InitialCapitalUsd;
        _logger.LogInfo($"Equity: {equity:F2} USD | Cash: {_state.CashUsd:F2} USD | Posizioni aperte: {_state.OpenPositions.Count} | " +
                         $"PnL totale: {totalPnl:+0.00;-0.00} USD ({totalPnlPct:P2}) | Trade chiusi: {_state.ClosedTrades.Count} | " +
                         $"Commissioni pagate: {_state.CumulativeFeesUsd:F4} USD");
    }
}
