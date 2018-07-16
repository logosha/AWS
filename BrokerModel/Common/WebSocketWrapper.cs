using System;
using System.Threading;
using Shared;
using Shared.Broker;
using Shared.Interfaces;
using Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace Shared.Common
{
    public class WebSocketWrapper : IDataStream
    {
        private CancellationTokenSource _source;
        private uint keepAliveSocketInterval = 3;
        private uint keepAliveSecs = 5;

        private ClientWebSocket _webSocketClient;

        private IPeriodicTask tickerchnl;


        private readonly string _uri;
        private readonly string _chnlName;
        private readonly string _name;
        private ILogger _logger;

        public bool IsConnected
        {
            get
            {
                return ((_webSocketClient != null && _webSocketClient.State == WebSocketState.Open) ? true : false);
            }
        }

        public WebSocketState Socket_state { get; private set; } = WebSocketState.None;

        public WebSocketWrapper(string Uri, string chnlName, ILogger logger, string name)
        {
            _uri = Uri;
            _name = name;
            _logger = logger;
            _chnlName = chnlName;
        }

        private async Task SocketConnect(ParseMessage parseMessage)
        {
            if (_webSocketClient.State != WebSocketState.Open)
            {
                //State_check();
                try
                { await _webSocketClient.ConnectAsync(new Uri(_uri), new CancellationToken()).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    //_logger.Log(LogPriority.Debug, "Connection unaviable");
                }
            }
            if (_webSocketClient.State == WebSocketState.Open)
            {
                //State_check();
                var connected = new SocketEventHandlerArgs
                {
                    chnlName = _chnlName,
                    type = StreamMessageType.Logon
                };
                await parseMessage(connected);
                while (_webSocketClient.State == WebSocketState.Open && !_source.Token.IsCancellationRequested)
                {
                    using (var stream = new System.IO.MemoryStream(1024))
                    {
                        WebSocketReceiveResult webSocketReceiveResult;
                        do
                        {
                            var receiveBuffer = new ArraySegment<byte>(new byte[1024 * 8]);
                            try
                            {
                                webSocketReceiveResult = await _webSocketClient.ReceiveAsync(receiveBuffer, _source.Token);
                                if (webSocketReceiveResult.CloseStatus != null || webSocketReceiveResult.CloseStatusDescription != null)
                                    _logger.Log(LogPriority.Debug, $"Server request for closing: CloseStatus - {webSocketReceiveResult.CloseStatus}, Description - {webSocketReceiveResult.CloseStatusDescription}", _name); if (webSocketReceiveResult.Count == 0)
                                {
                                    _logger.Log(LogPriority.Debug, $"Websocket empty msg", _name);
                                }
                                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                                {
                                    _logger.Log(LogPriority.Debug, $"Websocket received {webSocketReceiveResult.MessageType}", _name);
                                }
                                if (webSocketReceiveResult.CloseStatus != null)
                                {
                                    _logger.Log(LogPriority.Debug, $"Websocket received {webSocketReceiveResult.CloseStatus} reason:{webSocketReceiveResult.CloseStatusDescription}", _name);
                                }
                            }
                            catch (Exception ex)
                            {
                                await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", new CancellationToken());
                                //State_check();
                                var disconnected = new SocketEventHandlerArgs
                                {
                                    chnlName = _chnlName,
                                    type = StreamMessageType.Logout
                                };
                                await parseMessage(disconnected);
                                return;
                            }
                            await stream.WriteAsync(receiveBuffer.Array, receiveBuffer.Offset, receiveBuffer.Count);
                            //State_check();
                        }
                        while (!webSocketReceiveResult.EndOfMessage || Convert.ToChar(stream.GetBuffer()[stream.Length - 1]) != 0);

                        var stream_to_msg = stream.ToArray().Where(b => b != 0).ToArray();
                        var message = new SocketEventHandlerArgs
                        {
                            msg = stream_to_msg,
                            chnlName = _chnlName,
                            type = StreamMessageType.Data
                        };

                        try
                        {
                            await parseMessage(message);
                        }
                        catch (Exception ex)
                        {
                            //State_check();
                            //_internal_error(message, $" Exception: {ex.Message}  stack {ex.StackTrace}");
                            var encode_error = Encoding.ASCII.GetBytes($" Exception: {ex.Message}  stack {ex.StackTrace}");
                            var error = new SocketEventHandlerArgs
                            {
                                msg = encode_error,
                                chnlName = _chnlName,
                                type = StreamMessageType.Error
                            };
                            await parseMessage(error);
                        }
                    }
                }
            }
        }

        public async Task Start(ParseMessage parseMessage)
        {
            _source = new CancellationTokenSource();

            PeriodicTaskAction<object, CancellationToken> keepChannel = (async (object o, CancellationToken t) =>
            {
                if (_webSocketClient == null)
                {
                    _webSocketClient = new ClientWebSocket();
                    _webSocketClient.Options.KeepAliveInterval = new TimeSpan(keepAliveSocketInterval * TimeSpan.TicksPerSecond);
                    await SocketConnect(parseMessage);
                }
                else if (_webSocketClient.State != WebSocketState.Open && _webSocketClient.State != WebSocketState.Connecting)
                {
                    _webSocketClient.Dispose();
                    _webSocketClient = new ClientWebSocket();
                    _webSocketClient.Options.KeepAliveInterval = new TimeSpan(keepAliveSocketInterval * TimeSpan.TicksPerSecond);
                    await SocketConnect(parseMessage);
                }
            }
            );

            tickerchnl = new PeriodicTaskWrapper(_logger, _name);
            await tickerchnl.Start(keepChannel, new PeriodicTaskParams { period = (int)keepAliveSecs * 1000 }, _source.Token);

        }

        public async Task Stop()
        {
            await Task.Yield();
            _source.Cancel();
        }

        public async Task SendCommand(byte[] bytes)
        {
            ArraySegment<byte> subscriptionMessageBuffer = new ArraySegment<byte>(bytes);
            if (_webSocketClient != null && _webSocketClient.State == WebSocketState.Open)
            {
                await _webSocketClient.SendAsync(subscriptionMessageBuffer, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            }
        }

    }

}
