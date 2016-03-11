using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class CommunicationDataFrame
    {
        public const int NumberOfBytes = 110;

        public bool LifeBit { get; set; }
        public bool Insert { get; set; }
        public bool Delete { get; set; }
        public bool NotFound { get; set; }
        public bool SOAP { get; set; }
        public bool NotInsert { get; set; }
        public bool NotDelete { get; set; }
        public bool UnknownError { get; set; }

        #region Buildstring
        public string Prefix { get; set; }
        public string LBHD { get; set; }
        public string Sequence { get; set; }
        public string Broadcast { get; set; }
        public string PLPID { get; set; }
        public string Operation { get; set; }
        #endregion // Buildstring

        public CommunicationDataFrame()
        {
            //zmiana 
            Prefix = "";
            LBHD = "";
            Sequence = "";
            Broadcast = "";
            PLPID = "";
            Operation = "";
            
            //zmiana2
        }

        public CommunicationDataFrame GetCopy()
        {
            CommunicationDataFrame copy = new CommunicationDataFrame();

            copy.LifeBit = LifeBit;
            copy.Insert = Insert;
            copy.Delete = Delete;
            copy.NotFound = NotFound;
            copy.Prefix = Prefix;
            copy.LBHD = LBHD;
            copy.Sequence = Sequence;
            copy.Broadcast = Broadcast;
            copy.PLPID = PLPID;
            copy.Operation = Operation;

            return copy;
        }
    }
}
