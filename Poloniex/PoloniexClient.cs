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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poloniex
{
    public class PoloniexClient : IBrokerClient
    {
        private CancellationTokenSource _source = new CancellationTokenSource();

        private IBrokerApplication _client;
        private ILogger _logger;
        private WebSocketWrapper chnl;
        private readonly Index _index;
        private AsyncLock _lock = new AsyncLock();

        private const string ws_domain = "wss://api2.poloniex.com";
        private const string rest_domain = "https://poloniex.com/public?command=";

        private IDictionary<int, Subscribe> SubscribeList = new Dictionary<int, Subscribe>();
        private string _name = "Poloniex";

        public PoloniexClient(IBrokerApplication client, ILogger logger)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
            _index = new Index();
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
               SubscribeList.Clear();
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
                        if (jsonResponse.Contains("1002"))
                        {
                            JArray token = JArray.Parse(jsonResponse);
                            if (token[2].HasValues)
                            {
                                var chnlKey = Convert.ToInt32(token[2][0]);

                                if (SubscribeList.ContainsKey(chnlKey))
                                {
                                    MarketBook mb = new MarketBook()
                                    {
                                        AskPrice = token[2][2].ToObject<double>(),
                                        BidPrice = token[2][3].ToObject<double>(),
                                        LastSize = token[2][1].ToObject<double>(),
                                        LocalTime = DateTime.Now
                                    };
                                    _client.OnReport(Name, SubscribeList[chnlKey].pair.Id(), mb);
                                }
                            }
                        }
                    }
                    break;
                case StreamMessageType.Logon:
                    _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);
                    using (await _lock.LockAsync())
                    {
                        foreach (var item in SubscribeList)
                        {
                            var symbol = item.Key;

                            if (item.Value.mode == SubscriptionModel.TopBook)
                            {
                                string paramData = $"{{\"command\":\"subscribe\",\"channel\":\"1002\"}}";
                                await chnl.SendCommand(Encoding.UTF8.GetBytes(paramData));
                            }
                        }
                    }
                    break;
                case StreamMessageType.Logout:
                    _client.OnEvent(Name, BrokerEvent.SessionLogout, "");
                    break;
                case StreamMessageType.Error:
                    break;
                default:
                    break;
            }
        }

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            string response = await REST.SendRequest(rest_domain + "returnTicker", "GET");
            var pairsResponse = JsonConvert.DeserializeObject<Dictionary<string, Pair>>(response);

            IEnumerable<Instrument> result = pairsResponse.Select(u => new Instrument()
            {
                Exchange = Name,
                Symbol = u.Key,
                First = u.Key.Split('_')[0],
                Second = u.Key.Split('_')[1]
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
                _logger.Log(LogPriority.Error, "Subscribe: instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, string.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            if (IsConnected == false)
            {
                _logger.Log(LogPriority.Error, $"Subscribe: {Name} is not connected", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, instr.Id());
                throw new Exception($"{Name} is not connected");
            }

            var symbol = Utilities.PoloniexSymbol(instr);
            var chnlKey = _index.channels[symbol];
            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(chnlKey))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        SubscribeList.Add(chnlKey, new Subscribe() { mode = SubscriptionModel.TopBook, pair = instr });
                        _client.OnEvent(Name, BrokerEvent.CoinSubscribed, symbol);
                        string paramData = $"{{\"command\":\"subscribe\",\"channel\":\"1002\"}}";
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

            var symbol = Utilities.PoloniexSymbol(instr);
            var chnlKey = _index.channels[symbol];
            using (await _lock.LockAsync())
            {
                if (SubscribeList.ContainsKey(chnlKey))
                {
                    if (model == SubscriptionModel.TopBook)
                    {
                        SubscribeList.Remove(chnlKey);
                        _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, symbol);
                        string paramData = $"{{\"command\":\"unsubscribe\",\"channel\":\"1002\"}}";
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
