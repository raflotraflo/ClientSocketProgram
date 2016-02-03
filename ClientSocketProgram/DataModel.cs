using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class DataModel
    {
        public const int NumberOfBytes = 128;

        public bool LifeBit { get; set;}
        public bool Insert { get; set; }
        public bool Delete { get; set; }
        public bool NotFound { get; set; }

        #region Buildstring
        public string Prefix { get; set; }
        public string LBHD { get; set; }
        public string Sequence { get; set; }
        public string Broadcast { get; set; }
        public string PLPID { get; set; }
        public string Operation { get; set; }
        #endregion // Buildstring

        public DataModel()
        {
            Prefix = "";
            LBHD = "";
            Sequence = "";
            Broadcast = "";
            PLPID = "";
            Operation = "";
        }
    }
}
