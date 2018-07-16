using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Shared.Broker;
using Shared.Common;
using Shared.Interfaces;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Binance
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string BinanceSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}".ToLower();
        }
    }

    public class PublicSubscribe
    {
        public string Id;
        public Instrument _pair;
        public SubscriptionModel _model;
        public int lvl;
        public string _endpoint;
        public WebSocketWrapper chnl;
        public IBrokerApplication _client;
        public ILogger _logger;
        public string ExchName = null;
        public string Uri;
        private AsyncLock _lock = new AsyncLock();

        private IDictionary<string, Subscribe> SubscribeList = new Dictionary<string, Subscribe>();

        public PublicSubscribe(ILogger Logger, IBrokerApplication Client)
        {
            _client = Client;
            _logger = Logger;
            Uri = BinanceClient.ws_domain;
            ExchName = BinanceClient._name;
        }

        public bool? IsConnected
        {
            get
            {
                return (chnl == null ? false : chnl.IsConnected);
            }
        }

        public async Task Start(string endpoint, Instrument pair, SubscriptionModel model)
        {
            Id = pair.Id();
            _endpoint = endpoint;
            _pair = pair;
            _model = model;

            chnl = new WebSocketWrapper(Uri + _endpoint, _endpoint, _logger, ExchName);
            await chnl.Start(ParseMessage);
        }

        public async Task Stop()
        {
            await chnl.Stop();
            SubscribeList.Clear();
        }

        private async Task ParseMessage(SocketEventHandlerArgs msg)
        {
            string symbol = msg.chnlName;
            switch (msg.type)
            {
                case StreamMessageType.Data:
                    {
                        var jsonResponse = Encoding.UTF8.GetString(msg.msg, 0, msg.msg.Length);
                        if (String.IsNullOrEmpty(jsonResponse))
                        {
                            _logger.Log(LogPriority.Warning, $"<--Report : Empty message received - '{jsonResponse}' : ", ExchName);
                            return;
                        }

                        if (symbol.Equals(BinanceClient.connectSymbol + "@ticker"))
                        {
                            return;
                        }

                        JObject jToken = JObject.Parse(jsonResponse);
                        var symbolItem = jToken["s"].ToString();
                        
                        MarketBook mb = new MarketBook()
                        {
                            AskPrice = jToken["a"].ToObject<double>(),
                            AskSize = jToken["A"].ToObject<double>(),
                            BidPrice = jToken["b"].ToObject<double>(),
                            BidSize = jToken["B"].ToObject<double>(),
                            ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(jToken["E"].ToObject<double>()),
                            LocalTime = DateTime.Now
                        };
                        _client.OnReport(ExchName, ExchName + symbolItem, mb);
                    }
                    break;
                case StreamMessageType.Logon:
                    {
                        _client.OnEvent(ExchName, BrokerEvent.ConnectorStarted, string.Empty);
                        using (await _lock.LockAsync())
                        {
                            if (SubscribeList.Count > 0)
                            {
                                foreach (var i in SubscribeList)
                                {
                                    chnl = new WebSocketWrapper(Uri + i.Key, i.Key, _logger, ExchName);
                                    await chnl.Start(ParseMessage);
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}