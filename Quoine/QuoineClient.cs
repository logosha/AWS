using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Shared.Broker;
using Shared.Interfaces;
using Shared.Models;
using Nito.AsyncEx;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Common;
using Shared.Utils;
using System.Linq;

namespace Quoine
{
    public class QuoineClient : IBrokerClient
    {
        private CancellationTokenSource _source;
        private WebSocketWrapper chnl;
        private IBrokerApplication _client;
        private ILogger _logger;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://ws.pusherapp.com/app/2ff981bb060680b5ce97?protocol=7&client=js&version=4.2.1&flash=false";
        private const string rest_domain = "https://api.quoine.com";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "Quoine";

        public bool? IsStarted { get; private set; } = false;

        public bool? IsConnected
        {
            get
            {
                return (chnl == null ? false : chnl.IsConnected);
            }
        }
        public string Name
        {
            get
            {
                return _name;
            }
        }

        public QuoineClient(IBrokerApplication client, ILogger logger)
        {
            _source = new CancellationTokenSource();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
        }

        public async Task Start()
        {
            IsStarted = true;
            chnl = new WebSocketWrapper(ws_domain, "", _logger, Name);
            await chnl.Start(ParseMessage);
        }

        public async Task Stop()
        {
            var source = _source;
            source?.Cancel();
            using (await _lock.LockAsync())
            {
                if (SubscribeList != null)
                {
                    SubscribeList.Clear();
                }
            }
            IsStarted = false;
            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

        private async Task ParseMessage(SocketEventHandlerArgs msg)
        {
            switch (msg.type)
            {
                case StreamMessageType.Data:
                    {
                        var jsonResponse = Encoding.UTF8.GetString(msg.msg, 0, msg.msg.Length);
                        if (jsonResponse == "[]")
                            return;

                        string evnt = "";
                        JObject jToken = JObject.Parse(jsonResponse);
                        evnt = jToken["event"]?.Value<string>();
                        var data = jToken["data"].ToString();

                        if (evnt == "updated")
                        {
                            var result = JsonConvert.DeserializeObject<Ticker>(data);
                            string symbol = result.currency_pair_code.ToLower();

                            Subscribe dataItem;
                            if (SubscribeList.TryGetValue(symbol, out dataItem))
                            {
                                MarketBook mb = new MarketBook()
                                {
                                    AskPrice = Convert.ToDouble(result.market_ask),
                                    BidPrice = Convert.ToDouble(result.market_bid),
                                    LastPrice = Convert.ToDouble(result.last_traded_price),
                                    LastSize = Convert.ToDouble(result.last_traded_quantity),
                                    LocalTime = DateTime.Now,
                                };
                                _client.OnReport(Name, dataItem.pair.Id(), mb);
                            }
                        }
                    }
                    break;
                case StreamMessageType.Error:
                    break;
                case StreamMessageType.Logon:
                    {
                        var _pairs = await GetProducts();

                        using (await _lock.LockAsync())
                        {
                            foreach (var item in SubscribeList)
                            {
                                var symbol = item.Key;
                                var chnlId = _pairs[symbol];
                                var req = "product_cash_" + symbol + "_" + chnlId;
                                var request = $"{{\"event\":\"pusher:subscribe\",\"data\":{{\"channel\":\"{req}\"}}}}";
                                await chnl.SendCommand(Encoding.UTF8.GetBytes(request));
                                _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);
                                _logger.Log(LogPriority.Info, "Coin Subscribed - " + symbol, Name);
                            }
                        }
                        _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);

                    }
                    break;
                case StreamMessageType.Logout:
                    _client.OnEvent(Name, BrokerEvent.SessionLogout, "");
                    break;
                default:
                    break;
            }
        }

        public async Task<MarketBook?> GetTicker(Instrument instr)
        {
            await Task.Yield();
            throw new NotSupportedException();
        }

        public async Task Subscribe(Instrument instr, SubscriptionModel model)
        {
            if (instr == null)
            {
                _logger.Log(LogPriority.Error, "instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, string.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            if (IsConnected == false)
            {
                _logger.Log(LogPriority.Error, $"Subscribe: {Name} is not connected", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, instr.Id());
                throw new Exception($"{Name} is not connected");
            }
            var _pairs = await GetProducts();

            var symbol = Utilities.QuoineSymbol(instr);
            var chnlId = _pairs[symbol];
            var req = "product_cash_"+symbol+"_"+chnlId;
            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol))
                {
                    var request = $"{{\"event\":\"pusher:subscribe\",\"data\":{{\"channel\":\"{req}\"}}}}";
                    await chnl.SendCommand(Encoding.UTF8.GetBytes(request));
                    SubscribeList.Add(symbol, new Subscribe() { pair = instr, mode = SubscriptionModel.TopBook });
                    _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);
                    _logger.Log(LogPriority.Info, "Coin Subscribed - " + symbol, Name);
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, "Subscribe on " + symbol + " already created");
            }
        }

        public async Task Unsibscribe(Instrument instr, SubscriptionModel model)
        {
            if (instr == null)
            {
                _logger.Log(LogPriority.Error, "instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, string.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            if (IsConnected == false)
            {
                _logger.Log(LogPriority.Error, $"Unsubscribe: {Name} is not connected", Name);
                _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, instr.Id());
                throw new Exception($"{Name} is not connected");
            }

            var _pairs = await GetProducts();

            var symbol = Utilities.QuoineSymbol(instr);
            var chnlId = _pairs[symbol];
            var req = "product_cash_" + symbol + "_" + chnlId;

            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol))
                {
                    var request = $"{{\"event\":\"pusher:unsubscribe\",\"data\":{{\"channel\":\"{req}\"}}}}";
                    await chnl.SendCommand(Encoding.UTF8.GetBytes(request));
                    SubscribeList.Remove(symbol);
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, symbol);
                    _logger.Log(LogPriority.Info, "Coin Unsubscribed - " + symbol, Name);
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, "Unsubscribe for " + symbol + " not exists");
            }
        }

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            await Task.Yield();
            throw new NotSupportedException();
        }

        public async Task<Dictionary<string, int>> GetProducts()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            string response = await REST.SendRequest(rest_domain + "/products", "GET");
            var products = JsonConvert.DeserializeObject<List<Product>>(response);

            foreach(var item in products)
            {
                result.Add(item.currency_pair_code.ToLower(), Convert.ToInt16(item.id));
            }
         
            _client.OnEvent(Name, BrokerEvent.Info, $"-->sent 'GetProducts'");
            return result;
        }

        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return true;
            else return false;
        }
    }
}

