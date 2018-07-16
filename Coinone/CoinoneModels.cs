using Newtonsoft.Json;
using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Coinone
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string CoinoneSymbol(Instrument instr)
        {
            return $"{instr.First}_{instr.Second}";
        }
    }

    public class Bid
    {
        public string price { get; set; }
        public string qty { get; set; }
    }

    public class Ask
    {
        public string price { get; set; }
        public string qty { get; set; }
    }

    public class TickerResponse
    {
        public string errorCode { get; set; }
        public string timestamp { get; set; }
        public string currency { get; set; }
        public List<Bid> bid { get; set; }
        public List<Ask> ask { get; set; }
        public string result { get; set; }
    }
}