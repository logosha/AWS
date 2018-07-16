using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Gatecoin
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string GatecoinSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
   }
   
    public class Ticker
    {
        public string currencyPair { get; set; }
        public double open { get; set; }
        public double last { get; set; }
        public double lastQ { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double volume { get; set; }
        public double bid { get; set; }
        public double bidQ { get; set; }
        public double ask { get; set; }
        public double askQ { get; set; }
        public double vwap { get; set; }
        public string createDateTime { get; set; }
    }

    public class TickerResponse
    {
        public List<Ticker> tickers { get; set; }
    }
}

