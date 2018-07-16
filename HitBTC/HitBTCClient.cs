using HitBTC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Shared.Broker;
using Shared.Common;
using Shared.Interfaces;
using Shared.Models;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HitBTC
{
    public class HitBTCClient : IBrokerClient
    {
        private IBrokerApplication _client;
        private ILogger _logger;
        private CancellationTokenSource _source;
        private WebSocketWrapper chnl;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://api.hitbtc.com/api/2/ws";
        private const string rest_domain = "https://api.hitbtc.com/api/2";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "HitBTC";

        public HitBTCClient(IBrokerApplication client, ILogger logger)
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
            await chnl.Stop();
            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

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

        private async Task ParseMessage(SocketEventHandlerArgs msg)
        {
            switch (msg.type)
            {
                case StreamMessageType.Data:
                    {
                        var jsonResponse = Encoding.UTF8.GetString(msg.msg, 0, msg.msg.Length);
                        if (jsonResponse.Contains("error"))
                        {
                            JObject jToken;
                            jToken = JObject.Parse(jsonResponse);
                            var dict = jToken["error"].ToObject<Dictionary<string, string>>();
                            _client.OnEvent(Name, BrokerEvent.InternalError, "Message: " + dict["message"] + ", Description: " + dict["description"]);
                        }
                        else if (jsonResponse.Contains("method"))
                        {
                            var response = JsonConvert.DeserializeObject<TickerResponse>(jsonResponse);
                            var method = response.method;
                            var symbol = response.@params.symbol;
                            var data = response.@params;
                            if (method == "ticker" && SubscribeList.ContainsKey(symbol))
                            {
                                MarketBook mb = new MarketBook()
                                {
                                    LocalTime = DateTime.Now,
                                    ServerTime = data.timestamp,
                                    AskPrice = Convert.ToDouble(data.ask),
                                    BidPrice = Convert.ToDouble(data.bid),
                                    LastPrice = Convert.ToDouble(data.last),
                                    LastSize = Convert.ToDouble(data.volume),
                                };
                                _client.OnReport(Name, SubscribeList[symbol].pair.Id(), mb);
                            }
                        }
                    }
                    break;
                case StreamMessageType.Error:
                    break;
                case StreamMessageType.Logon:
                    {
                        _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);
                        using (await _lock.LockAsync())
                        {
                            foreach (var item in SubscribeList)
                            {
                                var symbol = item.Key;

                                var request = $"{{\"method\":\"subscribeTicker\",\"params\":{{ \"symbol\":\"{item.Value.pair.Symbol}\"}}, \"id\": \"123\"}}";

                                await chnl.SendCommand(Encoding.UTF8.GetBytes(request));
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

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            string response = await REST.SendRequest(rest_domain + "/public/symbol/", "GET");

            JArray jsonArray = JArray.Parse(response);
            IEnumerable<Instrument> result = jsonArray.Select(u => new Instrument()
            {
                Exchange = "HitBtc",
                Symbol = u["baseCurrency"].ToString(),
                First = u["quoteCurrency"].ToString(),
                Second = u["id"].ToString(),
            });

            _client.OnEvent(Name, BrokerEvent.Info, $"-->sent 'GetInstruments'");
            return result;
        }

        public Task<MarketBook?> GetTicker(Instrument instr)
        {
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

            var symbol = Utilities.HitBTCSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = $"{{\"method\":\"subscribeTicker\",\"params\":{{ \"symbol\":\"{symbol}\"}}, \"id\": \"123\"}}";

                        SubscribeList.Add(symbol, new Subscribe()
                        {
                            mode = SubscriptionModel.TopBook,
                            pair = instr
                        });
                        _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);
                        _logger.Log(LogPriority.Info, "Coin Subscribed - " + symbol, Name);

                        await chnl.SendCommand(Encoding.UTF8.GetBytes(request));
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

            var symbol = Utilities.HitBTCSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = $"{{\"method\":\"unsubscribeTicker\",\"params\":{{ \"symbol\":\"{symbol}\"}}, \"id\": \"123\"}}";
                        SubscribeList.Remove(symbol);
                        _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, symbol);
                        _logger.Log(LogPriority.Info, "Coin Unsubscribed - " + symbol, Name);

                        string paramData = JsonConvert.SerializeObject(request, Formatting.Indented);
                        await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
                    }
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, "Unsubscribe for " + symbol + " not exists");
            }
        }

        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return true;
            else return false;
        }
    }
}
