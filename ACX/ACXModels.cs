using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace ACX
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string ACXSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class Ticker
    {
        public string buy { get; set; }
        public string sell { get; set; }
        public string open { get; set; }
        public string low { get; set; }
        public string high { get; set; }
        public string last { get; set; }
        public string vol { get; set; }
    }

    public class TickerResponse
    {
        public int at { get; set; }
        public Ticker ticker { get; set; }
    }

    public class InstrumentResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public string base_unit { get; set; }
        public string quote_unit { get; set; }
    }
}

