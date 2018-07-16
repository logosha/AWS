using Shared.Models;
using System;
using TeaTime;

namespace CryptoExchanges2
{
    public class MarketBookResponse : IComparable
    {
        public MarketBook Book { get; set; }
        public string Name { get; set; }
        public Instrument Coin { get; set; }
        public bool isUpdated { get; set; }
        public string Type { get; set; }

        public int CompareTo(object obj)
        {
            MarketBookResponse mbr = obj as MarketBookResponse;

            return this.Name.CompareTo(mbr.Name);
        }
    };

    public struct Tick
    {
        public Tick(Time localTime, Time serverTime, double bidPrice, double askPrice, double bidSize, double askSize, double lastSize, double lastPrice)
        {
            LocalTime = localTime;
            ServerTime = serverTime;
            BidPrice = bidPrice;
            AskPrice = askPrice;
            BidSize = bidSize;
            AskSize = askSize;
            LastSize = lastSize;
            LastPrice = lastPrice;
        }

        public Time LocalTime { get; set; }
        public Time ServerTime { get; set; }
        public double BidPrice { get; set; }
        public double AskPrice { get; set; }
        public double BidSize { get; set; }
        public double AskSize { get; set; }
        public double LastSize { get; set; }
        public double LastPrice { get; set; }
    };
}
