using CryptoExchanges2;
using Shared.Broker;
using Shared.Common;
using Shared.Interfaces;
using System;
using System.Globalization;
using Shared.Models;
using CryptoExchanges2.Database;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace ConnectorsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger globalLogger = new ConsoleLogger();
            IBrokerApplication application = new BrokerNullApplication(globalLogger);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            MainClient mc = new MainClient(application, globalLogger);

            string fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\DB_TeaFiles\\";

            ITickStoreService _tickStoreService = new TeaTickStoreService(fileLocation);
            ITickStoreService _sqlStoreService = new SQLTickStoreService();

            Console.WriteLine("Please enter the command in the format: \n" +
                                       "start connector_name\n" +
                                       "or\n" +
                                       "stop connector_name\n" +
                                       "or\n" +
                                       "s connector_name base currency\n" +
                                       "or\n" +
                                       "u connector_name base currency\n" +
                                       "or\n" +
                                       "getTicks connector_name base currency\n" +
                                       "or\n" +
                                       "db connector_name base currency\n");

            Console.WriteLine("To obtain the connector names, enter list");

            while (true)
            {
                string[] command = Console.ReadLine().Split();

                if (command[0] == "start" && command.Length == 2)
                {
                    if (mc.SuppertedConnectors().Contains((command[1])))
                    {
                        try
                        {
                            var test = mc.GetConnectorName(command[1]);
                            var task = test.Start();
                            task.Wait();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exchange Name is incorrect");
                    }
                }
                else
                if (command[0] == "stop" && command.Length == 2)
                {
                    if (mc.SuppertedConnectors().Contains((command[1])))
                    {
                        try
                        {
                            var test = mc.GetConnectorName(command[1]);
                            var task = test.Stop();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exchange Name is incorrect");
                    }
                }
                else
                if (command[0] == "s" && command.Length == 4)
                {
                    if (mc.SuppertedConnectors().Contains((command[1])))
                    {
                        try
                        {
                            var test = mc.GetConnectorName(command[1]);
                            var task = test.Subscribe(
                                new Instrument() { Exchange = command[1], First = command[2], Second = command[3] },
                                SubscriptionModel.TopBook);
                            task.Wait();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exchange Name is incorrect");
                    }
                }
                else
                if (command[0] == "u" && command.Length == 4)
                {
                    if (mc.SuppertedConnectors().Contains((command[1])))
                    {
                        try
                        {
                            var test = mc.GetConnectorName(command[1]);
                            var task = test.Unsibscribe(
                                new Instrument() { Exchange = command[1], First = command[2], Second = command[3] },
                                SubscriptionModel.TopBook);
                            task.Wait();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exchange Name is incorrect");
                    }
                }
                else
                if (command[0] == "getTicks" && command.Length == 4)
                {
                    if (mc.SuppertedConnectors().Contains((command[1])))
                    {
                        try
                        {
                            var result = _tickStoreService.Get(new Instrument() { Exchange = command[1], First = command[2], Second = command[3] });
                            Console.WriteLine("Count = " + result.Count());
                            var last = result.Last();
                            Console.WriteLine("Last Item: Ask Price = " + last.AskPrice
                                                         + ", Ask Size = " + last.AskSize
                                                         + ", Bid Price = " + last.BidPrice
                                                         + ", Bid Size = " + last.BidSize
                                                         + ", Last Price = " + last.LastPrice
                                                         + ", Last Size = " + last.LastSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exchange Name is incorrect");
                    }
                }
                else
                if (command[0] == "db" && command.Length == 4)
                {
                    if (mc.SuppertedConnectors().Contains(command[1]))
                    {
                        try
                        {
                            var result = _sqlStoreService.Get(new Instrument { Exchange = command[1], First = command[2], Second = command[3] });
                            Console.WriteLine("Count = " + result.Count());
                            var last = result.Last();
                            Console.WriteLine("Last Item: Ask Price = " + last.AskPrice
                                                                        + ", Ask Size = " + last.AskSize
                                                                        + ", Bid Price = " + last.BidPrice
                                                                        + ", Bid Size = " + last.BidSize
                                                                        + ", Last Price = " + last.LastPrice
                                                                        + ", Last Size = " + last.LastSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Exchange Name is incorrect");
                    }
                }
                else
                if (command[0] == "list")
                {
                    foreach (var item in mc.SuppertedConnectors())
                    {
                        Console.WriteLine(item);
                    }
                }
                else
                {
                    Console.WriteLine("Command is incorrect");
                }
             }
        }
    }
}
