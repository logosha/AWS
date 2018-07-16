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

namespace Gemini
{
    public class GeminiClient : IBrokerClient
    {
        static readonly public string ws_domain = "wss://api.gemini.com/v1/marketdata/";
        private IBrokerApplication _client;
        private ILogger _logger;
        private CancellationTokenSource _source;
        private PublicSubscribe connectChannel;
        static public string connectSymbol = "zeceth";
        private AsyncLock _lock = new AsyncLock();
        private object _locker = new object();

        private Dictionary<string, PublicSubscribe> SubscribeList = new Dictionary<string, PublicSubscribe>();
        private List<string> _pairs = new List<string>();

        static public string _name = "Gemini";

        public GeminiClient(IBrokerApplication client, ILogger logger)
        {
            _source = new CancellationTokenSource();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
        }

        public bool? IsStarted { get; private set; } = false;

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

        public async Task Start()
        {
            if (IsStarted != true)
            {
                IsStarted = true;
                connectChannel = new PublicSubscribe(_logger, _client);
                await connectChannel.Start(connectSymbol, new Instrument(), SubscriptionModel.TopBook);
            }
        }

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

            if (IsConnected != true)
            {
                _logger.Log(LogPriority.Error, $"Subscribe: {Name} is not connected", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, instr.Id());
                throw new Exception($"{Name} is not connected");
            }

            string end_point = Utilities.GeminiSymbol(instr);

            using (await _lock.LockAsync())
            {
                if (!SubscribeList.ContainsKey(end_point))
                {
                    SubscribeList.Add(end_point, new PublicSubscribe(_logger, _client));
                    var res = SubscribeList[end_point];
                    await res.Start(end_point, instr, model);
                }
                else
                    _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, "Subscribe on " + end_point + " already created");
            }
        }

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

        public async Task<IEnumerable<Instrument>> GetInstruments()
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        public bool GetProperty(PropertyName name)
        {
            if (name == PropertyName.IsWebsoketSupport)
                return true;
            else return false;
        }
    }
}
