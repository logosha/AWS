using Shared.Broker;
using Shared.Models;
using System;

namespace EXX
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string EXXSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Ticker
    {
        public string vol { get; set; }
        public string last { get; set; }
        public string sell { get; set; }
        public string buy { get; set; }
        public double weekRiseRate { get; set; }
        public double riseRate { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public double monthRiseRate { get; set; }
    }

    public class TickerResponse
    {
        public Ticker ticker { get; set; }
        public string date { get; set; }
    }
}
