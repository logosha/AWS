using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace GDAX
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string GDAXSymbol(Instrument instr)
        {
            return $"{instr.First}-{instr.Second}";
        }
    }

    public class Channel
    {
        public string name { get; set; }
        public List<string> product_ids { get; set; }
    }

    public class SubscribeResponse
    {
        public string type { get; set; }
        public List<Channel> channels { get; set; }
    }
}
