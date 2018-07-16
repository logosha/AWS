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

namespace GateIO
{
    public class GateIOClient : IBrokerClient
    {
        private CancellationTokenSource _source = new CancellationTokenSource();

        private IBrokerApplication _client;
        private ILogger _logger;
        private WebSocketWrapper chnl;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://ws.gate.io/v3/";
        private const string rest_domain = "https://data.gate.io/api2/1";

        private IDictionary<int, Subscribe> SubscribeList = new Dictionary<int, Subscribe>();

        private string _name = "GateIO";

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

        public GateIOClient(IBrokerApplication client, ILogger logger)
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
            await chnl.Stop();
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

                        if (jsonResponse.Contains("depth.update"))
                        {
                            var depthResponse = JsonConvert.DeserializeObject<GateIOUpdateResponse>(jsonResponse);

                            var status = depthResponse.@params[0].ToString();
                            var askbid = depthResponse.@params[1].ToString();
                            var symbol = depthResponse.@params[2].ToString();

                            if (status == "True")
                            {
                                var askbidResponse = JsonConvert.DeserializeObject<AskBidResponse>(askbid);
                                var symbolId = symbol.GetHashCode();
                                Subscribe data;
                                if (SubscribeList.TryGetValue(symbolId, out data))
                                {
                                    using (await _lock.LockAsync())
                                    {
                                        SubscribeList[symbolId].market = new MarketBook()
                                        {
                                            AskPrice = Convert.ToDouble(askbidResponse.asks[0][0]),
                                            AskSize = Convert.ToDouble(askbidResponse.asks[0][1]),
                                            BidPrice = Convert.ToDouble(askbidResponse.bids[0][0]),
                                            BidSize = Convert.ToDouble(askbidResponse.bids[0][1]),
                                            LocalTime = DateTime.Now
                                        };

                                        _client.OnReport(Name, data.pair.Id(), SubscribeList[symbolId].market);
                                    }
                                }
                            }
                            else
                            if (status == "False")
                            {
                                var askbidResponse = JsonConvert.DeserializeObject<AskBidResponse>(askbid);
                                var symbolId = symbol.GetHashCode();
                                Subscribe data;
                                if (SubscribeList.TryGetValue(symbolId, out data))
                                {
                                    using (await _lock.LockAsync())
                                    {
                                        if (askbidResponse.asks != null)
                                        {
                                            if (askbidResponse.asks.Count == 1)
                                            {
                                                data.market.AskPrice = Convert.ToDouble(askbidResponse.asks[0][0]);
                                                data.market.AskSize = Convert.ToDouble(askbidResponse.asks[0][1]);
                                            }
                                            else
                                            if (askbidResponse.asks.Count == 2)
                                            {
                                                data.market.AskPrice = Convert.ToDouble(askbidResponse.asks[1][0]);
                                                data.market.AskSize = Convert.ToDouble(askbidResponse.asks[1][1]);
                                            }
                                        }

                                        if (askbidResponse.bids != null)
                                        {
                                            if (askbidResponse.bids.Count == 1)
                                            {
                                                data.market.BidPrice = Convert.ToDouble(askbidResponse.bids[0][0]);
                                                data.market.BidSize = Convert.ToDouble(askbidResponse.bids[0][1]);
                                            }
                                            else
                                            if (askbidResponse.bids.Count == 2)
                                            {
                                                data.market.BidPrice = Convert.ToDouble(askbidResponse.bids[1][0]);
                                                data.market.BidSize = Convert.ToDouble(askbidResponse.bids[1][1]);
                                            }
                                        }
                                        data.market.LocalTime = DateTime.Now;

                                        _client.OnReport(Name, data.pair.Id(), SubscribeList[symbolId].market);
                                    }
                                }
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

                                if (item.Value.mode == SubscriptionModel.TopBook)
                                {
                                    var request = new
                                    {
                                        id = symbol.GetHashCode(),
                                        method = "depth.subscribe",
                                        @params = new List<object>() { symbol, 1, "0" }
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

            var symbol = Utilities.GateIOSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol.GetHashCode()))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = new
                        {
                            id = symbol.GetHashCode(),
                            method = "depth.subscribe",
                            @params = new List<object>() { symbol, 1, "0" }
                        };
                        SubscribeList.Add(request.id, new Subscribe() { pair = instr});
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

            var symbol = Utilities.GateIOSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol.GetHashCode()))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = new
                        {
                            SubscribeList[symbol.GetHashCode()].id,
                            method = "depth.unsubscribe",
                            @params = new List<object>() { }
                        };

                        SubscribeList.Remove(request.id);
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
            string response = await REST.SendRequest(rest_domain + "/pairs", "GET");

            JArray jsonArray = JArray.Parse(response);

            IEnumerable<Instrument> result = jsonArray.Select(u => new Instrument()
            {
                Exchange = _name,
                Symbol = u.ToString(),
                First = u.ToString().Split('_').First(),
                Second = u.ToString().Split('_').Last()
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


