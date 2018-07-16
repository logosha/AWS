using CryptoExchanges2.Database;
using MySql.Data.MySqlClient;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace CryptoExchanges2
{
    public class SQLTickStoreService : ITickStoreService
    {
        private readonly string _connectionString;

        public SQLTickStoreService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        public void Add(Instrument instr, Tick tick)
        {
            string queryString = "INSERT INTO TradeData.ExchangeTrade (ExchID, CurrencyID, TradeBase, TradeTimeStamp, BidPrice, AskPrice, " +
                 "BidSize, AskSize, LastPrice, LastSize)" +
                 " VALUES ('" + instr.Exchange + "', '" + instr.Symbol + "', '" + instr.Second + "', '" + tick.ServerTime.ToString("yyyy-MM-dd H:mm:ss") + "', '" + tick.BidPrice + "', '" + tick.AskPrice + "', '" + tick.BidSize + "', '" + tick.AskSize + "', '" + tick.LastPrice + "', '" + tick.LastSize + "')";

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                MySqlCommand command = new MySqlCommand(queryString)
                {
                    Connection = connection
                };

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                finally
                {
                }
            }
        }

        public void Add(Instrument instr, IEnumerable<Tick> tick)
        {
            throw new NotImplementedException();
        }

        public void Clear(Instrument instr)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Tick> Get(Instrument instr)
        {
            string queryString = "SELECT ExchID, CurrencyID, TradeBase, TradeTimeStamp, BidPrice, AskPrice, " +
                "BidSize, AskSize, LastPrice, LastSize" +
                " FROM TradeData.ExchangeTrade WHERE ExchID = " + "'" + instr.Exchange + "' AND CurrencyID = " + "'" + instr.First + "/" + instr.Second + "'";

            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.ConnectionString = _connectionString;
                MySqlCommand command = new MySqlCommand(queryString)
                {
                    Connection = connection
                };
                connection.Open();

                MySqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                        yield return new Tick
                        {
                            ServerTime = reader.GetDateTime(3),
                            BidPrice = reader.GetDouble(4),
                            AskPrice = reader.GetDouble(5),
                            BidSize = reader.GetDouble(6),
                            AskSize = reader.GetDouble(7),
                            LastPrice = reader.GetDouble(8),
                            LastSize = reader.GetDouble(9)
                        };
                }
                finally
                {
                    reader.Close();
                }
            }
        }
    }
}
