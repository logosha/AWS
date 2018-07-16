using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace Bitstamp
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
        
    }

    public static class Utilities
    {
        public static string BitstampSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class OrderBookResponse
    {
        public List<List<string>> bids { get; set; }
        public List<List<string>> asks { get; set; }
        public string timestamp { get; set; }
    }
}
