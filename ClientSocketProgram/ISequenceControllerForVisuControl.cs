using System;
using System.Net;
using System.Threading.Tasks;

namespace ClientSocketProgram
{
    public interface ISequenceControllerForVisuControl
    {
        event EventHandler SendDataChanged;
        event EventHandler ReceiveDataChanged;
        event EventHandler ErrorChanged;
        event EventHandler StartedChanged;
        DataBitsFromSequenceControler SendDataBits { get; }
        DataBitsFromSequenceControler ReceiveDataBits { get; }
        ErrorBitsFromSequenceControler ErrorBits { get; }

        Task<bool> SaveAsync(IPAddress ip, int port);
        Task<bool> SaveAsync(string ip, int port);
        Task<bool> StartAsync();
        void Stop();

        IPAddress IP
        {
            get;
        }
        int Port
        {
            get;
        }

        bool Started
        {
            get;
        }
    }
}
