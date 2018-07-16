using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace HitBTC
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string HitBTCSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class Params
    {
        public string ask { get; set; }
        public string bid { get; set; }
        public string last { get; set; }
        public string open { get; set; }
        public string low { get; set; }
        public string high { get; set; }
        public string volume { get; set; }
        public string volumeQuote { get; set; }
        public DateTime timestamp { get; set; }
        public string symbol { get; set; }
    }

    public class TickerResponse
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public Params @params { get; set; }
    }
}
