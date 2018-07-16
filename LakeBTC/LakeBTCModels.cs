using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace LakeBTC
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string LakeBTCSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class Symbol
    {
        public double? high { get; set; }
        public double? low { get; set; }
        public double? vol { get; set; }
        public double? last { get; set; }
        public double? bid { get; set; }
        public double? ask { get; set; }
    }

    public class TickerResponse
    {
        public Symbol symbol { get; set; }
    }
}

