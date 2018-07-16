using Newtonsoft.Json;
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

namespace CexIO
{
    public class CexIOClient : IBrokerClient
    {
        private IBrokerApplication _client;
        private ILogger _logger;
        private CancellationTokenSource _source;
        private WebSocketWrapper chnl;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://ws.cex.io/ws/";
        private const string rest_domain = "https://cex.io/api";
        private const string apiKey = "Q9q7LfLJWt2ZamZmXZSLraSNXI";
        private const string apiSecret = "xgFPD78BD2IvnZalFmyBPs5MCM";

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        private string _name = "CexIO";

        public CexIOClient(IBrokerApplication client, ILogger logger)
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


        private string CreateSignature(string apiKey, long unixTimestamp, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{unixTimestamp}{apiKey}"));
                return string.Concat(hash.Select(b => b.ToString("X2")));
            }
        }

        private async Task Authentification()
        {
            var unixTimestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var authRequest = new
            {
                e = "auth",
                auth = new { key = apiKey, signature = CreateSignature(apiKey, unixTimestamp, apiSecret), timestamp = unixTimestamp },
            };

            string paramData = JsonConvert.SerializeObject(authRequest, Formatting.Indented);
            await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
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
                        if (jsonResponse == "[]")
                            return;
                        if (jsonResponse.Contains("auth"))
                        {
                            _client.OnEvent(Name, BrokerEvent.Info, "Authentification success");
                        }
                        else
                        if (jsonResponse.Contains("ping"))
                        {
                            var request = new
                            {
                                e = "pong"
                            };
                            string paramData = JsonConvert.SerializeObject(request, Formatting.Indented);
                            await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
                        }
                        else
                        if (jsonResponse.Contains("order-book-subscribe"))
                        {
                            var tickerResponse = JsonConvert.DeserializeObject<Response>(jsonResponse);
                            string symbol = tickerResponse.oid;
                            long time = tickerResponse.data.timestamp;

                            Subscribe data;
                            if (SubscribeList.TryGetValue(symbol, out data))
                            {
                                using (await _lock.LockAsync())
                                {
                                    foreach (var bid in tickerResponse.data.bids)
                                    {
                                        if (!data.market.bid_list.ContainsKey(bid[0]))
                                        {
                                            data.market.bid_list.Add(bid[0], bid[1]);
                                        }
                                    }

                                    foreach (var ask in tickerResponse.data.asks)
                                    {
                                        if (!data.market.ask_list.ContainsKey(ask[0]))
                                        {
                                            data.market.ask_list.Add(ask[0], ask[1]);
                                        }
                                    }

                                    MarketBook mb = new MarketBook()
                                    {
                                        LocalTime = DateTime.Now,
                                        ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(time),
                                        AskPrice = data.market.ask_list.First().Key,
                                        AskSize = data.market.ask_list.First().Value,
                                        BidPrice = data.market.bid_list.Last().Key,
                                        BidSize = data.market.bid_list.Last().Value,
                                    };

                                    _client.OnReport(Name, data.pair.Id(), mb);
                                }
                            }
                        }
                        else
                        if (jsonResponse.Contains("md_update"))
                        {
                            var updateResponse = JsonConvert.DeserializeObject<Response>(jsonResponse);
                            string symbol = updateResponse.data.pair;
                            long time = updateResponse.data.time;
                            Subscribe data;
                            if (SubscribeList.TryGetValue(symbol, out data))
                            {
                                using (await _lock.LockAsync())
                                {
                                    if (updateResponse.data.asks.Count != 0)
                                    {
                                        foreach (var ask in updateResponse.data.asks)
                                        {
                                            var askPrice = ask[0];
                                            var askSize = ask[1];

                                            if (askSize == 0.0)
                                            {
                                                if (!data.market.ask_list.ContainsKey(askPrice))
                                                {
                                                    _logger.Log(LogPriority.Error, $"this Price '-{askPrice}-' not exists for delete", Name);
                                                }
                                                else
                                                    data.market.ask_list.Remove(askPrice);
                                            }
                                            else
                                            {
                                                if (data.market.ask_list.ContainsKey(askPrice))
                                                {
                                                    data.market.ask_list[askPrice] = askSize;
                                                }
                                                else
                                                {
                                                    data.market.ask_list.Add(askPrice, askSize);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    if (updateResponse.data.bids.Count != 0)
                                    {
                                        foreach (var bid in updateResponse.data.bids)
                                        {
                                            var bidPrice = bid[0];
                                            var bidSize = bid[1];

                                            if (bidSize == 0.0)
                                            {
                                                data.market.bid_list.Remove(bidPrice);
                                            }
                                            else
                                            {
                                                if (data.market.bid_list.ContainsKey(bidPrice))
                                                {
                                                    if (bidSize == data.market.bid_list[bidPrice])
                                                    {
                                                        _logger.Log(LogPriority.Error, $"this Price '-{bidPrice}-' not changed", Name);
                                                    }
                                                    else
                                                        data.market.bid_list[bidPrice] = bidSize;
                                                }
                                                else
                                                {
                                                    data.market.bid_list.Add(bidPrice, bidSize);
                                                }
                                            }
                                        }
                                    }
                                }

                                MarketBook mb = new MarketBook()
                                {
                                    LocalTime = DateTime.Now,
                                    ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(time),
                                    AskPrice = data.market.ask_list.First().Key,
                                    AskSize = data.market.ask_list.First().Value,
                                    BidPrice = data.market.bid_list.Last().Key,
                                    BidSize = data.market.bid_list.Last().Value,
                                };
                                _client.OnReport(Name, data.pair.Id(), mb);
                            }
                        }
                    }
                    break;
                case StreamMessageType.Error:
                    break;
                case StreamMessageType.Logon:
                    {
                        await Authentification();
                        _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);
                        using (await _lock.LockAsync())
                        {
                            foreach (var item in SubscribeList)
                            {
                                var symbol = item.Key;

                                var request = new Request
                                {
                                    e = "order-book-subscribe",
                                    data = new DataRequest { pair = new List<string>() { item.Value.pair.First, item.Value.pair.Second }, subscribe = true, depth = 0 },
                                    oid = symbol
                                };

                                string paramData = JsonConvert.SerializeObject(request, Formatting.Indented);
                                await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
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
            string response = await REST.SendRequest(rest_domain + "/currency_limits", "GET");
            var curencyLimits = JsonConvert.DeserializeObject<CurrencyLimitsResponse>(response);

            var data = curencyLimits.data.pairs;
            IEnumerable<Instrument> result = data.Select(u => new Instrument()
            {
                Exchange = "CexIO",
                Symbol = u.symbol1.ToString() + "/" + u.symbol2.ToString(),
                First = u.symbol1.ToString(),
                Second = u.symbol2.ToString(),
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

            var symbol = Utilities.CexIOSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = new Request
                        {
                            e = "order-book-subscribe",
                            data = new DataRequest { pair = new List<string>() { instr.First, instr.Second }, subscribe = true, depth = 0 },
                            oid = symbol
                        };

                        SubscribeList.Add(symbol, new Subscribe()
                        {
                            market = new Market()
                            {
                                ask_list = new SortedDictionary<double, double>(),
                                bid_list = new SortedDictionary<double, double>()
                            },
                            pair = instr });
                        _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);
                        _logger.Log(LogPriority.Info, "Coin Subscribed - " + symbol, Name);

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

            var symbol = Utilities.CexIOSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = new Request
                        {
                            e = "order-book-unsubscribe",
                            data = new DataRequest { pair = new List<string>() { instr.First, instr.Second } },
                            oid = symbol
                        };
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
