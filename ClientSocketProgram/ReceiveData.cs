using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class ReceiveData
    {
        public const int NumberOfBytes = 128;

        public bool LifeBit
        {
            get
            {
                return _lifeBit;
            }
        }
        public bool Insert
        {
            get
            {
                return _insert;
            }
        }
        public bool Delete
        {
            get
            {
                return _delete;
            }
        }
        public bool NotFound
        {
            get
            {
                return _notFound;
            }
        }


        #region Buildstring
        public string Prefix
        {
            get { return _prefix; }
        }
        public string LBHD
        {
            get { return _lBHD; }
        }
        public string Sequence
        {
            get { return _sequence; }
        }
        public string Broadcast
        {
            get { return _broadcast; }
        }
        public string PLPID
        {
            get { return _pLPID; }
        }
        public string Operation
        {
            get { return _operation; }
        }
        #endregion // Buildstring

        private bool _lifeBit;
        private bool _insert;
        private bool _delete;
        private bool _notFound;

        private string _prefix;
        private string _lBHD;
        private string _sequence;
        private string _broadcast;
        private string _pLPID;
        private string _operation;



        public ReceiveData()
        {
            _prefix = "";
            _lBHD = "";
            _sequence = "";
            _broadcast = "";
            _pLPID = "";
            _operation = "";
        }

        internal ReceiveData(byte[] data)
        {
            _lifeBit = data[0].GetBit(0);
            _prefix = GetString(data, 2, 4);
            _lBHD = GetString(data, 6, 12);
            _broadcast = GetString(data, 18, 60);
            _sequence = GetString(data, 78, 10);
            _pLPID = GetString(data, 88, 2);
            _operation = GetString(data, 90, 2);
        }

        private string GetString(byte[] input, int startindex, int num_bytes)
        {
            return ASCIIEncoding.ASCII.GetString(input, startindex, num_bytes).Replace("\0", " ");
        }
    }
}
