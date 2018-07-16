using CryptoExchanges2.Database;
using Shared.Broker;
using Shared.Common;
using Shared.Interfaces;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoExchanges2
{
    public partial class CryptoForm : Form, IBrokerApplication
    {
        private Dictionary<string, MarketBookResponse> _marketList;
        private List<Instrument> _symbolList;
        private ILogger _globalLogger;
        private Timer _timerRefresh;
        private const string Pairs = "Pairs.csv";
        private MainClient _mc;
        ITickStoreService _tickStoreService, _dbStoreService;

        public CryptoForm()
        {
            InitializeComponent();
        }

        private void CryptoForm_Load(object sender, EventArgs e)
        {

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "DB_TeaFiles"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "DB_TeaFiles");
            }
            string fileLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\DB_TeaFiles\\";
            log4net.Config.XmlConfigurator.Configure();
            _globalLogger = new Log4NetWrapper();
            _marketList = new Dictionary<string, MarketBookResponse>();
            _symbolList = new List<Instrument>();
            _tickStoreService = new TeaTickStoreService(fileLocation);
            _dbStoreService = new SQLTickStoreService();
            _timerRefresh = new Timer
            {
                Interval = 1000
            };
            _timerRefresh.Tick += _timer_RefreshGrid;
            _timerRefresh.Start();

            try
            {
                var file = File.ReadAllLines(Pairs);

                foreach (var item in file)
                {
                    try
                    {
                        var itemSplit = item.Split(',');
                        _symbolList.Add(new Instrument()
                        {
                            Exchange = itemSplit[0],
                            First = itemSplit[1],
                            Second = itemSplit[2],
                            Symbol = itemSplit[3],
                            Decimals = Convert.ToInt16(itemSplit[4])
                        });
                    }
                    catch (Exception)
                    {
                        _globalLogger.Log(LogPriority.Error,
                            "Error in line " + item + "," +
                            " File format is incorect. The file format must consist of strings: ExchangeName; Base; Currency; Symbol; Decimals");
                    }
                }
                _mc = new MainClient(this, _globalLogger);
                _mc.StartAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CryptoForm_Closed(object sender, EventArgs e)
        {
            _marketList.Clear();
            _symbolList.Clear();
            _timerRefresh?.Stop();
        }

        private void _timer_RefreshGrid(object sender, EventArgs e)
        {
            for (int i = 0; i < _marketList.Count; ++i)
            {
                if (_marketList.Values.ElementAt(i).isUpdated)
                {
                    for (int j = 3; j < dataGridView.ColumnCount; ++j)
                    {
                        if (j == 4) continue;
                        dataGridView.InvalidateCell(j, i);
                    }
                    _marketList.Values.ElementAt(i).isUpdated = false;
                }
            }
            dataGridView.InvalidateColumn(4);
        }

        public enum MarketColumnIndex
        {
            Exchange = 0,
            Interface = 1,
            Coin = 2,
            Timestamp = 3,
            Elapsed = 4,
            Bid = 5,
            BidSize = 6,
            Ask = 7,
            AskSize = 8,
            Last = 9,
            LastSize = 10
        };

        private string GetType(string exchangeName)
        {
            var type = "";
            switch (_mc.GetConnectorName(exchangeName).GetProperty(PropertyName.IsWebsoketSupport))
            {
                case false:
                    type = "REST";
                    break;
                default:
                    type = "WEBS";
                    break;
            }
            return type;
        }

        public void OnEvent(string exchangeName, BrokerEvent what, string details)
        {
            _globalLogger.Log(LogPriority.Info, $"OnEvent : {what.ToString()} : {details}", exchangeName);

            if (what == BrokerEvent.ConnectorStarted && _marketList.Where(r => r.Value.Name == exchangeName).Count() == 0)
            {
                foreach (var symbol in _symbolList.Where(item => item.Exchange == exchangeName))
                {
                    try
                    {
                        dataGridView.Invoke(new Action(() =>
                        {
                            symbol.Symbol = $"{symbol.First.ToUpper()}/{symbol.Second.ToUpper()}";

                            _marketList.Add(symbol.Id(), new MarketBookResponse()
                            {
                                Type = GetType(exchangeName),
                                Name = exchangeName,
                                Coin = symbol,
                                isUpdated = false,
                                Book = new MarketBook()
                            });
                            dataGridView.RowCount = _marketList.Count;
                        }));

                        var client = _mc.clientList.First(z => z.Name == exchangeName);
                        Task t = client.Subscribe(symbol, SubscriptionModel.TopBook);
                        t.Wait();
                    }
                    catch (Exception ex)
                    {
                        _globalLogger.Log(LogPriority.Error, $"OnEvent : {ex.Message}");
                    }
                }
            }
        }

        public void OnReport(string exchangeName, string symbol, MarketBook topOfBook)
        {
            if (_marketList.TryGetValue(symbol, out MarketBookResponse result))
            {
                if (result.Book.AskPrice != topOfBook.AskPrice
                    || result.Book.AskSize != topOfBook.AskSize
                    || result.Book.BidPrice != topOfBook.BidPrice
                    || result.Book.BidSize != topOfBook.BidSize
                    || result.Book.LastPrice != topOfBook.LastPrice
                    || result.Book.LastSize != topOfBook.LastSize)
                {
                    result.Book = topOfBook;
                    result.isUpdated = true;
                }

                Tick tick = new Tick()
                {
                    AskPrice = topOfBook.AskPrice,
                    AskSize = topOfBook.AskSize,
                    BidPrice = topOfBook.BidPrice,
                    BidSize = topOfBook.BidSize,
                    LastPrice = topOfBook.LastPrice,
                    LastSize = topOfBook.LastSize,
                    LocalTime = topOfBook.LocalTime,
                    ServerTime = topOfBook.EstTime
                };

                try
                {
                    _tickStoreService.Add(result.Coin, tick);
                }
                catch (Exception ex)
                {
                    _globalLogger.Log(LogPriority.Error, $"OnEvent : {ex.Message}");
                }

                try
                {
                    _dbStoreService.Add(result.Coin, tick);
                }
                catch (Exception ex)
                {
                    _globalLogger.Log(LogPriority.Error, $"OnEvent : {ex.Message}");
                }
            }
        }

        private void dataGridView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.ColumnIndex == -1 || e.RowIndex >= _marketList.Count) return;
            var result = _marketList.Values.ElementAt(e.RowIndex);
            var book = result.Book;
            var prec = "n" + result.Coin.Decimals.ToString();
            switch (e.ColumnIndex)
            {
                case (int)MarketColumnIndex.Exchange:
                    e.Value = result.Name;
                    break;
                case (int)MarketColumnIndex.Interface:
                    e.Value = result.Type;
                    break;
                case (int)MarketColumnIndex.Coin:
                    e.Value = result.Coin.Symbol;
                    break;
                case (int)MarketColumnIndex.Timestamp:
                    e.Value = book.EstTime.ToString("hh:mm:ss");
                    break;
                case (int)MarketColumnIndex.Elapsed:
                    if (book.LocalTime != DateTime.MinValue)
                    {
                        var resTime = (int)(DateTime.UtcNow - book.UtcTime).TotalSeconds;
                        e.Value = resTime.ToString();
                    }
                    break;
                case (int)MarketColumnIndex.Bid:
                    if (book.BidPrice == 0.0)
                    {
                        e.Value = "no data";
                    }
                    else
                    {
                        e.Value = book.BidPrice.ToString(prec);
                    }
                    break;
                case (int)MarketColumnIndex.BidSize:
                    if (book.BidSize == 0.0)
                    {
                        e.Value = "no data";
                    }
                    else
                    {
                        e.Value = book.BidSize.ToString(prec);
                    }
                    break;
                case (int)MarketColumnIndex.Ask:
                    if (book.AskPrice == 0.0)
                    {
                        e.Value = "no data";
                    }
                    else
                    {
                        e.Value = book.AskPrice.ToString(prec);
                    }
                    break;
                case (int)MarketColumnIndex.AskSize:
                    if (book.AskSize == 0.0)
                    {
                        e.Value = "no data";
                    }
                    else
                    {
                        e.Value = book.AskSize.ToString(prec);
                    }
                    break;
                case (int)MarketColumnIndex.Last:
                    if (book.LastPrice == 0.0)
                    {
                        e.Value = "no data";
                    }
                    else
                    {
                        e.Value = book.LastPrice.ToString(prec);
                    }
                    break;
                case (int)MarketColumnIndex.LastSize:
                    if (book.LastSize == 0.0)
                    {
                        e.Value = "no data";
                    }
                    else
                    {
                        e.Value = book.LastSize.ToString(prec);
                    }
                    break;
            }
        }

        private void _timer_DateTime(object sender, EventArgs e)
        {
            label1.Text = "Date: " + DateTime.Now.ToString("yyyy-MM-dd");
            label2.Text = "Local Time: " + DateTime.Now.ToString("HH:mm");
        }

        private void dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            String columnName = dataGridView.Columns[e.ColumnIndex].Name;

            if (columnName.Equals("Exchange"))
            {
                var res = _marketList.OrderBy(t => t.Value.Name).ToDictionary(pair => pair.Key, pair => pair.Value);
                _marketList.Clear();
                _marketList = new Dictionary<string, MarketBookResponse>(res);
            }
            if (columnName.Equals("Coin"))
            {
                var res = _marketList.OrderBy(t => t.Value.Coin).ToDictionary(pair => pair.Key, pair => pair.Value);
                _marketList.Clear();
                _marketList = new Dictionary<string, MarketBookResponse>(res);
            }
            dataGridView.Refresh();
        }
    }
}
