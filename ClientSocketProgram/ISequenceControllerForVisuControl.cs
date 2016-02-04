using System;
using System.Net;

namespace ClientSocketProgram
{
    public interface ISequenceControllerForVisuControl
    {
        event EventHandler SendDataChanged;
        event EventHandler ReceiveDataChanged;
        event EventHandler ErrorChanged;
        DataBitsFromSequenceControler SendDataBits { get; }
        DataBitsFromSequenceControler ReceiveDataBits { get; }
        ErrorBitsFromSequenceControler ErrorBits { get; }

        IPAddress IP
        {
            get;
        }
        int Port
        {
            get;
        }

    }
}
