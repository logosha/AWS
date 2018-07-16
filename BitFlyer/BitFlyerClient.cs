using Newtonsoft.Json;
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

namespace BitFlyer
{
    public class BitFlyerClient : IBrokerClient
    {
        private CancellationTokenSource _source = new CancellationTokenSource();
        private IBrokerApplication _client;
        private ILogger _logger;
        private WebSocketWrapper chnl;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://ws.lightstream.bitflyer.com/json-rpc";
        private const string rest_domain = "https://api.bitflyer.jp/v1";

        private IDictionary<int, Subscribe> SubscribeList = new Dictionary<int, Subscribe>();

        private string _name = "BitFlyer";

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

        public BitFlyerClient(IBrokerApplication client, ILogger logger)
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

                        if (jsonResponse.Contains("tick_id"))
                        {
                            var tickerResponse = JsonConvert.DeserializeObject<TickerResponse>(jsonResponse);
                            using (await _lock.LockAsync())
                            {
                                string symbol = "";
                                var temp = tickerResponse.@params.channel.Split('_');
                                if (temp.Count() == 4)
                                {
                                    symbol = temp[2] + "_" + temp[3];
                                }
                                else
                                    if (temp.Count() == 5)
                                {
                                    symbol = temp[2] + "_" + temp[3] + "_" + temp[4];
                                }

                                var symbolId = symbol.GetHashCode();
                                Subscribe data;
                                if (SubscribeList.TryGetValue(symbolId, out data))
                                {

                                    data.market = new MarketBook()
                                    {
                                        AskPrice = Convert.ToDouble(tickerResponse.@params.message.best_ask),
                                        AskSize = Convert.ToDouble(tickerResponse.@params.message.best_ask_size),
                                        BidPrice = Convert.ToDouble(tickerResponse.@params.message.best_bid),
                                        BidSize = Convert.ToDouble(tickerResponse.@params.message.best_bid_size),
                                        LocalTime = DateTime.Now,
                                        ServerTime = tickerResponse.@params.message.timestamp,
                                        LastPrice = tickerResponse.@params.message.ltp,
                                        LastSize = tickerResponse.@params.message.volume
                                    };

                                    _client.OnReport(Name, data.pair.Id(), data.market);
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
                                    string channelName = "lightning_ticker_" + symbol;
                                    var symbolId = symbol.GetHashCode();
                                    var request =
                                       new
                                       {
                                           method = "subscribe",
                                           @params = new { channel = channelName },
                                           id = symbolId,
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

            var symbol = Utilities.BitFlyerSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol.GetHashCode()))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        string channelName = "lightning_ticker_" + symbol;
                        var symbolId = symbol.GetHashCode();
                        var request =
                           new
                           {
                               method = "subscribe",
                               @params = new { channel = channelName },
                               id = symbolId,
                           };

                        SubscribeList.Add(symbolId, new Subscribe() { pair = instr});
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

            var symbol = Utilities.BitFlyerSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(symbol.GetHashCode()))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        string channelName = "lightning_ticker_" + symbol;
                        var symbolId = symbol.GetHashCode();
                        var request =
                           new
                           {
                               method = "unsubscribe",
                               @params = new { channel = channelName },
                               id = symbolId,
                           };

                        SubscribeList.Remove(symbolId);
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
            string response = await REST.SendRequest(rest_domain + "/getmarkets", "GET");

            var instrumentResponse = JsonConvert.DeserializeObject<List<InstrumentResponse>>(response);
            List<Instrument> result = new List<Instrument>();
            foreach (var item in instrumentResponse)
            {
                var res = item.product_code.Split('_');
                if (res.Count() == 2)
                {
                    result.Add(new Instrument()
                    {
                        Exchange = _name,
                        Symbol = item.product_code,
                        First = res.First(),
                        Second = res.Last()
                    });
                }
                else
                if (res.Count() == 3)
                {
                    result.Add(new Instrument()
                    {
                        Exchange = _name,
                        Symbol = item.product_code,
                        First = res[0] + "_" + res[1],
                        Second = res[2]
                    });
                }
            }
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