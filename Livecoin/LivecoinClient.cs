using Newtonsoft.Json;
using Nito.AsyncEx;
using Shared.Broker;
using Shared.Interfaces;
using Shared.Models;
using Shared.Utils;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Livecoin
{
    public class LivecoinClient : IBrokerClient
    {
        private IBrokerApplication _client;
        private ILogger _logger;
        PeriodicTaskWrapper _keepTimer;
        private CancellationTokenSource _source;

        private AsyncLock _lock = new AsyncLock();

        private const string rest_domain = "https://api.livecoin.net";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "Livecoin";

        public LivecoinClient(IBrokerApplication client, ILogger logger)
        {
            _source = new CancellationTokenSource();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
        }

        public async Task Start()
        {
            IsStarted = true;
            _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);

            _keepTimer = new PeriodicTaskWrapper(_logger, _name);
            await _keepTimer.Start(keepTimer, new PeriodicTaskParams { period = 5000 }, _source.Token);
        }

        public async Task keepTimer(object o, CancellationToken token)
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

        public async Task Stop()
        {
            using (await _lock.LockAsync())
            {
                if (SubscribeList.Count > 0)
                {
                    SubscribeList.Clear();
                }
            }
            _source.Cancel();
            _keepTimer.Task.Wait();

            IsStarted = false;
            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

        public bool? IsStarted { get; private set; } = false;

        public bool? IsConnected => throw new NotImplementedException();

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            string response = await REST.SendRequest(rest_domain + "/exchange/restrictions", "GET");
            var tickersResponse = JsonConvert.DeserializeObject<Instruments>(response);

                IEnumerable<Instrument> result = tickersResponse.restrictions.Select(u => new Instrument()
                {
                    Exchange = _name,
                    Symbol = u.currencyPair,
                    First = u.currencyPair.Split('/')[0],
                    Second = u.currencyPair.Split('/')[1]
                });
          
            _client.OnEvent(Name, BrokerEvent.Info, $"-->sent 'GetInstruments'");
            return result;
        }

        public async Task<MarketBook?> GetTicker(Instrument instr)
        {
            MarketBook result = new MarketBook();
            string response = await REST.SendRequest(rest_domain +"/exchange/ticker?currencyPair=" + instr.First + "/" + instr.Second, "GET");

            var tickersResponse = JsonConvert.DeserializeObject<TickerResponse>(response);

            result.AskPrice = tickersResponse.best_ask;
            result.BidPrice = tickersResponse.best_bid;
            result.LastPrice = tickersResponse.last;
            result.LastSize = tickersResponse.volume;
            result.LocalTime = DateTime.Now;

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

            var symbol = Utilities.LivecoinSymbol(instr);

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
            var symbol = Utilities.LivecoinSymbol(instr);

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

        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return false;
            else return true;
        }
    }
}


