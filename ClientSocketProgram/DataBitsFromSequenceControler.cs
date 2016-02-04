using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class DataBitsFromSequenceControler
    {
        public bool LifeBit { get; set; }
        public bool Insert { get; set; }
        public bool Delete { get; set; }
        public string Prefix { get; set; }
        public string LBHD { get; set; }
        public DateTime BalancingDate { get; set; }
    }
}
