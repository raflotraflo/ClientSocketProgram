using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class TCPDataReceiver
    {
        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private const int DelayBeforeReconnecting = 500;
        private const int DelayBetweenPings = 3000;
        private bool _connected;
        public event EventHandler ConnectedChanged;
        private IPEndPoint _iPEndPoint;


        public int BufferSize = 128;

        public TCPDataReceiver(IPEndPoint iPEndPoint)
        {
            _iPEndPoint = iPEndPoint;
        }

        public bool Connected
        {
            get { return _connected; }
            set
            {
                if (_connected != value)
                {
                    _connected = value;
                    ConnectedChanged(this, EventArgs.Empty);
                }
            }
        }

        private async Task ConnectAsync(TcpClient client, CancellationToken cancellationToken)
        {
            bool reconnecting = false;

            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (reconnecting)
                    {
                        await Task.Delay(DelayBeforeReconnecting, cancellationToken);
                    }

                    var tcs = new TaskCompletionSource<bool>();
                    using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                    {
                        Task connectTask = client.ConnectAsync(_iPEndPoint.Address, _iPEndPoint.Port);

                        Task completedTask = await Task.WhenAny(connectTask, tcs.Task);

                        await completedTask;
                        if (completedTask == connectTask)
                        {
                            Connected = true;
                            return;
                        }
                    }
                }
                catch (SocketException)
                {
                    Connected = false;
                    reconnecting = true;
                }
                catch (ObjectDisposedException)
                {
                    //jak bedzie cancel i zrobie close na cliencie
                }
            }
        }

        private async Task CheckPing(CancellationToken cancellationToken)
        {
            using (Ping ping = new Ping())
            {

                while (true)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await Task.Delay(DelayBetweenPings, cancellationToken);

                        var tcs = new TaskCompletionSource<bool>();
                        using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                        {
                            Task<PingReply> sendPingTask = ping.SendPingAsync(_iPEndPoint.Address);
                            Task completedTask = await Task.WhenAny(sendPingTask, tcs.Task);

                            await completedTask;
                            if (completedTask == sendPingTask)
                            {
                                PingReply reply = await sendPingTask;
                                if (reply.Status != IPStatus.Success)
                                {
                                    Connected = false;
                                    return;
                                }
                                Connected = true;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exp)
                    {
                        _logger.Error(exp, "UnknownException. {0}", exp);
                        //if Any error during ping then finish task 
                        return;
                    }
                }


            }
        }


        async Task<byte[]> GetDataAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var client = new TcpClient())
                    {
                        await ConnectAsync(client, cancellationToken);

                        var tcs = new TaskCompletionSource<bool>();
                        using (CancellationTokenSource internalCTS = new CancellationTokenSource())
                        using (CancellationTokenSource linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, internalCTS.Token))
                        using (var networkStream = client.GetStream())
                        using (linkedCTS.Token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                        {
                            byte[] buffer = new byte[1024];

                            Task<int> readTask = networkStream.ReadAsync(buffer, 0, buffer.Length);
                            Task checkPingTask = CheckPing(linkedCTS.Token);
                            Task completedTask = await Task.WhenAny(readTask, tcs.Task, checkPingTask);

                            await completedTask;
                            internalCTS.Cancel();

                            if (completedTask == readTask)
                            {
                                int bytesRead = await readTask;
                                if (bytesRead <= 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    try
                                    {
                                        //return buffer.SkipWhile(x => x != STX).SkipWhile(x => x == STX).TakeWhile(x => x != ETX && x != EOT).ToArray();
                                        return buffer;
                                    }
                                    catch (Exception)
                                    {
                                        throw new FormatException(Encoding.Default.GetString(buffer.Take(bytesRead).ToArray()));
                                    }
                                }
                            }
                            else
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (FormatException fexp)
                {
                    _logger.Error(fexp, "FormatException. {0}", fexp);
                    throw;
                }
                catch (Exception exp)
                {
                    _logger.Error(exp, "UnknownException. {0}", exp);
                    continue;
                }
            }
        }

        async Task<bool> DataRejectedAsync()
        {
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource(3000);
                using (var client = new TcpClient())
                {
                    await ConnectAsync(client, cts.Token);

                    var tcs = new TaskCompletionSource<bool>();
                    using (CancellationTokenSource internalCTS = new CancellationTokenSource())
                    using (CancellationTokenSource linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, internalCTS.Token))
                    using (var networkStream = client.GetStream())
                    using (linkedCTS.Token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                    {
                        byte[] buffer = new byte[] { 7, 7, 7 };


                        Task writeTask = networkStream.WriteAsync(buffer, 0, buffer.Length);
                        Task checkPingTask = CheckPing(linkedCTS.Token);
                        Task completedTask = await Task.WhenAny(writeTask, tcs.Task, checkPingTask);

                        await completedTask;
                        internalCTS.Cancel();

                        if (completedTask == writeTask)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exp)
            {
                _logger.Error(exp, "UnknownException. {0}", exp);
                return false;
            }
        }

    }
}
