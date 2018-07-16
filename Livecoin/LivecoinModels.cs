using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Livecoin
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string LivecoinSymbol(Instrument instr)
        {
            return $"{instr.First}/{instr.Second}";
        }
    }

    public class TickerResponse
    {
        public string cur { get; set; }
        public string symbol { get; set; }
        public double last { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double volume { get; set; }
        public double vwap { get; set; }
        public double max_bid { get; set; }
        public double min_ask { get; set; }
        public double best_bid { get; set; }
        public double best_ask { get; set; }
    }

    public class Restriction
    {
        public string currencyPair { get; set; }
        public int priceScale { get; set; }
        public double minLimitQuantity { get; set; }
    }

    public class Instruments
    {
        public bool success { get; set; }
        public double minBtcVolume { get; set; }
        public List<Restriction> restrictions { get; set; }
    }
}


