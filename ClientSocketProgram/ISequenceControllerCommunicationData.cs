﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public interface ISequenceControllerCommunicationData
    {
        event EventHandler<SequenceControllerCommunicationDataEventArgs> InsertOrder;
        event EventHandler<SequenceControllerCommunicationDataEventArgs> DeleteOrder;

        Task LBHDnotFoundAsync(string lbhd);
        Task OrderWithSOPAsync(string lbhd);
        Task OrderInsertedAsync(string lbhd);
        Task OrderNotInsertedAsync(string lbhd);
        Task OrderDeletedAsync(string lbhd);
        Task OrderNotDeletedAsync(string lbhd);
        Task UnknownErrorAsync();
    }
}
