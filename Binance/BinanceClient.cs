using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shared.Broker;
using Shared.Interfaces;
using Shared.Models;
using Nito.AsyncEx;
using System.Globalization;

namespace Binance
{
    public class BinanceClient : IBrokerClient
    {
        static readonly public string ws_domain = "wss://stream.binance.com:9443/ws/";
        private IBrokerApplication _client;
        private ILogger _logger;
        private CancellationTokenSource _source;
        private PublicSubscribe connectChannel;
        static public string connectSymbol = "bnbbtc";
        private AsyncLock _lock = new AsyncLock();
        private object _locker = new object();

        private Dictionary<string, PublicSubscribe> SubscribeList = new Dictionary<string, PublicSubscribe>();
        private List<string> _pairs = new List<string>();

        static public string _name = "Binance";

        public BinanceClient(IBrokerApplication client, ILogger logger)
        {
            _source = new CancellationTokenSource();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// IBrokerClient implementation,
        /// return true if Start() is called and the connector is started, 
        /// return false if Stop() is called and the connector is stopped.
        /// </summary>
        public bool? IsStarted { get; private set; } = false;

        /// <summary>
        /// IBrokerClient implementation,
        /// if WebSocket is supported return state of websocket mean Connected/NotConnected, 
        /// if WebSocket is not supported just return true.
        /// </summary>
        public bool? IsConnected
        {
            get
            {
                return (connectChannel == null ? false : connectChannel.IsConnected);
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// IBrokerClient implementation,
        /// One should start websocket here if the connector supports websocket
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            if (IsStarted != true)
            {
                IsStarted = true;
                connectChannel = new PublicSubscribe(_logger, _client);
                await connectChannel.Start(connectSymbol + "@ticker", new Instrument(), SubscriptionModel.TopBook);
            }
        }

        /// <summary>
        /// IBrokerClient implementation,
        /// One should stop websocket here, clear all other resources, like subscriptions etc...
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            using (await _lock.LockAsync())
            {
                if (SubscribeList.Count > 0)
                {
                    foreach (var i in SubscribeList)
                    {
                        await i.Value.Stop();
                    }
                }
            }
            IsStarted = false;
            await connectChannel.Stop();
            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

        /// <summary>
        ///   IBrokerClient implementation,
        ///   it is not supported for now and can be omitted
        /// </summary>
        /// <param name="instr"></param>
        /// <returns></returns>
        public async Task<MarketBook?> GetTicker(Instrument instr)
        {
            await Task.Yield();
            throw new NotSupportedException();
        }

        /// <summary>
        ///  IBrokerClient implementation, 
        /// One should subscribe to a coin here.
        /// Mean send the subscription message to the websocket and put the subscription to subscription list,
        /// if  websocket connection is restarted the connector should resubscribe subscribed coins automatically.
        /// </summary>
        /// <param name="instr"></param>
        /// <param name="model"></param>
        /// <returns></returns>
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

            string symbol = Utilities.BinanceSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(symbol))
                {
                    SubscribeList.Add(symbol, new PublicSubscribe(_logger, _client));
                    var res = SubscribeList[symbol];
                    await res.Start(symbol + "@ticker", instr, model);
                    _client.OnEvent(Name, BrokerEvent.CoinSubscribed, instr.Id());
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, "Subscribe on " + symbol + " already created");
            }
        }

        /// <summary>
        ///  IBrokerClient implementation, 
        ///  Need unsubscribe to a coin here.
        /// </summary>
        /// <param name="instr"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task Unsibscribe(Instrument instr, SubscriptionModel model)
        {
            using (await _lock.LockAsync())
            {
                var subscribe = SubscribeList.Where(u => u.Value._pair.Id() == instr.Id() && u.Value._model == model).FirstOrDefault();
                if (subscribe.Value != null)
                {
                    try
                    {
                        await subscribe.Value.Stop();
                        SubscribeList.Remove(subscribe.Key);
                        _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, instr.Id());
                        _logger.Log(LogPriority.Info, $"Unsubscribed {subscribe.Value._pair} - {subscribe.Value._endpoint}", Name);
                    }
                    catch (Exception ex)
                    {
                        _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, instr.Id());
                        _logger.Log(LogPriority.Info, $"Unsubscribed error {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log(LogPriority.Warning, $"Unsubscribe: {instr.First}-{instr.Second} - is not subscribed", Name);
                    _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, instr.Id());
                    return;
                }
            }
        }

        /// <summary>
        ///  IBrokerClient implementation,
        ///   it is not supported for now and can be omitted
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        /// <summary>
        ///  IBrokerClient implementation,
        ///  it just gets some features of the connector, it supports only IsWebsoketSupport for now,
        ///  so if the connector supports WebSocket return true, otherwise return false
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return true;
            else return false;
        }
    }
}
