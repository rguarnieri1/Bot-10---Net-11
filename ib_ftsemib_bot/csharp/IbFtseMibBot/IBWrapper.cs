using System.Collections.Concurrent;
using IBApi;

namespace IbFtseMibBot;

/// Implementazione di EWrapper: riceve tutti i callback dal thread di lettura
/// dell'API IB e li smista in strutture thread-safe che il resto del bot
/// (in esecuzione su un altro thread/async loop) può leggere in modo sicuro.
internal sealed class IBWrapper : EWrapper
{
    public readonly ConcurrentDictionary<int, List<Bar>> Bars = new();
    public readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> Completions = new();
    public readonly TaskCompletionSource<int> Ready = new();

    // Codici di errore puramente informativi inviati da TWS (stato farm dati, ecc.)
    private static readonly HashSet<int> InfoCodes = [2104, 2106, 2107, 2108, 2119, 2158];

    public void error(int id, int errorCode, string errorMsg)
    {
        if (InfoCodes.Contains(errorCode))
        {
            Console.WriteLine($"[IB INFO] {errorMsg}");
            return;
        }

        Console.WriteLine($"[IB ERRORE] reqId={id} code={errorCode} msg={errorMsg}");
        if (Completions.TryGetValue(id, out var tcs))
            tcs.TrySetResult(false);
    }

    public void error(string str) => Console.WriteLine($"[IB ERRORE] {str}");
    public void error(Exception e) => Console.WriteLine($"[IB ERRORE] {e.Message}");

    public void nextValidId(int orderId) => Ready.TrySetResult(orderId);

    public void historicalData(int reqId, Bar bar) =>
        Bars.GetOrAdd(reqId, _ => []).Add(bar);

    public void historicalDataEnd(int reqId, string start, string end)
    {
        if (Completions.TryGetValue(reqId, out var tcs))
            tcs.TrySetResult(true);
    }

    // --- Callback non utilizzati dal bot: implementazioni vuote richieste dall'interfaccia EWrapper ---
    public void accountDownloadEnd(string account) { }
    public void accountSummary(int reqId, string account, string tag, string value, string currency) { }
    public void accountSummaryEnd(int reqId) { }
    public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency) { }
    public void accountUpdateMultiEnd(int requestId) { }
    public void bondContractDetails(int reqId, ContractDetails contract) { }
    public void commissionReport(CommissionReport commissionReport) { }
    public void completedOrder(Contract contract, Order order, OrderState orderState) { }
    public void completedOrdersEnd() { }
    public void connectAck() => Console.WriteLine("[IB] Connessione accettata dal server.");
    public void connectionClosed() => Console.WriteLine("[IB] Connessione chiusa.");
    public void contractDetails(int reqId, ContractDetails contractDetails) { }
    public void contractDetailsEnd(int reqId) { }
    public void currentTime(long time) { }
    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract) { }
    public void displayGroupList(int reqId, string groups) { }
    public void displayGroupUpdated(int reqId, string contractInfo) { }
    public void execDetails(int reqId, Contract contract, Execution execution) { }
    public void execDetailsEnd(int reqId) { }
    public void familyCodes(FamilyCode[] familyCodes) { }
    public void fundamentalData(int reqId, string data) { }
    public void headTimestamp(int reqId, string headTimestamp) { }
    public void histogramData(int reqId, HistogramEntry[] data) { }
    public void historicalDataUpdate(int reqId, Bar bar) { }
    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline) { }
    public void historicalNewsEnd(int requestId, bool hasMore) { }
    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done) { }
    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done) { }
    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done) { }
    public void managedAccounts(string accountsList) { }
    public void marketDataType(int reqId, int marketDataType) { }
    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements) { }
    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions) { }
    public void newsArticle(int requestId, int articleType, string articleText) { }
    public void newsProviders(NewsProvider[] newsProviders) { }
    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState) { }
    public void openOrderEnd() { }
    public void orderBound(long orderId, int apiClientId, int apiOrderId) { }
    public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) { }
    public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value) { }
    public void position(string account, Contract contract, double pos, double avgCost) { }
    public void positionEnd() { }
    public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost) { }
    public void positionMultiEnd(int requestId) { }
    public void realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count) { }
    public void receiveFA(int faDataType, string faXmlData) { }
    public void rerouteMktDataReq(int reqId, int conId, string exchange) { }
    public void rerouteMktDepthReq(int reqId, int conId, string exchange) { }
    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr) { }
    public void scannerDataEnd(int reqId) { }
    public void scannerParameters(string xml) { }
    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes) { }
    public void securityDefinitionOptionParameterEnd(int reqId) { }
    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap) { }
    public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }
    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions) { }
    public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttriblast, string exchange, string specialConditions) { }
    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk) { }
    public void tickByTickMidPoint(int reqId, long time, double midPoint) { }
    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate) { }
    public void tickGeneric(int tickerId, int field, double value) { }
    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData) { }
    public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice) { }
    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs) { }
    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions) { }
    public void tickSize(int tickerId, int field, int size) { }
    public void tickSnapshotEnd(int tickerId) { }
    public void tickString(int tickerId, int field, string value) { }
    public void updateAccountTime(string timestamp) { }
    public void updateAccountValue(string key, string value, string currency, string accountName) { }
    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size) { }
    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth) { }
    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange) { }
    public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName) { }
    public void verifyAndAuthCompleted(bool isSuccessful, string errorText) { }
    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge) { }
    public void verifyCompleted(bool isSuccessful, string errorText) { }
    public void verifyMessageAPI(string apiData) { }
}
