using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace ClientSocketProgram
{
    [Serializable]
    public class SequenceControllerCommunication : ISequenceControllerCommunicationData, ISequenceControllerForVisuControl
    {
        public event EventHandler<SequenceControllerCommunicationDataEventArgs> DeleteOrder;
        public event EventHandler<SequenceControllerCommunicationDataEventArgs> InsertOrder;
        public event EventHandler SendDataChanged;
        public event EventHandler ReceiveDataChanged;
        public event EventHandler ErrorChanged;
        public event EventHandler StartedChanged;

        private TcpClient _client;
        private IMapper<byte[], CommunicationDataFrame> _mapper = new CommunicationDataFrameMapper();
        private CommunicationDataFrame _lastReceiveData = new CommunicationDataFrame();
        private CommunicationDataFrame _lastSendData = new CommunicationDataFrame();
        private IPAddress _ipAddress;
        private Semaphore _semaphoreSend = new Semaphore(1, 1);
        private Semaphore _semaphoreReconect = new Semaphore(1, 1);
        private Task _task;
        private bool _lifeBit;
        private bool _alarmConnection;
        private bool _successLoopReconnect = false;
        private bool _isActivated;
        private int _port;
        private int _bufferSize = CommunicationDataFrame.NumberOfBytes;
        private CancellationTokenSource _cancellationTokenReciveData;

        private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private static System.Timers.Timer _timerCounter;
        private const int TimerCounter = 1000;          //czas w milisekundach
        private int _connectionCounter = 0;
        private const int ConnectionAlarmTimeout = 100;  //czas w sekundach
        private const int ConnectionTimeout = 120;       //czas w sekundach

        private DataBitsFromSequenceControler _sendDataBits = new DataBitsFromSequenceControler();
        private DataBitsFromSequenceControler _receiveDataBits = new DataBitsFromSequenceControler();
        private ErrorBitsFromSequenceControler _errorBits = new ErrorBitsFromSequenceControler();


        //private Alarm _sequenceControllerCommunication_Stopped;
        //private Alarm _sequenceControllerCommunication_NoConnection;

        public SequenceControllerCommunication(string ip, int port)
        {
            //_sequenceControllerCommunication_Stopped = new Alarms.Alarm(Alarms.AlarmType.Warning);
            //_sequenceControllerCommunication_Stopped.Info.Name = "SequenceControllerCommunication Stopped";
            //_sequenceControllerCommunication_Stopped.Info.DeviceType = "SequenceController";
            //_sequenceControllerCommunication_Stopped.Info.DeviceName = "SequenceController";

            //_sequenceControllerCommunication_NoConnection = new Alarms.Alarm();
            //_sequenceControllerCommunication_NoConnection.Info.Name = "SequenceControllerCommunication NoConnection";
            //_sequenceControllerCommunication_NoConnection.Info.DeviceType = "SequenceController";
            //_sequenceControllerCommunication_NoConnection.Info.DeviceName = "SequenceController";

            _port = port;
            _ipAddress = IPAddress.Parse(ip);
            AlarmConnection = false;
            //_sequenceControllerCommunication_Stopped.Rise("Stopped");
        }

        private async Task<bool> SaveAsync(IPAddress ipAddress, int port)
        {
            bool status = true;

            _ipAddress = ipAddress;
            _port = port;

            if (Started)
            {
                Stop();
                status = await StartAsync().ConfigureAwait(false);
            }

            return status;
        }

        private async Task<bool> StartAsync()
        {
            bool success = false;
            Started = true;

            _logger.Debug("Connect(ipAddress ={0}, port={1}", _ipAddress.ToString(), _port);

            //sprawdzenie parametrów
            success = await ReconnectAsync().ConfigureAwait(false);

            _connectionCounter = 0;

            if (success)
            {

                _timerCounter = new System.Timers.Timer(TimerCounter);
                _timerCounter.AutoReset = true;
                _timerCounter.Elapsed += async (s, e) => { await TimerCounter_Elapsed(s, e); };
                _timerCounter.Start();
                GC.KeepAlive(_timerCounter);

                var cancellationTokenSourceHelper = _cancellationTokenReciveData;
                if (cancellationTokenSourceHelper != null)
                {
                    cancellationTokenSourceHelper.Cancel();
                    await _task.ConfigureAwait(false);
                    cancellationTokenSourceHelper.Dispose();
                }

                _cancellationTokenReciveData = new CancellationTokenSource();


                _task = new Task(async () =>
                {
                    try
                    {
                        await ReceiveAsync(_cancellationTokenReciveData.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Debug("Ended Task ReceiveAsync()");
                    }
                },
                        TaskCreationOptions.LongRunning);

                _task.Start();
                _logger.Debug("_task.Start(await ReceiveAsync(_cancellationTokenReciveData.Token))");
            }

            return success;
        }

        private async Task<bool> ReconnectAsync()
        {
            _semaphoreReconect.WaitOne();
            _successLoopReconnect = false;

            while (!_successLoopReconnect && Started)
            {
                try
                {
                    if (_client != null)
                    {
                        _client.Close();
                    }
                    _client = new TcpClient();

                    _logger.Debug("_client.Connect(ipAddress ={0}, port={1}", _ipAddress.ToString(), _port);

                    await _client.ConnectAsync(_ipAddress, _port).ConfigureAwait(false);

                    AlarmConnection = false;
                    _successLoopReconnect = true;
                    _connectionCounter = 0;

                    _logger.Info("Connected to {0},{1}", _ipAddress, _port);

                }
                catch (Exception exp)
                {
                    AlarmConnection = true;
                    _successLoopReconnect = false;
                    _logger.Error("Error while connecting to ({0}:{1}). {2}.", _ipAddress, _port, exp);
                }
                if (!_successLoopReconnect)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }

            _semaphoreReconect.Release();
            return _successLoopReconnect;
        }

        public async void Stop()
        {
            Started = false;


            var cancellationTokenSourceHelper = _cancellationTokenReciveData;
            if (cancellationTokenSourceHelper != null)
            {
                if (cancellationTokenSourceHelper.Token != null)
                    if (cancellationTokenSourceHelper.Token.CanBeCanceled)
                    {
                        cancellationTokenSourceHelper.Cancel();
                        await _task.ConfigureAwait(false);
                    }

                cancellationTokenSourceHelper.Dispose();
            }
            _cancellationTokenReciveData = null;

            if (_client != null)
            {
                _client.Close();
            }

            if (_timerCounter != null)
            {
                _timerCounter.Stop();
            }

            _logger.Info("Disconnect()");
        }

        private async Task TimerCounter_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Interlocked.Increment(ref _connectionCounter);

            AlarmConnection = (_connectionCounter < ConnectionAlarmTimeout) ? false : true;
            if (_connectionCounter % ConnectionTimeout == 0)
            {
                _logger.Debug("_connectionCounter = {0}, Call ReConnect()", _connectionCounter);
                if (_successLoopReconnect)
                {
                    await ReconnectAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (Started)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] buffer = new byte[_bufferSize];
                    int bytesRead = 0;

                    //using (NetworkStream stream = _client.GetStream())
                    //{
                    //    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    //}

                    bytesRead = await _client.GetStream().ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);


                    if (bytesRead <= 0)
                    {
                        continue;
                    }


                    CommunicationDataFrame receiveData = _mapper.Map(buffer);
                    _logger.Debug("Receive frame, length = {0}, lifeBbit={1} ", bytesRead, receiveData.LifeBit);
                    bool isNewRequest = CheckFrame(receiveData, _lastReceiveData);

                    try
                    {
                        receiveData.Insert = false;
                        receiveData.Delete = false;
                        _logger.Debug("Send frame from Task ReceiveAsync()");
                        await SendAsync(receiveData, isNewRequest).ConfigureAwait(false);
                    }
                    catch(Exception)
                    { }

                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exp)
                {
                    _logger.Error("Error in Task ReceiveAsync() error: {0}", exp.ToString());
                }
            }

        }

        private async Task SendAsync(CommunicationDataFrame frame, bool newRequest)
        {
            try
            {
                _semaphoreSend.WaitOne();

                bool isConnected = false;
                var client = _client;
                if (client != null)
                {
                    isConnected = client.Connected;
                }

                if (isConnected)
                {
                    if (!newRequest) //zapewnia wysyłkę tej samej ramki dopóki serwer nie informuje o prawidłowym odebraniu
                    {
                        frame = _lastSendData.GetCopy();
                    }
                    else
                    {
                        ;
                    }

                    frame.LifeBit = _lifeBit;
                    byte[] data = _mapper.InverseMap(frame);

                    try
                    {
                        //using (NetworkStream stream = _client.GetStream())
                        //{
                        //    await stream.WriteAsync(data, 0, data.Length);
                        //}
                        await _client.GetStream().WriteAsync(data, 0, data.Length).ConfigureAwait(false);

                        _logger.Debug("Send frame: length={0}, lifBit={1}, lbhd={2}", data.Length, frame.LifeBit, frame.LBHD);
                    }
                    catch (Exception)
                    {
                        _logger.Error("Error while sending frame: length={0}, lifBit={1}, lbhd={2}", data.Length, frame.LifeBit, frame.LBHD);
                       // throw new SendingFailedException("Wysyłanie nie powiodło się.");
                    }

                    if (IsNewRequest(frame, _lastSendData))
                    {
                        _lastSendData = frame;
                        OnErrorChanged();
                    }
                    OnSendDataChanged();
                }
                else
                {
                    _logger.Error("Without connection while sending frame: lifBit={1}, lbhd={2}", frame.LifeBit, frame.LBHD);
                    //throw new SendingFailedException("Sending error. No connecion.");
                }
            }
            finally
            {
                _semaphoreSend.Release();
            }
        }

        private bool CheckFrame(CommunicationDataFrame newframe, CommunicationDataFrame oldFrame)
        {
            bool isNewRequest = IsNewRequest(newframe, oldFrame);

            if (isNewRequest)
            {
                _logger.Debug("New frame");
                SequenceControllerCommunicationDataEventArgs eventArgs = new SequenceControllerCommunicationDataEventArgs();
                eventArgs.LBHD = newframe.LBHD;
                eventArgs.BalancingDate = DateTime.Now;

                if (newframe.Delete)
                {
                    OnDeleteOrder(eventArgs);
                }
                if (newframe.Insert)
                {
                    OnInsertOrder(eventArgs);
                }
                _lastReceiveData = newframe.GetCopy();
            }

            if (newframe.LifeBit != _lifeBit)
            {
                Interlocked.Exchange(ref _connectionCounter, 0);
                _lifeBit = newframe.LifeBit;
                OnReceiveDataChanged();
            }

            return isNewRequest;
        }

        private bool IsNewRequest(CommunicationDataFrame newframe, CommunicationDataFrame oldFrame)
        {
            _logger.Debug("Check if received a new frame");

            if (newframe.Broadcast != oldFrame.Broadcast)
                return true;
            if (newframe.Delete != oldFrame.Delete)
                return true;
            if (newframe.Insert != oldFrame.Insert)
                return true;
            if (newframe.LBHD != oldFrame.LBHD)
                return true;
            if (newframe.NotFound != oldFrame.NotFound)
                return true;
            if (newframe.Operation != oldFrame.Operation)
                return true;
            if (newframe.PLPID != oldFrame.PLPID)
                return true;
            if (newframe.Prefix != oldFrame.Prefix)
                return true;
            if (newframe.Sequence != oldFrame.Sequence)
                return true;

            return false;
        }

        //private void ModifySendFrame(ref CommunicationDataFrame sendFrame, bool newRequest)
        //{
        //    if (_lastSendData.Delete && _lastReceiveData.Delete || _lastSendData.Insert && _lastReceiveData.Insert)
        //        sendFrame = _lastReceiveData.GetCopy();        
        //}

        private void OnDeleteOrder(SequenceControllerCommunicationDataEventArgs eventArgs)
        {
            _logger.Debug("Received frame: Delete order: {0}, BalancingData: {1}", eventArgs.LBHD, eventArgs.BalancingDate);
            if (DeleteOrder != null)
            {
                DeleteOrder(this, eventArgs);
            }
        }
        private void OnInsertOrder(SequenceControllerCommunicationDataEventArgs eventArgs)
        {
            _logger.Debug("Received frame: Insert order: {0}, BalancingData: {1}", eventArgs.LBHD, eventArgs.BalancingDate);
            if (InsertOrder != null)
            {
                InsertOrder(this, eventArgs);
            }
        }
        private void OnSendDataChanged()
        {
            _logger.Debug("Changed sending frame OnSendDataChanged()");
            _sendDataBits.BalancingDate = DateTime.Now;

            _sendDataBits.Delete = _lastSendData.Delete;
            _sendDataBits.Insert = _lastSendData.Insert;
            _sendDataBits.LBHD = _lastSendData.LBHD;
            _sendDataBits.LifeBit = _lastSendData.LifeBit;
            _sendDataBits.Prefix = _lastSendData.Prefix;
            _sendDataBits.LifeBit = _lifeBit;

            if (_lastSendData.NotFound || _lastSendData.UnknownError || _lastSendData.NotDelete || _lastSendData.NotInsert || _lastSendData.SOAP)
                OnErrorChanged();

            if (SendDataChanged != null)
                SendDataChanged(this, EventArgs.Empty);
        }
        private void OnReceiveDataChanged()
        {
            _logger.Debug("Changed receiving frame OnReceiveDataChanged()");
            _receiveDataBits.BalancingDate = DateTime.Now;
            _receiveDataBits.Delete = _lastReceiveData.Delete;
            _receiveDataBits.Insert = _lastReceiveData.Insert;
            _receiveDataBits.LBHD = _lastReceiveData.LBHD;
            _receiveDataBits.LifeBit = _lifeBit;
            _receiveDataBits.Prefix = _lastReceiveData.Prefix;

            if (ReceiveDataChanged != null)
                ReceiveDataChanged(this, EventArgs.Empty);
        }
        private void OnErrorChanged()
        {
            _logger.Debug("Call error OnErrorChanged()");
            _errorBits.ConnectionAlarm = AlarmConnection;
            _errorBits.LBHDNotFound = _lastSendData.NotFound;
            _errorBits.UnknownError = _lastSendData.UnknownError;
            _errorBits.OrderNotDeleted = _lastSendData.NotDelete;
            _errorBits.OrderNotInserted = _lastSendData.NotInsert;
            _errorBits.OrderWithSOP = _lastSendData.SOAP;

            if (ErrorChanged != null)
                ErrorChanged(this, EventArgs.Empty);
        }

        public bool AlarmConnection
        {
            get { return _alarmConnection; }
            private set
            {
                if (_alarmConnection != value)
                {
                    _logger.Debug("Change status connection: ", value);
                    _alarmConnection = value;
                    //if (_alarmConnection)
                    //{
                    //    _sequenceControllerCommunication_NoConnection.Rise(String.Format("NoConnection to {0}, port {1}", _ipAddress, _port));
                    //}
                    //else
                    //{
                    //    _sequenceControllerCommunication_NoConnection.Fall();
                    //}
                    OnErrorChanged();
                }
            }
        }

        public bool Started
        {
            get { return _isActivated; }
            private set
            {
                if (_isActivated != value)
                {
                    // _logger.Debug("Change status connection: ", value);
                    _isActivated = value;
                    //if (_isActivated)
                    //{
                    //    _sequenceControllerCommunication_Stopped.Fall();
                    //}
                    //else
                    //{
                    //    _sequenceControllerCommunication_Stopped.Rise("Stopped");
                    //}
                    if (StartedChanged != null)
                        StartedChanged(this, EventArgs.Empty);
                }
            }
        }

        #region ISequenceControllerCommunicationData

        private async Task SendNewAsync(CommunicationDataFrame frame)
        {
            try
            {
                await SendAsync(frame, true).ConfigureAwait(false);
            }
            catch (Exception)
            { }
        }

        public async Task LBHDnotFoundAsync(string lbhd)
        {
            _logger.Debug("Call method: LBHDnotFound({0})", lbhd);
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.NotFound = true;
            data.LBHD = lbhd;

            await SendNewAsync(data).ConfigureAwait(false);
        }

        public async Task OrderWithSOPAsync(string lbhd)
        {
            _logger.Debug("Call method: OrderWithSOPAsync({0},)", lbhd);
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.SOAP = true;
            data.LBHD = lbhd;

            await SendNewAsync(data).ConfigureAwait(false);
        }

        public async Task OrderInsertedAsync(string lbhd)
        {
            _logger.Debug("Call method: OrderInsertedAsync({0},)", lbhd);
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.Insert = true;
            data.LBHD = lbhd;

            await SendNewAsync(data).ConfigureAwait(false);

        }

        public async Task OrderNotInsertedAsync(string lbhd)
        {
            _logger.Debug("Call method: OrderNotInsertedAsync({0})", lbhd);
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.NotInsert = true;
            data.LBHD = lbhd;

            await SendNewAsync(data).ConfigureAwait(false);
        }

        public async Task OrderDeletedAsync(string lbhd)
        {
            _logger.Debug("Call method: OrderDeletedAsync({0})", lbhd);
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.Delete = true;
            data.LBHD = lbhd;

            await SendNewAsync(data).ConfigureAwait(false);
        }

        public async Task OrderNotDeletedAsync(string lbhd)
        {
            _logger.Debug("Call method: OrderNotDeletedAsync({0})", lbhd);
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.NotDelete = true;
            data.LBHD = lbhd;

            await SendNewAsync(data).ConfigureAwait(false);
        }

        public async Task UnknownErrorAsync()
        {
            _logger.Debug("Call method: UnknownErrorAsync()");
            CommunicationDataFrame data = new CommunicationDataFrame();
            data.UnknownError = true;

            await SendNewAsync(data).ConfigureAwait(false);
        }


        #endregion


        #region ISequenceControllerForVisuControl

        async Task<bool> ISequenceControllerForVisuControl.SaveAsync(IPAddress ipAddress, int port)
        {
            return await SaveAsync(ipAddress, port).ConfigureAwait(false);
        }

        async Task<bool> ISequenceControllerForVisuControl.SaveAsync(string ipAddress, int port)
        {
            IPAddress ip = IPAddress.Parse(ipAddress);
            return await SaveAsync(ip, port).ConfigureAwait(false);
        }

        async Task<bool> ISequenceControllerForVisuControl.StartAsync()
        {
            if (Started)
            {
                return true;
            }
            else
            {
                return await StartAsync().ConfigureAwait(false);
            }
        }

        void ISequenceControllerForVisuControl.Stop()
        {
            if (Started)
            {
                Stop();
            }
        }

        DataBitsFromSequenceControler ISequenceControllerForVisuControl.SendDataBits
        {
            get
            {
                return _sendDataBits;
            }
        }

        DataBitsFromSequenceControler ISequenceControllerForVisuControl.ReceiveDataBits
        {
            get
            {
                return _receiveDataBits;
            }
        }

        ErrorBitsFromSequenceControler ISequenceControllerForVisuControl.ErrorBits
        {
            get
            {
                return _errorBits;
            }
        }

        IPAddress ISequenceControllerForVisuControl.IP
        {
            get
            {
                return _ipAddress;
            }
        }

        int ISequenceControllerForVisuControl.Port
        {
            get
            {
                return _port;
            }
        }

        bool ISequenceControllerForVisuControl.Started
        {
            get
            {
                return Started;
            }
        }

        #endregion


        public void OnInsertOrder(string lbhd)// do testow
        {
            SequenceControllerCommunicationDataEventArgs BEA = new SequenceControllerCommunicationDataEventArgs();
            BEA.LBHD = lbhd;
            if (InsertOrder != null)
            {
                InsertOrder(this, BEA);
            }
        }
        public void OnDeleteOrder(string lbhd)// do testow
        {
            SequenceControllerCommunicationDataEventArgs BEA = new SequenceControllerCommunicationDataEventArgs();
            BEA.LBHD = lbhd;
            if (DeleteOrder != null)
            {
                DeleteOrder(this, BEA);
            }
        }
    }
}
