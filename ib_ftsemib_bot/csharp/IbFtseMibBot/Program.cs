using IBApi;
using IbFtseMibBot;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var wrapper = new IBWrapper();
var signal = new EReaderMonitorSignal();
var client = new EClientSocket(wrapper, signal);

Console.WriteLine($"Connessione a {Config.Host}:{Config.Port} (clientId={Config.ClientId})...");
client.eConnect(Config.Host, Config.Port, Config.ClientId);

if (!client.IsConnected())
{
    Console.WriteLine("[ERRORE] Impossibile connettersi. Verifica che TWS o IB Gateway siano aperti " +
                       "con l'API abilitata sulla porta configurata (vedi README.md).");
    return 1;
}

var reader = new EReader(client, signal);
reader.Start();
var readerThread = new Thread(() =>
{
    while (client.IsConnected())
    {
        signal.waitForSignal();
        reader.processMsgs();
    }
}) { IsBackground = true };
readerThread.Start();

var readyTimeout = Task.Delay(TimeSpan.FromSeconds(10));
var readyTask = await Task.WhenAny(wrapper.Ready.Task, readyTimeout);
if (readyTask == readyTimeout)
{
    Console.WriteLine("[ERRORE] Timeout in attesa dell'handshake iniziale con IB.");
    return 1;
}

Console.WriteLine($"Connesso. Watchlist: {Config.Watchlist.Length} titoli. Ctrl+C per uscire.");

var state = StateStore.Load();
int nextReqId = 1;

try
{
    while (!cts.IsCancellationRequested)
    {
        foreach (var symbol in Config.Watchlist)
        {
            if (cts.IsCancellationRequested) break;

            var reqId = nextReqId++;
            var contract = new Contract
            {
                Symbol = symbol,
                SecType = "STK",
                Exchange = Config.Exchange,
                PrimaryExch = Config.PrimaryExchange,
                Currency = Config.Currency,
            };

            var tcs = new TaskCompletionSource<bool>();
            wrapper.Completions[reqId] = tcs;
            wrapper.Bars[reqId] = [];

            client.reqHistoricalData(reqId, contract, "", Config.HistoryDuration, Config.BarSize,
                "TRADES", 1, 1, false, null);

            var timeout = Task.Delay(TimeSpan.FromSeconds(Config.HistoricalRequestTimeoutSeconds));
            var finished = await Task.WhenAny(tcs.Task, timeout);

            wrapper.Completions.TryRemove(reqId, out _);

            if (finished == timeout)
            {
                Console.WriteLine($"[AVVISO] {symbol}: timeout nella richiesta dati storici, salto.");
                wrapper.Bars.TryRemove(reqId, out _);
                continue;
            }

            if (!await tcs.Task)
            {
                // errore già stampato dal wrapper (es. ticker non risolto su BVME)
                wrapper.Bars.TryRemove(reqId, out _);
                continue;
            }

            wrapper.Bars.TryRemove(reqId, out var bars);
            var info = SmaCross.Evaluate(bars ?? [], Config.SmaShort, Config.SmaLong);
            if (info is null) continue;

            if (info.BullishCross && state.GetValueOrDefault(symbol) != info.BarTime)
            {
                Notifier.Print(symbol, info);
                state[symbol] = info.BarTime;
                StateStore.Save(state);
            }
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Config.PollIntervalSeconds), cts.Token);
        }
        catch (TaskCanceledException)
        {
            break;
        }
    }
}
finally
{
    Console.WriteLine("\nDisconnessione da IB...");
    client.eDisconnect(true);
}

return 0;
