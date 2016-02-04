using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class ErrorBitsFromSequenceControler
    {
        public bool LBHDNotFound { get; set; }
        public bool ConnectionAlarm { get; set; }
        public bool UnknownError { get; set; }
    }
}
