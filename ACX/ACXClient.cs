using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shared.Utils;
using System.Globalization;
using Shared.Broker;
using Shared.Interfaces;
using Shared.Models;
using Shared.Common;
using Nito.AsyncEx;
using System.Diagnostics;

namespace ACX
{
    public class ACXClient : IBrokerClient
    {
        private IBrokerApplication _client;
        private ILogger _logger;
        PeriodicTaskWrapper _keepTimer;
        private CancellationTokenSource _source;

        private AsyncLock _lock = new AsyncLock();

        private const string rest_domain = "https://acx.io//api/v2";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "ACX";

        public bool? IsStarted { get; private set; } = false;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public bool? IsConnected => throw new NotImplementedException();

        public ACXClient(IBrokerApplication client, ILogger logger)
        {
            _source = new CancellationTokenSource();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
        }

        public async Task KeepTimer(object o, CancellationToken token)
        {         
            using (await _lock.LockAsync())
            {
                foreach (var item in SubscribeList.Values)
                {
                    if (item.mode == SubscriptionModel.TopBook)
                    try
                    {
                        await GetTicker(item.pair);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Object reference not set to an instance of an object.")
                        {
                            _client.OnEvent(Name, BrokerEvent.SubscribedCoinIsNotValid, "Pair: " + item.pair + "is not valid");
                            _logger.Log(LogPriority.Info, "SubscribedCoinIsNotValid - Pair: " + item.pair + "is not valid", Name);
                        }
                    }
                }
            }
        }

        public async Task Start()
        {
            IsStarted = true;
            _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);

            _keepTimer = new PeriodicTaskWrapper(_logger, _name);
            await _keepTimer.Start(KeepTimer, new PeriodicTaskParams { period = 5000 }, _source.Token);
        }

        public async Task Stop()
        {
            using (await _lock.LockAsync())
            {
                if (SubscribeList.Count > 0)
                {
                    SubscribeList.Clear();
                }
            }

            var source = _source;
            source.Cancel();
            var t = _keepTimer.Task;
            t.Wait();
            IsStarted = false;

            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

        public async Task<MarketBook?> GetTicker(Instrument instr)
        {
            MarketBook result = new MarketBook();
            string response = await REST.SendRequest(rest_domain + "/tickers/" + Utilities.ACXSymbol(instr), "GET");

            var tickersResponse = JsonConvert.DeserializeObject<TickerResponse>(response);

            result.AskPrice = Convert.ToDouble(tickersResponse.ticker.sell);
            result.BidPrice = Convert.ToDouble(tickersResponse.ticker.buy);
            result.LastPrice = Convert.ToDouble(tickersResponse.ticker.last);
            result.LastSize = Convert.ToDouble(tickersResponse.ticker.vol);
            result.LocalTime = DateTime.Now;
            result.ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToDouble(tickersResponse.at));
           
            _client.OnReport(Name, instr.Id(), result);
            return result;
        }

        public async Task Subscribe(Instrument instr, SubscriptionModel model)
        {
            if (instr == null)
            {
                _logger.Log(LogPriority.Error, "instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, string.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            var symbol = Utilities.ACXSymbol(instr);
            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        SubscribeList.Add(symbol, new Subscribe() { mode = SubscriptionModel.TopBook, pair = instr });
                        _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);
                        _logger.Log(LogPriority.Info, "Coin Subscribed - " + symbol, Name);
                    }
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, "Subscribe on " + symbol + " already created");
            }
        }

        public async Task Unsibscribe(Instrument instr, SubscriptionModel model)
        {
            Debug.Assert(instr != null);
            var symbol = Utilities.ACXSymbol(instr);
            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol))
                {
                    SubscribeList.Remove(symbol);
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, symbol);
                    _logger.Log(LogPriority.Info, "Coin Unsubscribed - " + symbol, Name);
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, "Unsubscribe: {instr.First}-{instr.Second} - is not subscribed");
            }
        }

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            string response = await REST.SendRequest(rest_domain + "/markets", "GET");
            var instrumentResponse = JsonConvert.DeserializeObject<List<InstrumentResponse>>(response);
            IEnumerable<Instrument> result = instrumentResponse.Select(u => new Instrument()
            {
                Exchange = _name,
                Symbol = u.base_unit + u.quote_unit,
                First = u.base_unit,
                Second = u.quote_unit
            });
            _client.OnEvent(Name, BrokerEvent.Info, $"-->sent 'GetInstruments'");
            return result;
        }

        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return false;
            else return true;
        }
    }
}


