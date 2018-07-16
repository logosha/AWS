using System;
using System.Runtime.Serialization;

namespace Shared.Models
{
    public class Instrument : IComparable
    {
        public string Exchange { get; set; }
        public string Symbol { get; set; }
        public string First { get; set; }
        public string Second { get; set; }
        public string Description { get; set; }
        public int    Decimals { get; set; }
        public string Id() { return Id(Exchange, First, Second); }
        static public string Id(string exchange, string first, string second) { return exchange + first + second; }

        public int CompareTo(object obj)
        {
            Instrument instrument = obj as Instrument;

            return this.Symbol.CompareTo(instrument.Symbol);
        }
    }
}
