using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Shared.Broker;
using Shared.Interfaces;
using Shared.Models;
using Nito.AsyncEx;
using Shared.Utils;
using Shared.Common;

namespace GDAX
{
    public class GDAXClient : IBrokerClient
    {
        private CancellationTokenSource _source = new CancellationTokenSource();

        private IBrokerApplication _client;
        private ILogger _logger;
        private WebSocketWrapper chnl;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://ws-feed.gdax.com";
        private const string rest_domain = "https://api.gdax.com";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "GDAX";

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

        public GDAXClient(IBrokerApplication client, ILogger logger)
        {
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
            await chnl?.Stop();
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

                        string type = "";
                        JObject jToken = JObject.Parse(jsonResponse);
                        type = jToken["type"]?.Value<string>();


                        if (type == "error")
                        {
                            if (jToken["message"]?.Value<string>() == "Failed to subscribe")
                            {
                                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, jToken["reason"]?.Value<string>());
                                _client.OnEvent(Name, BrokerEvent.InternalError, jToken["reason"]?.Value<string>());
                            }
                        }
                        else
                        if (type == "ticker")
                        {
                            string symbol = jToken["product_id"].ToString();

                            MarketBook mb = new MarketBook()
                            {
                                AskPrice = (double)jToken["best_ask"],
                                BidPrice = (double)jToken["best_bid"],
                                LocalTime = DateTime.Now,
                            };
                            _client.OnReport(Name, Name + symbol.Replace("-", ""), mb);
                        }
                    }
                    break;
                case StreamMessageType.Error:
                    break;
                case StreamMessageType.Logon:
                    {
                        _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);

                        _client.OnEvent(Name, BrokerEvent.SessionLogon, "");
                        using (await _lock.LockAsync())
                        {
                            foreach (var item in SubscribeList)
                            {
                                var symbol = item.Key;

                                if (item.Value.mode == SubscriptionModel.TopBook)
                                {
                                    var request = new
                                    {
                                        type = "subscribe",
                                        product_ids = new List<string>() { symbol },
                                        channels = new List<string>() { "ticker" },
                                    };

                                    string paramData = JsonConvert.SerializeObject(request, Formatting.Indented);
                                    await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
                                }
                            }
                        }
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
            throw new NotImplementedException();
        }

        public async Task Subscribe(Instrument instr, SubscriptionModel model)
        {
            if (instr == null)
            {
                _logger.Log(LogPriority.Error, "instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, string.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            if (IsConnected != true)
            {
                _logger.Log(LogPriority.Error, $"Subscribe: {Name} is not connected", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, instr.Id());
                throw new Exception($"{Name} is not connected");
            }

            var symbol = Utilities.GDAXSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = new
                        {
                            type = "subscribe",
                            product_ids = new List<string>() { symbol },
                            channels = new List<string>() { "ticker" },
                        };
                        SubscribeList.Add(symbol, new Subscribe() { pair = instr });
                        _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);

                        string paramData = JsonConvert.SerializeObject(request, Formatting.Indented);
                        await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
                    }
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

            var symbol = Utilities.GDAXSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = new
                        {
                            type = "unsubscribe",
                            product_ids = new List<string>() { symbol },
                            channels = new List<string>() { "ticker" },
                        };
                        SubscribeList.Remove(symbol);
                        _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, symbol);

                        string paramData = JsonConvert.SerializeObject(request, Formatting.Indented);
                        await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
                    }
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, "Unsubscribe for " + symbol + " not exists");
            }
        }

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            string response = await REST.SendRequest(rest_domain + "/products", "GET");

            JArray jsonArray = JArray.Parse(response);
            IEnumerable<Instrument> result = jsonArray.Select(u => new Instrument()
            {
                Exchange = _name,
                Symbol = u["id"].ToString(),
                First = u["base_currency"].ToString(),
                Second = u["quote_currency"].ToString(),
            });
            _client.OnEvent(Name, BrokerEvent.Info, $"-->sent 'GetInstruments'");
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

