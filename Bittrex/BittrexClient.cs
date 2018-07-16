﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Broker;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;
using Shared.Interfaces;
using Shared.Models;
using Nito.AsyncEx;
using Microsoft.AspNet.SignalR.Client;
using System.IO.Compression;
using System.Globalization;
using Shared.Common;

namespace Bittrex
{
    public class BittrexClient : IBrokerClient
    {
        private Dictionary<string, Subscribe> _subscribeList = new Dictionary<string, Subscribe>();
        private string baseUrl = "https://beta.bittrex.com/signalr";

        private IBrokerApplication _client;
        private ILogger _logger;
        private AsyncLock _lock = new AsyncLock();

        private HubConnection _hubConnection;
        public IHubProxy _hubProxy;
        public delegate void BittrexCallback(string info);
        private BittrexCallback _updateExchangeState { get; }
        private string _name = "Bittrex";

        public BittrexClient(IBrokerApplication client, ILogger logger)
        {
            _updateExchangeState = MarketDataCallback;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _client = client;
            _logger = logger;
        }

        private async Task<bool?> ConnectToSignalR()
        {
            _hubConnection = new HubConnection(baseUrl);
            _hubProxy = _hubConnection.CreateHubProxy("c2");

            _hubConnection.Closed += _hubConnection_Closed;
            _hubConnection.ConnectionSlow += _hubConnection_ConnectionSlow;
            _hubConnection.Error += _hubConnection_Error;
            _hubConnection.Received += _hubConnection_Received;
            _hubConnection.Reconnected += _hubConnection_Reconnected;
            _hubConnection.Reconnecting += _hubConnection_Reconnecting;
            _hubConnection.StateChanged += _hubConnection_StateChanged;

            try
            {
                await _hubConnection.Start().ConfigureAwait(false);

                #region obsolete
                if (_hubConnection.State == ConnectionState.Connected)
                {
                    _hubProxy.On(
                               "uE",
                               exchangeStateDelta => _updateExchangeState?.Invoke(exchangeStateDelta)
                               );
                    _client.OnEvent(Name, BrokerEvent.SessionLogon, string.Empty);

                    using (await _lock.LockAsync())
                    {
                        if (_subscribeList.Count > 0)
                        {
                            foreach (var i in _subscribeList)
                            {
                                if (i.Value.market != null)
                                {
                                    i.Value.market.ask_list.Clear();
                                    i.Value.market.bid_list.Clear();
                                    i.Value.market.nonce = 0;
                                }
                                try
                                {
                                    var res = await QueryExchangeState(i.Key);
                                    ParseMessage(Decode(res));
                                    await SubscribeToExchangeDeltas(i.Key);
                                }
                                catch (Exception ex)
                                {
                                    _logger.Log(LogPriority.Error, $"Connection error {ex.Message}", Name);
                                }
                            }
                        }
                    }
                    _client.OnEvent(Name, BrokerEvent.SessionLogon, string.Empty);
                    _client.OnEvent(Name, BrokerEvent.ConnectorStarted, string.Empty);
                }
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogPriority.Error, $"Exception - {ex.Message}", Name);
                return false;
            }
        }

        private void _hubConnection_StateChanged(StateChange obj)
        {
            _logger.Log(LogPriority.Debug, $"*** StateChanged {obj.NewState}", Name);
            switch (obj.NewState)
            {
                case ConnectionState.Connecting:
                    _client.OnEvent(Name, BrokerEvent.Info, "Connecting");
                    break;
                case ConnectionState.Connected:
                    break;
                case ConnectionState.Reconnecting:
                    break;
                case ConnectionState.Disconnected:
                    _client.OnEvent(Name, BrokerEvent.SessionLogout, string.Empty);
                    break;
                default:
                    break;
            }
        }

        private void _hubConnection_Reconnecting()
        {
            _logger.Log(LogPriority.Debug, $"*** Reconnecting", Name);
        }

        private void _hubConnection_Reconnected()
        {
            _logger.Log(LogPriority.Debug, $"*** Reconnected", Name);
        }

        private void _hubConnection_Received(string obj)
        {
        }

        private void _hubConnection_Error(Exception obj)
        {
            _logger.Log(LogPriority.Debug, $"*** Error  {obj.Message}", Name);
        }

