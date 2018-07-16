using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Shared.Broker;
using Shared.Common;
using Shared.Interfaces;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gemini
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
        public Market market;
        public Subscribe(Instrument instr, SubscriptionModel model, Market markt, int lvl)
        {
            this.pair = instr;
            this.mode = model;
            this.market = markt;
        }
        public Subscribe()
        {
        }
    }

    public static class Utilities
    {
        public static string GeminiSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class Market
    {
        public string Id;
        public string endpoint;
        public long ServerTime;
        public SortedDictionary<double, double> sell_list;
        public SortedDictionary<double, double> buy_list;
        public Market(string endpoint, string Id)
        {
            this.endpoint = endpoint;
            this.Id = Id;
        }
        public Market()
        {
        }
    }

    public class PriceTick
    {
        public string type { get; set; }
        public string reason { get; set; }
        public double price { get; set; }
        public double delta { get; set; }
        public double remaining { get; set; }
        public string side { get; set; }
    }

    public class Update
    {
        public string type { get; set; }
        public string eventId { get; set; }
        public int timestamp { get; set; }
        public long timestampms { get; set; }
        public uint socket_sequence { get; set; }
        public IList<PriceTick> events { get; set; }
    }

    public class PublicSubscribe
    {
        public string Id;
        public Instrument _pair;
        public SubscriptionModel _model;
        public int lvl;
        public string _endpoint;
        public WebSocketWrapper chnl;
        public Market market;
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
            Uri = GeminiClient.ws_domain;
            ExchName = GeminiClient._name;
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

            chnl = new WebSocketWrapper(Uri+ _endpoint, _endpoint, _logger, ExchName);
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

                        if (symbol.Equals(GeminiClient.connectSymbol))
                        {
                            return;
                        }

                        JObject jToken = JObject.Parse(jsonResponse);
                        var type = jToken["type"]?.Value<string>();

                        if (type == "update")
                        {
                            var subscribeResponse = JsonConvert.DeserializeObject<Update>(jsonResponse);

                            if (jsonResponse.Contains("initial"))
                            {
                                SortedDictionary<double, double> ask_book = new SortedDictionary<double, double>();
                                SortedDictionary<double, double> bid_book = new SortedDictionary<double, double>();

                                foreach (var item in subscribeResponse.events)
                                {
                                    if (item.side == "ask")
                                    {
                                        ask_book.Add(item.price, item.remaining);
                                    }
                                    else
                                    if (item.side == "bid")
                                    {
                                        bid_book.Add(item.price, item.remaining);
                                    }
                                }
                                using (await _lock.LockAsync())
                                {
                                    SubscribeList.Add(symbol, new Subscribe()
                                    {
                                        market = new Market()
                                        {
                                            buy_list = bid_book,
                                            sell_list = ask_book,
                                            ServerTime = subscribeResponse.timestampms
                                        }
                                    });

                                    _client.OnEvent(ExchName, BrokerEvent.CoinSubscribed, symbol);
                                    _logger.Log(LogPriority.Info, "Coin Subscribed - " + symbol, ExchName);

                                    SendReport(symbol, SubscribeList[symbol]);
                                }
                            }
                            else

                            foreach (var eventItem in subscribeResponse.events)
                            {
                                if (eventItem.reason == "cancel" && eventItem.type == "change")
                                {
                                    var price = eventItem.price;
                                    var size = eventItem.remaining;
                                    var side = eventItem.side;
                                    Subscribe data;
                                    if (SubscribeList.TryGetValue(symbol, out data))
                                    {
                                        using (await _lock.LockAsync())
                                        {
                                            if (side == "ask")
                                            {
                                                if (!data.market.sell_list.ContainsKey(price))
                                                {
                                                    _logger.Log(LogPriority.Error, $"this Price '-{price}-' not exists for delete", symbol);
                                                }
                                                else
                                                if (size == 0)
                                                {
                                                    data.market.sell_list.Remove(price);
                                                }
                                                else
                                                {
                                                    data.market.sell_list.Remove(price);
                                                    data.market.sell_list.Add(price, size);
                                                }
                                            }
                                            else
                                            if (side == "bid")
                                            {
                                                if (!data.market.buy_list.ContainsKey(price))
                                                {
                                                    _logger.Log(LogPriority.Error, $"this Price '-{price}-' not exists for delete", symbol);
                                                }
                                                else
                                                if (size == 0)
                                                {
                                                    data.market.buy_list.Remove(price);
                                                }
                                                else
                                                {
                                                    data.market.buy_list.Remove(price);
                                                    data.market.buy_list.Add(price, size);
                                                }
                                            }
                                                data.market.ServerTime = subscribeResponse.timestampms;
                                                SendReport(symbol, SubscribeList[symbol]);
                                        }
                                    }
                                }
                                else
                                if ((eventItem.reason == "place" || eventItem.reason == "trade") && eventItem.type == "change")
                                {
                                    var price = eventItem.price;
                                    var size = eventItem.remaining;
                                    var side = eventItem.side;
                                    Subscribe data;
                                    if (SubscribeList.TryGetValue(symbol, out data))
                                    {
                                        using (await _lock.LockAsync())
                                        {
                                            if (side == "ask")
                                            {
                                                if (data.market.sell_list.ContainsKey(price) && data.market.sell_list[price] == size)
                                                {
                                                    _logger.Log(LogPriority.Error, $"this Price '-{price}-' already exists", symbol);
                                                }
                                                else
                                                if (data.market.sell_list.ContainsKey(price) && data.market.sell_list[price] != size)
                                                {
                                                    data.market.sell_list[price] = size;
                                                }
                                                else
                                                if(size == 0)
                                                {
                                                    data.market.sell_list.Remove(price);
                                                }
                                                else
                                                data.market.sell_list.Add(price, size);
                                            }
                                            else
                                            if (side == "bid")
                                            {
                                                if (data.market.buy_list.ContainsKey(price) && data.market.buy_list[price] == size)
                                                {
                                                    _logger.Log(LogPriority.Error, $"this Price '-{price}-' already exists", symbol);
                                                }
                                                else
                                                if (data.market.buy_list.ContainsKey(price) && data.market.buy_list[price] != size)
                                                {
                                                    data.market.buy_list[price] = size;
                                                }
                                                else
                                                if (size == 0)
                                                {
                                                    data.market.buy_list.Remove(price);
                                                }
                                                else
                                                    data.market.buy_list.Add(price, size);
                                            }
                                        }
                                    }
                                    data.market.ServerTime = subscribeResponse.timestampms;
                                    SendReport(symbol, SubscribeList[symbol]);
                                }
                            }
                        }
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

        private void SendReport(string symbol, Subscribe symbolSubscribe)
        {
            _client.OnReport(ExchName, ExchName + symbol,
                            new MarketBook()
                            {
                                BidPrice = symbolSubscribe.market.buy_list.Last().Key,
                                BidSize = symbolSubscribe.market.buy_list.Last().Value,
                                AskPrice = symbolSubscribe.market.sell_list.First().Key,
                                AskSize = symbolSubscribe.market.sell_list.First().Value,
                                LocalTime = DateTime.Now,
                                ServerTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(symbolSubscribe.market.ServerTime)
                            });
        }
    }
}