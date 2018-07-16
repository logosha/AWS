using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Kraken
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string KrakenSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class Ticker
    {
        public decimal[] a;
        public decimal[] b;
        public decimal[] c;
        public decimal[] v;
        public decimal[] p;
        public int[] t;
        public decimal[] l;
        public decimal[] h;
        public decimal o;
    }

    public class GetTickerResponse
    {
        public List<string> error;
        public Dictionary<string, Ticker> result;
    }
}
