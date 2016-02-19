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
using System.Windows.Threading;

namespace ClientSocketProgram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region old
        //TcpClient clientSocket = new TcpClient();
        //IMapper<byte[], DataModel> _mapper = new DataMapper();
        //DataModel _model = new DataModel();
        #endregion

        ISequenceControllerCommunicationData controller;
        ISequenceControllerForVisuControl visu;
        int i = 0;
        bool getMessage;

        public MainWindow()
        {
            InitializeComponent();

            SequenceControllerCommunication a = new SequenceControllerCommunication("127.1", 4311);
            //a.Connect("127.0.0.1", 4321);
            //a.Connect("192.168.20.203", 4070);
            
            // a.Start();
            //a.OrderDeleted("nrlbhd", true);
            controller = a;
            visu = a;

            textBox2.Text = "";

            controller.InsertOrder += Controller_InsertOrder;
            controller.DeleteOrder += Controller_DeleteOrder;
            visu.ReceiveDataChanged += Visu_ReceiveDataChanged;
            visu.ErrorChanged += Visu_ErrorChanged;

            Task.Delay(100);

            //visu.SaveAsync("127.0.0.1", 4322).ConfigureAwait(false);
            visu.SaveAsync("121.1", 4311).ConfigureAwait(false);
            //visu.SaveAsync("192.168.20.203", 4070).ConfigureAwait(false);
            visu.StartAsync();

        }

        private void Visu_ErrorChanged(object sender, EventArgs e)
        {
            bool a = visu.ErrorBits.ConnectionAlarm;
            Color color = Colors.Green;

            if (a)
                color = Colors.Red;


            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                     new Action(() => controlEllipse3.Fill = new SolidColorBrush(color)));
        }

        private void Visu_ReceiveDataChanged(object sender, EventArgs e)
        {
            bool a = visu.ReceiveDataBits.LifeBit;
            getMessage = !getMessage;

            Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                   new Action(() => CheckElipse(a, getMessage)));
            
        }

        private void Controller_DeleteOrder(object sender, SequenceControllerCommunicationDataEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
               DispatcherPriority.Background,
                new Action(() => textBox2.Text += "\nDelete: " + e.LBHD));    
        }


        private void Controller_InsertOrder(object sender, SequenceControllerCommunicationDataEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
               DispatcherPriority.Background,
                new Action(() => textBox2.Text += "\nInsert: " + e.LBHD));
        }

        public Task Initialization { get; private set; }
        private void button1_Click(object sender, RoutedEventArgs e)
        {

            Initialization = controller.OrderDeletedAsync("nrlbhd");
            i++;

            #region old

            //List<string> fields = new List<string>();
            //fields.Add(textBox2.Text);
            //_model.LBHD = textBox2.Text;
            //_model.Insert = true;
            //_model.NotFound = true;
            ////_model.LifeBit = true;
            //_model.Broadcast = "bla bla bla bla" + "$";
            //CheckElipse();

            //NetworkStream serverStream = clientSocket.GetStream();
            ////byte[] outStream = Encoding.ASCII.GetBytes(textBox2.Text + "$");
            //byte[] outStream = _mapper.InverseMap(_model);
            //serverStream.Write(outStream, 0, outStream.Length);
            //serverStream.Flush();

            //byte[] inStream = new byte[10025];
            //int v = clientSocket.ReceiveBufferSize;


            //serverStream.Read(inStream, 0, clientSocket.ReceiveBufferSize);
            //_model = _mapper.Map(inStream);
            //CheckElipse(_model.LifeBit);
            //inStream = inStream.Where(x => x != 0).ToArray();

            //string returndata = Encoding.ASCII.GetString(inStream);
            //msg(returndata);
            //textBox2.Text = "";
            //textBox2.Focus();

            #endregion
        }

        public void msg(string mesg)
        {
            textBox1.Text = textBox1.Text + Environment.NewLine + " >> " + mesg;
        }

        private void CheckElipse(bool lifeBit, bool lifeBit1)
        {
            if (lifeBit)
            {
                controlEllipse.Fill = new SolidColorBrush(Colors.Green);
            }
            else
            {
                controlEllipse.Fill = new SolidColorBrush(Colors.Red);
            }

            if (lifeBit1)
            {
                controlEllipse1.Fill = new SolidColorBrush(Colors.Green);
            }
            else
            {
                controlEllipse1.Fill = new SolidColorBrush(Colors.Red);
            }
        }

        private void buttonPrzerwij_Click(object sender, RoutedEventArgs e)
        {
            if(controller is ISequenceControllerForVisuControl)
            {
                ((ISequenceControllerForVisuControl)controller).Stop();
            }
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            if (controller is ISequenceControllerForVisuControl)
            {
                ((ISequenceControllerForVisuControl)controller).StartAsync();
            }
        }
    }
}
