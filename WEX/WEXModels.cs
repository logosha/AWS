using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace WEX
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string WEXSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Ticker
    {
        public double high { get; set; }
        public double low { get; set; }
        public double avg { get; set; }
        public double vol { get; set; }
        public double vol_cur { get; set; }
        public double last { get; set; }
        public double buy { get; set; }
        public double sell { get; set; }
        public long updated { get; set; }
    }
}
