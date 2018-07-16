using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Shared.Broker;
using Shared.Common;
using Shared.Interfaces;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bitfinex
{
    public class BitfinexClient : IBrokerClient
    {
        private IBrokerApplication _client;
        private ILogger _logger;
        private CancellationTokenSource _source;
        private WebSocketWrapper chnl;

        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://api.bitfinex.com/ws/2";

        private Dictionary<int, SubscribeChannels> SubscribeList = new Dictionary<int, SubscribeChannels>();

        private string _name = "Bitfinex";

        public BitfinexClient(IBrokerApplication client, ILogger logger)
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
                        JObject jToken;
                        JArray jT;
                        if (!jsonResponse.Contains('{') && !jsonResponse.Contains("hb"))
                        {
                            jT = JArray.Parse(jsonResponse);
                            switch (jT[0].ToString())
                            {
                            case "0":   
                                break;
                            default:
                                {
                                    if (SubscribeList.ContainsKey((int)jT[0]))
                                    {
                                        var chn = SubscribeList[(int)jT[0]];
                                        if (chn.name == "ticker")
                                        {
                                            _client.OnReport(Name, chn.Id,
                                                new MarketBook()
                                                {
                                                    BidPrice = (double)jT[1][0],
                                                    AskPrice = (double)jT[1][2],
                                                    BidSize = (double)jT[1][1],
                                                    AskSize = (double)jT[1][3],
                                                    LastPrice = (double)jT[1][6],
                                                    LocalTime = DateTime.Now,
                                                });
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            if (jsonResponse.Contains("\"hb\"") || jsonResponse.Contains("\"ps\"") || jsonResponse.Contains("\"os\"") || jsonResponse.Contains("\"ws\""))
                                return;

                            jToken = JObject.Parse(jsonResponse);
                            var type = jToken["event"]?.Value<string>();
                            switch (type)
                            {
                                case "subscribed":
                                    {
                                        var cId = Utilities.StrToInstr(jToken["pair"].Value<string>(), Name).Id();
                                        using (await _lock.LockAsync())
                                        {
                                            SubscribeList.Add(jToken["chanId"].Value<int>(), new SubscribeChannels("ticker", jToken["pair"].Value<string>(), new MarketBook(), cId));
                                        }
                                        _logger.Log(LogPriority.Info, $"Channel {cId} - {jToken["channel"].Value<string>()}  is  subscribed.", Name);
                                        _client.OnEvent(Name, BrokerEvent.CoinSubscribed, cId);

                                        break;
                                    }
                                case "unsubscribed":
                                    {
                                        using (await _lock.LockAsync())
                                        {
                                            if (SubscribeList.ContainsKey(jToken["chanId"].Value<int>()))
                                            {
                                                var chId = SubscribeList[jToken["chanId"].Value<int>()].Id;
                                                SubscribeList.Remove(jToken["chanId"].Value<int>());
                                                _logger.Log(LogPriority.Info, $"Channel '{chId.ToString()}'  is  unsubscribed.", Name);
                                                _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, chId.ToString());
                                            }
                                        }
                                        break;
                                    }
                                case "error":
                                    {
                                        var code = jToken["code"]?.Value<string>();
                                        _logger.Log(LogPriority.Info, jToken["msg"].ToString(), Name);
                                        switch (code)
                                        {
                                            /*
                                                Error Codes
                                                10300 : Subscription failed (generic)
                                                10301 : Already subscribed
                                                10302 : Unknown channel
                                                10400 : Unsubscription failed (generic)
                                                10401 : Not subscribed
                                            */
                                            case "10300":
                                            case "10301":
                                            case "10302":
                                                {
                                                    using (await _lock.LockAsync())
                                                    {
                                                        var ch = SubscribeList.Where(u => u.Value.pair.Contains(jToken["pair"].Value<string>()) && u.Value.name == jToken["channel"].Value<string>()).FirstOrDefault();
                                                        if (ch.Value != null)
                                                        {
                                                            _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, ch.Value.Id);
                                                            SubscribeList.Remove(ch.Key);
                                                        }
                                                        else
                                                            _client.OnEvent(Name, BrokerEvent.InternalError, jToken["msg"].ToString());
                                                    }
                                                    break;
                                                }
                                            case "10401":
                                            case "10400":
                                                {
                                                    var ch = SubscribeList.Where(u => u.Value.pair.Contains(jToken["pair"].Value<string>()) && u.Value.name == jToken["channel"].Value<string>()).FirstOrDefault();
                                                    if (ch.Value != null)
                                                    {
                                                        _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, ch.Value.Id);
                                                    }
                                                    break;
                                                }
                                            default:
                                                {
                                                    {
                                                        _client.OnEvent(Name, BrokerEvent.InternalError, jToken["msg"].ToString());
                                                        break;
                                                    }
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        _client.OnEvent(Name, BrokerEvent.Info, jsonResponse);
                                        break;
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
                                var request = $"{{\"event\":\"subscribe\",\"channel\":\"ticker\", \"symbol\": \"{item.Value.pair}\"}}";
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

        public Task<IEnumerable<Instrument>> GetInstruments()
        {
            throw new NotSupportedException();
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

            var symbol = Utilities.BitfinexSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (SubscribeList.Count(u => u.Value.pair == symbol) == 0)
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        var request = $"{{\"event\":\"subscribe\",\"channel\":\"ticker\", \"symbol\": \"{symbol}\"}}";
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

            var chn = SubscribeList.Where(u => u.Value.pair == Utilities.BitfinexSymbol(instr)).FirstOrDefault();

            using (await _lock.LockAsync())
            {
                if (chn.Value != null)
                {
                    var request = $"{{\"event\":\"unsubscribe\",\"chanId\": \"{chn.Key.ToString()}\"}}";
                    await chnl.SendCommand(Encoding.UTF8.GetBytes(request));
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, "Unsubscribe for " + instr.Symbol + " not exists");
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
