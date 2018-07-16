using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace Vaultoro
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string VaultoroSymbol(Instrument instr)
        {
            return $"{instr.First}-{instr.Second}";
        }
    }

    public class BidAsk
    {
        public List<List<double>> bids { get; set; }
        public List<List<double>> asks { get; set; }
    }
}
