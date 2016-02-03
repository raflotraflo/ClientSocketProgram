using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;

namespace ClientSocketProgram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient clientSocket = new TcpClient();
        IMapper<byte[], DataModel> _mapper = new DataMapper();
        DataModel _model = new DataModel();

        public MainWindow()
        {
            InitializeComponent();

            msg("Client Started");
            clientSocket.Connect("127.0.0.1", 8888);
            //clientSocket.Connect("192.168.20.167", 11000);
            //clientSocket.ConnectAsync()
            label1.Content = "Client Socket Program - Server Connected ...";
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            List<string> fields = new List<string>();
            fields.Add(textBox2.Text);
            _model.LBHD = textBox2.Text;
            _model.Insert = true;
            _model.NotFound = true;
            //_model.LifeBit = true;
            _model.Broadcast = "bla bla bla bla" + "$";
            CheckElipse();

            NetworkStream serverStream = clientSocket.GetStream();
            //byte[] outStream = Encoding.ASCII.GetBytes(textBox2.Text + "$");
            byte[] outStream = _mapper.InverseMap(_model);
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();

            byte[] inStream = new byte[10025];
            int v = clientSocket.ReceiveBufferSize;

            
            serverStream.Read(inStream, 0, clientSocket.ReceiveBufferSize);
            _model = _mapper.Map(inStream);
            CheckElipse();
            inStream = inStream.Where(x => x != 0).ToArray();

            string returndata = Encoding.ASCII.GetString(inStream);
            msg(returndata);
            textBox2.Text = "";
            textBox2.Focus();
        }

        public void msg(string mesg)
        {
            textBox1.Text = textBox1.Text + Environment.NewLine + " >> " + mesg;
        }

        private void CheckElipse()
        {
            if(_model.LifeBit)
            {
                controlEllipse.Fill = new SolidColorBrush(Colors.Green);
            }
            else
            {
                controlEllipse.Fill = new SolidColorBrush(Colors.Red);
            }
        }


    }
}
