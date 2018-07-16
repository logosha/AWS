using Shared.Broker;
using Shared.Models;
using System;

namespace itBit
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string itBitSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class TickerResponse
    {
        public string pair { get; set; }
        public string bid { get; set; }
        public string bidAmt { get; set; }
        public string ask { get; set; }
        public string askAmt { get; set; }
        public string lastPrice { get; set; }
        public string lastAmt { get; set; }
        public string volume24h { get; set; }
        public string volumeToday { get; set; }
        public string high24h { get; set; }
        public string low24h { get; set; }
        public string highToday { get; set; }
        public string lowToday { get; set; }
        public string openToday { get; set; }
        public string vwapToday { get; set; }
        public string vwap24h { get; set; }
        public DateTime serverTimeUTC { get; set; }
    }
}
