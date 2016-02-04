using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    class SequenceControllerCommunication : ISequenceControllerCommunicationData, ISequenceControllerForVisuControl
    {
        public event EventHandler<SequenceControllerCommunicationDataEventArgs> DeleteOrder;
        public event EventHandler<SequenceControllerCommunicationDataEventArgs> InsertOrder;
        public event EventHandler SendDataChanged;
        public event EventHandler ReceiveDataChanged;
        public event EventHandler ErrorChanged;

        private TcpClient _client;
        private IMapper<byte[], DataModel> _mapper = new DataMapper();
        private DataModel _receiveData = new DataModel();
        private DataModel _oldReceiveData = new DataModel();
        private DataModel _sendData = new DataModel();
        private IPAddress _ipAddress;
        private Semaphore _semaphore = new Semaphore(1, 1);
        private Task _task;
        private bool _lifeBit;
        private bool _isConnected;
        private int _port;
        private int _bufferSize = DataModel.NumberOfBytes;
        private CancellationTokenSource _cancellationTokenReciveData;

 
        public SequenceControllerCommunication()
        {
         
        }

        public void Connect(string ipAddress, int port)
        { 
            //CheckClientAlreadyConnected();
            _ipAddress = IPAddress.Parse(ipAddress);
            _port = port;

            _client = new TcpClient();
            _client.Connect(_ipAddress, _port);

        }
        public void Connect(IPAddress ipAddress, int port)
        {
            // CheckClientAlreadyConnected();
            _ipAddress = ipAddress;
            _port = port;

            _client = new TcpClient();
            _client.Connect(ipAddress, port);

            //CallOnConnect(this);

            //StartReceiveFrom(this);
        }
        public void Disconnect()
        {
            Stop();
            _client.Close();
        }

        public void Start()
        {
            if(_cancellationTokenReciveData != null)
            {
                _cancellationTokenReciveData.Dispose();
            }

            _cancellationTokenReciveData = new CancellationTokenSource();

            _task = new Task(async () => await ReceiveAsync(_cancellationTokenReciveData.Token), TaskCreationOptions.LongRunning);
            _task.Start();

        }
        public void Stop()
        {
            _cancellationTokenReciveData.Cancel();
        }

        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    NetworkStream stream = _client.GetStream();
                    byte[] buffer = new byte[_bufferSize];

                    while (_client.Connected)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        _receiveData = _mapper.Map(buffer);
                        _lifeBit = _receiveData.LifeBit;

                        CheckFrame(_receiveData, _oldReceiveData);
                       
                        await SendAsync(_receiveData);
                    }

                   //powtórne połaczenie

                }
                catch (OperationCanceledException)
                {
                   //Działa :) 
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Błąd w Tasku ReciveAsync");
                }
            }
        }

        private async Task SendAsync(DataModel frame)
        {
            _semaphore.WaitOne();

            if (_client.Connected)
            {
                frame.LifeBit = _lifeBit;
                byte[] data = _mapper.InverseMap(frame);
                await _client.GetStream().WriteAsync(data, 0, data.Length);
            }

            _semaphore.Release();
        }

        private void CheckFrame(DataModel newframe, DataModel oldFrame)
        {
            if(IsDifferentFrame(newframe, oldFrame))
            {
                SequenceControllerCommunicationDataEventArgs eventArgs = new SequenceControllerCommunicationDataEventArgs();
                eventArgs.LBHD = newframe.LBHD;
                eventArgs.BalancingDate = DateTime.Now;

                if (newframe.Delete)
                {
                    CallDeleteOrder(eventArgs);
                }
                if(newframe.Insert)
                {
                    CallInsertOrder(eventArgs);
                }

                CallReceiveDataChanged();

                _oldReceiveData = newframe;
            }

        }
        private bool IsDifferentFrame(DataModel newframe, DataModel oldFrame)
        {
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

        private void CallDeleteOrder(SequenceControllerCommunicationDataEventArgs eventArgs)
        {
            if (DeleteOrder != null)
            {
                DeleteOrder(this, eventArgs);
            }
        }
        private void CallInsertOrder(SequenceControllerCommunicationDataEventArgs eventArgs)
        {
            if (InsertOrder != null)
            {
                InsertOrder(this, eventArgs);
            }
        }
        private void CallReceiveDataChanged()

        {
            if (ReceiveDataChanged != null)
                ReceiveDataChanged(this, EventArgs.Empty);
        }


        #region ISequenceControllerCommunicationData

        public void LBHDnotFound(string lbhd)
        {
            DataModel data = new DataModel();
            data.NotFound = true;
            data.LBHD = lbhd;

            SendAsync(data);
        }

        public void OrderDeleted(string lbhd, bool result)
        {
            DataModel data = new DataModel();
            data.Delete = true;
            data.LBHD = lbhd;

            SendAsync(data);
        }

        public void OrderInserted(string lbhd, bool result)
        {
            DataModel data = new DataModel();
            data.Insert = true;
            data.LBHD = lbhd;

            SendAsync(data);
        }

        #endregion


        #region ISequenceControllerForVisuControl

        public DataBitsFromSequenceControler SendDataBits
        {
            get
            {
                DataBitsFromSequenceControler data = new DataBitsFromSequenceControler();
                data.BalancingDate = DateTime.Now;
                data.Delete = _sendData.Delete;
                data.Insert = _sendData.Insert;
                data.LBHD = _sendData.LBHD;
                data.LifeBit = _sendData.LifeBit;
                data.Prefix = _sendData.Prefix;

                return data;
            }
        }

        public DataBitsFromSequenceControler ReceiveDataBits
        {
            get
            {
                DataBitsFromSequenceControler data = new DataBitsFromSequenceControler();
                data.BalancingDate = DateTime.Now;
                data.Delete = _receiveData.Delete;
                data.Insert = _receiveData.Insert;
                data.LBHD = _receiveData.LBHD;
                data.LifeBit = _receiveData.LifeBit;
                data.Prefix = _receiveData.Prefix;

                return data;
            }
        }

        public ErrorBitsFromSequenceControler ErrorBits
        {
            get
            {
                ErrorBitsFromSequenceControler errorBits = new ErrorBitsFromSequenceControler();
                errorBits.ConnectionAlarm = !_isConnected;
                errorBits.LBHDNotFound = _sendData.NotFound;
                errorBits.UnknownError = false;

                return errorBits;
            }
        }

        public IPAddress IP
        {
            get
            {
                return _ipAddress;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
        }

        #endregion
    }
}