        private void _hubConnection_ConnectionSlow()
        {
            _logger.Log(LogPriority.Debug, $"*** ConnectionSlow", Name);
        }

        private void _hubConnection_Closed()
        {
            _logger.Log(LogPriority.Debug, $"*** Closed", Name);
            IsStarted = false;
            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

        public async Task Start()
        {
            if (IsStarted == true)
                throw new Exception($"{Name} already started");
            IsStarted = true;

            try
            {
                await ConnectToSignalR().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Log(LogPriority.Debug, $"Channel has not been started : {ex.StackTrace}, please restart channel manually.");
                throw;
            }
        }

        public async Task Stop()
        {
            if (IsStarted == false)
                throw new Exception($"{Name} already stopped");
            await Task.Run(() =>
            {
                _hubConnection.Stop();
            });
            using (await _lock.LockAsync())
            {
                if (_subscribeList != null)
                {
                    _subscribeList.Clear();
                }
            }
            IsStarted = false;
            _client.OnEvent(Name, BrokerEvent.ConnectorStopped, string.Empty);
        }

        public bool? IsConnected
        {
            get
            {
                return (_hubConnection != null && _hubConnection.State == ConnectionState.Connected ? true : false);
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public bool? IsStarted { get; private set; } = false;

        void MarketDataCallback(string info)
        {
            string decoded = "";
            try
            {
                decoded = Decode(info);
                ParseMessage(decoded);
            }
            catch (Exception exc)
            {
                _logger.Log(LogPriority.Error, $"Error parse: {decoded} exc({exc.ToString()})", Name);
                _client.OnEvent(Name, BrokerEvent.InternalError, $"Error parse: {decoded} exc({exc.ToString()})");
            }
        }

        public async Task<bool> SubscribeToExchangeDeltas(string marketName) =>
            await _hubProxy.Invoke<bool>("SubscribeToExchangeDeltas", marketName);

        public async Task<string> QueryExchangeState(string marketName) =>
            await _hubProxy.Invoke<string>("QueryExchangeState", marketName);

        public string Decode(string wireData)
        {
            byte[] gzipData = Convert.FromBase64String(wireData);
            using (var decompressedStream = new MemoryStream())
            using (var compressedStream = new MemoryStream(gzipData))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(decompressedStream);
                decompressedStream.Position = 0;

                using (var streamReader = new StreamReader(decompressedStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public async Task Subscribe(Instrument instr, SubscriptionModel model)
        {
            if (instr == null)
            {
                _logger.Log(LogPriority.Error, "Subscribe: instrument not specified", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, String.Empty);
                throw new ArgumentNullException("instrument not specified");
            }

            if (IsConnected == false)
            {
                _logger.Log(LogPriority.Error, $"Subscribe: {Name} is not connected", Name);
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, instr.Id());
                throw new Exception($"{Name} is not connected");
            }

            var marketName = Utils.BittrexSymbol(instr);
            if (!_subscribeList.ContainsKey(marketName))
            {
                using (await _lock.LockAsync())
                {
                    _subscribeList.Add(marketName, new Subscribe(instr, model));
                }
                _client.OnEvent(Name, BrokerEvent.CoinSubscribed, instr.Id());
                _logger.Log(LogPriority.Info, $"Channel '{instr.Id()}'  is  subscribed.", Name);
                try
                {
                    await SubscribeToExchangeDeltas(marketName);
                    var res = await QueryExchangeState(marketName);
                    ParseMessage(Decode(res));
                }
                catch (Exception ex)
                {
                    _logger.Log(LogPriority.Error, $"Connection error {ex.Message}", Name);
                }
            }
            else
            {
                _client.OnEvent(Name, BrokerEvent.CoinSubscribedFault, instr.Id());
                _logger.Log(LogPriority.Error, $"{instr.Id()} - channel is already subscribed", Name);
            }
        }

        public async Task Unsibscribe(Instrument instr, SubscriptionModel model)
        {
            var marketName = Utils.BittrexSymbol(instr);
            if (_subscribeList.ContainsKey(marketName))
            {
                using (await _lock.LockAsync())
                { _subscribeList.Remove(marketName); }
                _client.OnEvent(Name, BrokerEvent.CoinUnsubscribed, instr.Id());
                _logger.Log(LogPriority.Info, $"Channel {instr.Id()} unsubscribed.", Name);
            }
            else
            {
                _client.OnEvent(Name, BrokerEvent.CoinUnsubscribedFault, instr.Id());
                _logger.Log(LogPriority.Warning, $"{instr.Id()} - channel is not subscribed", Name);
            }

        }

        public async void ParseMessage(string jsonResponse)
        {
            if (jsonResponse == "null")
                return;

            JObject msg;
            string id;
            try
            {
                msg = JObject.Parse(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.Log(LogPriority.Debug, $"{jsonResponse}  exc: {ex.Message} {ex.StackTrace}", Name);
                throw;
            }

            //MarketBook part
            if (msg.ContainsKey(Keys.MarketName))
            {
                using (await _lock.LockAsync())
                {
                    if (!_subscribeList.ContainsKey(msg[Keys.MarketName].ToString()))
                        return;
                }
                IEnumerable<JToken> buy_msg = msg[Keys.Buys].Children();
                IEnumerable<JToken> sell_msg = msg[Keys.Sells].Children();
                int currentNonce = msg[Keys.Nonce].Value<int>();

                using (await _lock.LockAsync())
                {
                    var data = _subscribeList[msg[Keys.MarketName].ToString()].market;
                    id = _subscribeList[msg[Keys.MarketName].ToString()].pair.Id();

                    if (data.nonce != 0 && data.nonce == currentNonce)
                    {
                        _logger.Log(LogPriority.Debug, $"Received a duplicate nonce - {currentNonce} ", Name);
                        return;
                    }
                    else if (data.nonce != 0 && ((currentNonce - data.nonce) != 1))
                    {
                        _logger.Log(LogPriority.Debug, $"Error of synchronization {id}: correct nonce - {data.nonce + 1}, received nonce - {currentNonce} ", Name);
                        await Resibscribe(msg[Keys.MarketName].ToString(), data);
                        return;
                    }

                    int checker = 0;
                    if (buy_msg.FirstOrDefault() != null)
                    {
                        checker = buy_msg.FirstOrDefault().Count();
                    }
                    else if (sell_msg.FirstOrDefault() != null)
                    {
                        checker = sell_msg.FirstOrDefault().Count();
                    }
                    else
                    {
                        _logger.Log(LogPriority.Debug, "Empty data in ask/bid list", Name);
                        return;
                    }

                    if (checker == (int)msgType.Update && data.nonce < currentNonce && data.nonce != 0)
                    {
                        foreach (var item in buy_msg)
                        {

                            var itemType = (int)item[Keys.Type];
                            var itemSize = (double)item[Keys.Quantity];
                            var itemPrice = (double)item[Keys.Rate];
                            if (itemType == (int)UpdMarket.Add)
                            {
                                if (data.bid_list.ContainsKey(itemPrice))
                                {
                                    _logger.Log(LogPriority.Error, $"this Price '-{itemPrice}-' already exists, refresh marketbook snapshot", Name);
                                    _client.OnEvent(Name, BrokerEvent.InternalError, $"this Price '-{itemPrice}-' already exists, refresh marketbook snapshot");
                                    await Resibscribe(msg[Keys.MarketName].ToString(), data);
                                    return;
                                }
                                else
                                { data.bid_list.Add(itemPrice, itemSize); }
                            }
                            else if (itemType == (int)UpdMarket.Remove)
                            {
                                if (!data.bid_list.ContainsKey(itemPrice))
                                {
                                    _logger.Log(LogPriority.Error, $"this Price '-{itemPrice}-' not exists for delete, refresh marketbook snapshot", Name);
                                    _client.OnEvent(Name, BrokerEvent.InternalError, $"this Price '-{itemPrice}-' not exists for delete, refresh marketbook snapshot");
                                    await Resibscribe(msg[Keys.MarketName].ToString(), data);
                                    return;
                                }
                                else
                                    data.bid_list.Remove(itemPrice);
                            }
                            else if (itemType == (int)UpdMarket.Update)
                            {
                                if (!data.bid_list.ContainsKey(itemPrice))
                                {
                                    _logger.Log(LogPriority.Error, $"this Price '-{itemPrice}-' not exists for update, refresh marketbook snapshot", Name);
                                    _client.OnEvent(Name, BrokerEvent.InternalError, $"this Price '-{itemPrice}-' not exists for update, refresh marketbook snapshot");
                                    await Resibscribe(msg[Keys.MarketName].ToString(), data);
                                    return;
                                }
                                else
                                    data.bid_list[itemPrice] = itemSize;
                            }
                            data.nonce = currentNonce;
                        }

                        foreach (var item in sell_msg)
                        {
                            var itemType = (int)item[Keys.Type];
                            var itemSize = (double)item[Keys.Quantity];
                            var itemPrice = (double)item[Keys.Rate];
                            if (itemType == (int)UpdMarket.Add)
                            {
                                if (data.ask_list.ContainsKey(itemPrice))
                                {
                                    _logger.Log(LogPriority.Error, $"this Price '-{itemPrice}-' already exists, refresh marketbook snapshot", Name);
                                    _client.OnEvent(Name, BrokerEvent.InternalError, $"this Price '-{itemPrice}-' already exists, refresh marketbook snapshot");
                                    await Resibscribe(msg[Keys.MarketName].ToString(), data);
                                    return;
                                }
                                else
                                    data.ask_list.Add(itemPrice, itemSize);
                            }
                            else if (itemType == (int)UpdMarket.Remove)
                            {
                                if (!data.ask_list.ContainsKey(itemPrice))
                                {
                                    _logger.Log(LogPriority.Error, $"this Price '-{itemPrice}-' not exists for delete, refresh marketbook snapshot", Name);
                                    _client.OnEvent(Name, BrokerEvent.InternalError, $"this Price '-{itemPrice}-' not exists for delete, refresh marketbook snapshot");
                                    await Resibscribe(msg[Keys.MarketName].ToString(), data);
                                    return;
                                }
                                else
                                    data.ask_list.Remove(itemPrice);
                            }
                            else if (itemType == (int)UpdMarket.Update)
                            {
                                if (!data.ask_list.ContainsKey(itemPrice))
                                {
                                    _logger.Log(LogPriority.Error, $"this Price '-{itemPrice}-' not exists for update, refresh marketbook snapshot", Name);
                                    _client.OnEvent(Name, BrokerEvent.InternalError, $"this Price '-{itemPrice}-' not exists for update, refresh marketbook snapshot");
                                    await Resibscribe(msg[Keys.MarketName].ToString(), data);
                                    return;
                                }
                                else
                                    data.ask_list[itemPrice] = itemSize;
                            }
                            data.nonce = currentNonce;
                        }
                    }
                    else if (checker == (int)msgType.FullSnapShot)
                    {
                        data.nonce = currentNonce;
                        data.bid_list = new SortedDictionary<double, double>
                            (buy_msg.ToDictionary(u => (double)u[Keys.Rate], u => (double)u[Keys.Quantity]), Comparer<double>.Create((a, b) => b.CompareTo(a)));
                        data.ask_list = new SortedDictionary<double, double>
                            (sell_msg.ToDictionary(u => (double)u[Keys.Rate], u => (double)u[Keys.Quantity]), Comparer<double>.Default);
                    }
                    else if (checker == (int)msgType.Update && data.nonce == 0)
                        return;

                    try
                    {
                        var Market = new MarketBook()
                        {
                            LocalTime = DateTime.Now,
                            BidPrice = data.bid_list.First().Key,
                            AskPrice = data.ask_list.First().Key,
                            BidSize = data.bid_list.First().Value,
                            AskSize = data.ask_list.First().Value,
                        };

                        if (_subscribeList[msg[Keys.MarketName].ToString()].mode == SubscriptionModel.TopBook)
                        { _client.OnReport(Name, id, Market); }
                    }
                    catch (Exception e)
                    {
                        _logger.Log(LogPriority.Error, $"Exception - {e.Message}", Name);
                    }
                }
            }
        }

        public async Task Resibscribe(string _marketName, Market item)
        {
            item.ask_list.Clear();
            item.bid_list.Clear();
            item.nonce = 0;
            var res = await QueryExchangeState(_marketName);
            ParseMessage(Decode(res));
        }

        public Task<MarketBook?> GetTicker(Instrument instr)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Instrument>> GetInstruments()
        {
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