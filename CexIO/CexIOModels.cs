using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CexIO
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
        public Market market;
        public int lvl;
        public Subscribe(Instrument instr, SubscriptionModel model, Market markt, int lvl)
        {
            this.pair = instr;
            this.mode = model;
            this.market = markt;
            this.lvl = lvl;
        }
        public Subscribe()
        {
        }
    }

    public class Market
    {
        public SortedDictionary<double, double> ask_list;
        public SortedDictionary<double, double> bid_list;
        public Market()
        { }
    }

    public static class Utilities
    {
        public static string CexIOSymbol(Instrument instr)
        {
            return $"{instr.First}:{instr.Second}";
        }
    }

    public class Pair
    {
        public string symbol1 { get; set; }
        public string symbol2 { get; set; }
        public double minLotSize { get; set; }
        public double minLotSizeS2 { get; set; }
        public object maxLotSize { get; set; }
        public string minPrice { get; set; }
        public string maxPrice { get; set; }
    }

    public class DataPair
    {
        public List<Pair> pairs { get; set; }
    }

    public class CurrencyLimitsResponse
    {
        public string e { get; set; }
        public string ok { get; set; }
        public DataPair data { get; set; }
    }

    public class Data
    {
        public long timestamp { get; set; }
        public long time { get; set; }
        public List<List<double>> bids { get; set; }
        public List<List<double>> asks { get; set; }
        public string pair { get; set; }
        public int id { get; set; }
        public string sell_total { get; set; }
        public string buy_total { get; set; }
    }

    public class Response
    {
        public string e { get; set; }
        public Data data { get; set; }
        public string oid { get; set; }
        public string ok { get; set; }
    }

    public class Auth
    {
        public string key { get; set; }
        public string signature { get; set; }
        public int timestamp { get; set; }
    }

    public class AuthRequest
    {
        public string e { get; set; }
        public Auth auth { get; set; }
    }


    public class DataRequest
    {
        public List<string> pair { get; set; }
        public bool subscribe { get; set; }
        public int depth { get; set; }
    }

    public class Request
    {
        public string e { get; set; }
        public DataRequest data { get; set; }
        public string oid { get; set; }
    }
}
