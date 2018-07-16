using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;

namespace Exmo
{
    public class ExmoClient : IBrokerClient
    {
        private IBrokerApplication _client;
        private ILogger _logger;
        PeriodicTaskWrapper _keepTimer;
        private CancellationTokenSource _source;

        private AsyncLock _lock = new AsyncLock();

        private const string rest_domain = "https://api.exmo.com/v1";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "Exmo";

        public bool? IsStarted { get; private set; } = false;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public bool? IsConnected => throw new NotImplementedException();

        public ExmoClient(IBrokerApplication client, ILogger logger)
        {
            _source = new CancellationTokenSource();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
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

        public async Task Start()
        {
            IsStarted = true;
            _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);

            _keepTimer = new PeriodicTaskWrapper(_logger, _name);
            await _keepTimer.Start(keepTimer, new PeriodicTaskParams { period = 5000 }, _source.Token);
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

        public async Task<MarketBook?> GetTicker(Instrument instr)
        {
            MarketBook mb = new MarketBook();
            string response = await REST.SendRequest(rest_domain + "/ticker/", "GET");
            var tickersResponse = JsonConvert.DeserializeObject<Dictionary<string,Ticker>>(response);

            foreach (var item in tickersResponse)
            {
                if (item.Key == Utilities.ExmoSymbol(instr))
                {
                    mb.AskPrice = Convert.ToDouble(item.Value.sell_price);
                    mb.BidPrice = Convert.ToDouble(item.Value.buy_price);
                    mb.LastPrice = Convert.ToDouble(item.Value.last_trade);
                    mb.LastSize = Convert.ToDouble(item.Value.vol);
                    mb.LocalTime = DateTime.Now;
                    mb.ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Convert.ToDouble(item.Value.updated));

                    _client.OnReport(Name, SubscribeList[item.Key].pair.Id(), mb);
                }
            }
            return mb;
        }

        public async Task Subscribe(Instrument instr, SubscriptionModel model)
        {
            if (instr == null)
            {
                _logger.Log(LogPriority.Error, "instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, string.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            var symbol = Utilities.ExmoSymbol(instr);
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
            var symbol = Utilities.ExmoSymbol(instr);
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
            await Task.Yield();
            throw new NotSupportedException();
        }

        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return false;
            else return true;
        }
    }
}


