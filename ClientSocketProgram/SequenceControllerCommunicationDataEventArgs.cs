﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public class SequenceControllerCommunicationDataEventArgs : EventArgs
    {
        public string LBHD { get; set; }
        public DateTime BalancingDate { get; set; }

    }
}
