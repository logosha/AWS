using Shared.Broker;
using System.Collections.Generic;
using ACX;
using Allcoin;
using BitcoinIndonesia;
using BitFlyer;
using CexIO;
using Coinone;
using Gatecoin;
using GateIO;
using GDAX;
using LakeBTC;
using Livecoin;
using QuadrigaCX;
using Shared.Interfaces;
using Tidex;
using Poloniex;
using System.Threading.Tasks;
using System;
using HitBTC;
using Liqui;
using Kraken;
using Gemini;
using itBit;
using Vaultoro;
using WEX;
using EXX;
using Exmo;
using Binance;
using Bitfinex;
using Bittrex;
using Bitstamp;
using Quoine;

namespace CryptoExchanges2
{
    public class MainClient
    {
        public List<IBrokerClient> clientList = new List<IBrokerClient>();
        private ILogger _globalLogger;
        IBrokerApplication _app;

        public MainClient(IBrokerApplication app, ILogger globalLogger)
        {
            _globalLogger = globalLogger;
            _app = app;
            clientList.Add(new ACXClient(_app, _globalLogger));
            clientList.Add(new GDAXClient(_app, _globalLogger));
            clientList.Add(new GatecoinClient(_app, _globalLogger));
            clientList.Add(new QuadrigaCXClient(_app, _globalLogger));
            clientList.Add(new BitcoinIndonesiaClient(_app, _globalLogger));
            clientList.Add(new LakeBTCClient(_app, _globalLogger));
            clientList.Add(new LivecoinClient(_app, _globalLogger));
            clientList.Add(new TidexClient(_app, _globalLogger));
            clientList.Add(new CexIOClient(_app, _globalLogger));
            clientList.Add(new BitFlyerClient(_app, _globalLogger));
            clientList.Add(new CoinoneClient(_app, _globalLogger));
            clientList.Add(new PoloniexClient(_app, _globalLogger));
            clientList.Add(new LiquiClient(_app, _globalLogger));
            clientList.Add(new KrakenClient(_app, _globalLogger));
            clientList.Add(new itBitClient(_app, _globalLogger));
            clientList.Add(new VaultoroClient(_app, _globalLogger));
            clientList.Add(new ExmoClient(_app, _globalLogger));
            clientList.Add(new EXXClient(_app, _globalLogger));
            clientList.Add(new GeminiClient(_app, _globalLogger));
            clientList.Add(new AllcoinClient(_app, _globalLogger));
            clientList.Add(new WEXClient(_app, _globalLogger));
            clientList.Add(new BinanceClient(_app, _globalLogger));
            clientList.Add(new BitfinexClient(_app, _globalLogger));
            clientList.Add(new BitstampClient(_app, _globalLogger));
            clientList.Add(new QuoineClient(_app, _globalLogger));
            clientList.Add(new GateIOClient(_app, _globalLogger));
            clientList.Add(new HitBTCClient(_app, _globalLogger));
            clientList.Add(new BithumbClient(_app, _globalLogger));
            clientList.Add(new BittrexClient(_app, _globalLogger));


        }

        public async Task StartAll()
        {
            foreach (var item in clientList)
            {
                try
                {
                    await item.Start();

                }
                catch (Exception ex)
                {
                    _globalLogger.Log(LogPriority.Info, "Can't start connector " + item.Name + ", please check your configs and restart the app again");
                }
            }
        }

        public IEnumerable<string> SuppertedConnectors()
        {
            return new string[] {
                "Coinbase", "Coinone", "BitFlyer", "CexIO", "Livecoin", "Tidex",
                "LakeBTC", "GateIO", "BitcoinIndonesia", "QuadrigaCX", "ACX",
                "Allcoin", "Gatecoin", "Poloniex", "Bithumb", "HitBTC", "Liqui",
                "Kraken", "Gemini", "itBit", "Vaultoro", "WEX", "EXX", "Exmo",
                "Binance", "Bitfinex", "Bittrex", "Bitstamp", "Quoine"};
        }

        public IBrokerClient GetConnectorName(string name)
        {
            return clientList.Find(t => t.Name == name);
        }
    }
}
