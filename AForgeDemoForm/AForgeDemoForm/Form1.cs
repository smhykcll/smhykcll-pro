using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using AForge.Video;
using ZXing;
using System.Threading;

namespace AForgeDemo
{
    public partial class AForgeDemoForm : Form
    {
        private struct Device
        {
            public int Index;
            public string Name;
            public override string ToString()
            {
                return Name;
            }
        }
      
        private readonly CameraDevices camDevices;
        private Bitmap currentBitmapForDecoding;
        private readonly Thread decodingThread;
        private Result currentResult;
        private readonly Pen resultRectPen;

        int i;
        TcpClient client; // Creates a TCP Client
        NetworkStream stream; //Creats a NetworkStream (used for sending and receiving data)
        byte[] datalength = new byte[4]; // creates a new byte with length 4 ( used for receivng data's lenght)
        public AForgeDemoForm()
        {
            InitializeComponent();
           
            camDevices = new CameraDevices();

            decodingThread = new Thread(DecodeBarcode);
            decodingThread.Start();
           
            pictureBox1.Paint += pictureBox1_Paint;
            resultRectPen = new Pen(Color.Green, 10);
        }
        public void ClientReceive()
        {

            stream = client.GetStream(); //Gets The Stream of The Connection
            new Thread(() => // Thread (like Timer)
            {
                while ((i = stream.Read(datalength, 0, 4)) != 0)//Keeps Trying to Receive the Size of the Message or Data
                {
                    // how to make a byte E.X byte[] examlpe = new byte[the size of the byte here] , i used BitConverter.ToInt32(datalength,0) cuz i received the length of the data in byte called datalength :D
                    byte[] data = new byte[BitConverter.ToInt32(datalength, 0)]; // Creates a Byte for the data to be Received On
                    stream.Read(data, 0, data.Length); //Receives The Real Data not the Size
                    this.Invoke((MethodInvoker)delegate // To Write the Received data
                    {
                        txtLog.Text += System.Environment.NewLine + "Server : " + Encoding.Default.GetString(data); // Encoding.Default.GetString(data); Converts Bytes Received to String
                    });
                }
            }).Start(); // Start the Thread
        }
        public void ClientSend(string msg)

        {
            
            stream = client.GetStream(); //Gets The Stream of The Connection
            byte[] data; // creates a new byte without mentioning the size of it cuz its a byte used for sending
            data = Encoding.Default.GetBytes(msg); // put the msg in the byte ( it automaticly uses the size of the msg )
            int length = data.Length; // Gets the length of the byte data
            byte[] datalength = new byte[4]; // Creates a new byte with length of 4
            datalength = BitConverter.GetBytes(length); //put the length in a byte to send it
            stream.Write(datalength, 0, 4); // sends the data's length
            stream.Write(data, 0, data.Length); //Sends the real data
        }
        void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

        }
       

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadDevicesToCombobox();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel)
            {
                decodingThread.Abort();
                if (camDevices.Current != null)
                {
                    camDevices.Current.NewFrame -= Current_NewFrame;
                    if (camDevices.Current.IsRunning)
                    {
                        camDevices.Current.SignalToStop();
                    }
                }
            }
        }

        private void LoadDevicesToCombobox()
        {
            cmbDevice.Items.Clear();
            for (var index = 0; index < camDevices.Devices.Count; index++)
            {
                cmbDevice.Items.Add(new Device { Index = index, Name = camDevices.Devices[index].Name });
                
            }
            
        }

        private void cmbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (camDevices.Current != null)
            {
                camDevices.Current.NewFrame -= Current_NewFrame;
                if (camDevices.Current.IsRunning)
                {
                    camDevices.Current.SignalToStop();
                }
            }

            camDevices.SelectCamera(((Device)(cmbDevice.SelectedItem)).Index);
            camDevices.Current.NewFrame += Current_NewFrame;
            camDevices.Current.Start();
            int selectedIndex = cmbDevice.SelectedIndex;
           // textBox1.Text = selectedIndex.ToString();
        }

        private void Current_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                if (currentBitmapForDecoding == null)
                {
                    currentBitmapForDecoding = (Bitmap)eventArgs.Frame.Clone();
                }
                Invoke(new Action<Bitmap>(ShowFrame), eventArgs.Frame.Clone());
            }
            catch (ObjectDisposedException)
            {
                // not sure, why....
            }
        }

        private void ShowFrame(Bitmap frame)
        {
            if (pictureBox1.Width < frame.Width)
            {
                pictureBox1.Width = frame.Width;
            }
            if (pictureBox1.Height < frame.Height)
            {
                pictureBox1.Height = frame.Height;
            }
            pictureBox1.Image = frame;
        }
        public void yazdir()
        {
            label5.Text = txtContent.Text + comboBox1.SelectedItem.ToString();
        }
        private void DecodeBarcode()
        {
            var reader = new BarcodeReader();
            while (true)
            {
                if (currentBitmapForDecoding != null)
                {
                    var result = reader.DecodeMultiple(currentBitmapForDecoding);
                    if (result != null)
                    {
                        Invoke(new Action<Result>(ShowResult), result);
                    }
                    currentBitmapForDecoding.Dispose();
                    currentBitmapForDecoding = null;
                    
                }
                
                Thread.Sleep(200);
               
                
            }
        }
       
        public void ShowResult(Result result)
        {
            currentResult = result;
            txtBarcodeFormat.Text = result.BarcodeFormat.ToString();
            txtContent.Text = result.Text;

        
        }

        private void button1_Click(object sender, EventArgs e)
        {
   
            
            camDevices.SelectCamera(((Device)(cmbDevice.SelectedItem)).Index);
            camDevices.Current.NewFrame += Current_NewFrame;
            camDevices.Current.Start();
            int selectedIndex = cmbDevice.SelectedIndex+1;
          
            
            
           
        }

      

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
       
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                client = new TcpClient("127.0.0.1", 1980); //Trys to Connect
                ClientReceive(); //Starts Receiving When Connected
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); // Error handler :D
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (client.Connected) // if the client is connected
            {
                string gonder;
                gonder = "UserIsAt:"+txtContent.Text +","+ comboBox1.SelectedItem.ToString();
                ClientSend(gonder); // uses the Function ClientSend and the msg as txtSend.Text
            }
        }

        private void txtContent_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

       
        

       
       
        
       
    }
}