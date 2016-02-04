using System;
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

        void LBHDnotFound(string lbhd);
        void OrderInserted(string lbhd, bool result);
        void OrderDeleted(string lbhd, bool result);
    }
}
