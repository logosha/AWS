using Shared.Broker;
using Shared.Models;
using System.Collections.Generic;

namespace Bitfinex
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string BitfinexSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }

        static public string FirstSymbol(string instr)
        {
            return instr.Substring(0, 3);
        }

        static public string SecondSymbol(string instr)
        {
            return instr.Substring(3);
        }

        static public Instrument StrToInstr(string instr, string name)
        {
            return new Instrument { First = FirstSymbol(instr), Second = SecondSymbol(instr), Exchange = name };
        }
    }

    public class SubscribeChannels
    {
        public string Id;
        public string name;
        public string pair;
        public Instrument instr;
        public MarketBook market;

        public SubscribeChannels(string Name, string Pair, MarketBook Market, string cId)
        {
            name = Name;
            pair = Pair;
            market = Market;
            Id = cId;
        }
    }
}
