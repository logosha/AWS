using Shared.Broker;
using Shared.Models;
using System;
using System.Collections.Generic;

namespace Quoine
{
    public class Subscribe
    {
        public Instrument pair;
        public SubscriptionModel mode;
    }

    public static class Utilities
    {
        public static string QuoineSymbol(Instrument instr)
        {
            return $"{instr.First}{instr.Second}";
        }
    }

    public class OrderBookResponse
    {
        public string @event { get; set; }
        public string data { get; set; }
        public string channel { get; set; }
    }

    public class Pair
    {
        public string coin { get; set; }
        public string id { get; set; }
    }

    public class Product
    {
        public string id { get; set; }
        public string product_type { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public double market_ask { get; set; }
        public double market_bid { get; set; }
        public int indicator { get; set; }
        public string currency { get; set; }
        public string currency_pair_code { get; set; }
        public string symbol { get; set; }
        public object btc_minimum_withdraw { get; set; }
        public object fiat_minimum_withdraw { get; set; }
        public string pusher_channel { get; set; }
        public double taker_fee { get; set; }
        public double maker_fee { get; set; }
        public string low_market_bid { get; set; }
        public string high_market_ask { get; set; }
        public string volume_24h { get; set; }
        public string last_price_24h { get; set; }
        public string last_traded_price { get; set; }
        public string last_traded_quantity { get; set; }
        public string quoted_currency { get; set; }
        public string base_currency { get; set; }
        public bool disabled { get; set; }
    }

    public class Ticker
    {
        public int id { get; set; }
        public string product_type { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string market_ask { get; set; }
        public string market_bid { get; set; }
        public int indicator { get; set; }
        public string currency { get; set; }
        public string currency_pair_code { get; set; }
        public string symbol { get; set; }
        public object btc_minimum_withdraw { get; set; }
        public object fiat_minimum_withdraw { get; set; }
        public string pusher_channel { get; set; }
        public object taker_fee { get; set; }
        public object maker_fee { get; set; }
        public string low_market_bid { get; set; }
        public string high_market_ask { get; set; }
        public string volume_24h { get; set; }
        public string last_price_24h { get; set; }
        public string last_traded_price { get; set; }
        public string last_traded_quantity { get; set; }
        public string quoted_currency { get; set; }
        public string base_currency { get; set; }
        public bool disabled { get; set; }
    }
}
