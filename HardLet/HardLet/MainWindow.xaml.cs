﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.IO.Ports;
using RestSharp;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.IO;
using System.Threading.Tasks;

namespace HardLet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// For Serial ports
        /// </summary>
        private bool connected = false;
        string comdata;
        private static SerialPort mySerialPort;
        string nodeurl = "http://3.1.202.148:3000";

        /// <summary>
        /// For 6 pin button
        /// </summary>
        private int value = 1;
        private bool oneset = false;
        private bool twoset = false;
        private bool threeset = false;
        private bool fourset = false;
        private bool fiveset = false;
        private bool sixset = false;
        public IDictionary<string,int> buttonseq = new Dictionary<string, int>()
        {
            {"1",0},
            {"2",0},
            {"3",0},
            {"4",0},
            {"5",0},
            {"6",0},
        };
        string message;

        /// <summary>
        /// Account info class for sender and receiver
        /// </summary>
        class SenderAccount
        {
            public SenderAccount()
            {
                publickey = "";
                privatekey = "";
                mosaicID = "";
            }

            private string publickey { get; set; }
            private string privatekey { get; set; }
            private string mosaicID { get; set; }

            public void setpublickey(string puk)
            {
                publickey = puk;
            }

            public string getpublickey()
            {
                return publickey;
            }

            public void setprivatekey(string pik)
            {
                privatekey = pik;
            }

            public string getprivatekey()
            {
                return privatekey;
            }

            public void setmosaicID(string mosaic)
            {
                mosaicID = mosaic;
            }

            public string getmosaicID()
            {
                return mosaicID;
            }
        };
        class ReceiverAccount
        {
            public ReceiverAccount()
            {
                address = "";
                mosaicamount = 0;
            }

            private string address { get; set; }
            private int mosaicamount { get; set; }

            public void setAddress(string add)
            {
                address = add;
            }

            public string getAddress()
            {
                return address;
            }

            public void setMosaicamount(int amount)
            {
                mosaicamount = amount;
            }

            public int getMosaicamount()
            {
                return mosaicamount;
            }
        };
        SenderAccount Sender = new SenderAccount();
        ReceiverAccount Receiver = new ReceiverAccount();

        public MainWindow()
        {
            InitializeComponent();
            COMList.ItemsSource = SerialPort.GetPortNames();
            mySerialPort = new SerialPort();
        }

        /// <summary>
        /// Serial Connection Button
        /// </summary>
        private void Connection_Click(object sender, RoutedEventArgs e)
        {
            if (COMList.SelectedItem != null)
            {
                if (connected)
                {
                    //reset button parameters
                    var bc = new BrushConverter();
                    Connection.Background = (Brush)bc.ConvertFrom("#FF00D1FF");
                    one.IsEnabled = true;
                    two.IsEnabled = true;
                    three.IsEnabled = true;
                    four.IsEnabled = true;
                    five.IsEnabled = true;
                    six.IsEnabled = true;
                    Refresh.IsEnabled = false;
                    for (int i = 1; i < buttonseq.Count + 1; i++)
                    {
                        buttonseq[i.ToString()] = 1;
                    }
                    one.Content = "";
                    two.Content = "";
                    three.Content = "";
                    four.Content = "";
                    five.Content = "";
                    six.Content = "";
                    value = 1;
                    oneset = false;
                    twoset = false;
                    threeset = false;
                    fourset = false;
                    fiveset = false;
                    sixset = false;
                    //send value to notify user and arduino close connection
                    SerialDataSend(buttonseq);
                    connected = false;
                    Connection.Content = "Connect";
                    mySerialPort.Close();
                    //MessageBox.Show("COM port disconnected", "COM port Status");

                    for (int i = 1; i < buttonseq.Count + 1; i++)
                    {
                        buttonseq[i.ToString()] = 0;
                    }

                    SenderPublicKey.Text = string.Empty;
                }
                else
                {
                    connected = true;
                    one.IsEnabled = false;
                    two.IsEnabled = false;
                    three.IsEnabled = false;
                    four.IsEnabled = false;
                    five.IsEnabled = false;
                    six.IsEnabled = false;
                    try
                    {
                        Connection.Content = "Disconnect";
                        Connection.Background = Brushes.Red;
                        for (int i = 1; i < buttonseq.Count+1; i++)
                        {
                            Console.WriteLine(buttonseq[i.ToString()]);
                        }

                        //serial connect
                        mySerialPort = new SerialPort(COMList.SelectedItem.ToString(), 115200);
                        mySerialPort.Open();
                        MessageBox.Show("COM port connected", "COM port Status");

                        //Send information
                        SerialDataSend(buttonseq);

                        //begin serial reading
                        mySerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialDataRead);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "COM port not selected");
                    }
                }
            }
            else
            {
                MessageBox.Show("COM port not selected", "COM port status");
            }
        }

        /// <summary>
        /// Event for serial reading with multithreading
        /// </summary>
        private void SerialDataRead(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (mySerialPort.IsOpen || SenderPrivateKey.Content == string.Empty || SenderPublicKey.Text == string.Empty)
                {
                    comdata = mySerialPort.ReadLine();
                    //Dispatcher.InvokeAsync((Action)(() => Console.WriteLine("Data: " + comdata)));
                    if (comdata.Substring(0, 13) == message + "private")
                    {
                        string privatekey = comdata.Substring(13, 32) + System.Environment.NewLine + comdata.Substring(comdata.Length - 33, 32);
                        Sender.setprivatekey(comdata.Substring(13, 64));
                        Dispatcher.InvokeAsync((Action)(() => SenderPrivateKey.Content = privatekey));
                    }
                    else if (comdata.Substring(0, 12) == message + "public")
                    {
                        string publickey = comdata.Substring(12, 32) + System.Environment.NewLine + comdata.Substring(comdata.Length - 33, 32);
                        Sender.setpublickey(comdata.Substring(12, 64));
                        Dispatcher.InvokeAsync((Action)(() => SenderPublicKey.Text = publickey));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Sends the pin data to the hardware wallet
        /// </summary>
        private void SerialDataSend(IDictionary<string, int> pinseq)
        {
            if (mySerialPort.IsOpen)
            {
                message = "{0}{1}{2}{3}{4}{5}";
                message = String.Format(message, pinseq["1"], pinseq["2"], pinseq["3"], pinseq["4"], pinseq["5"], pinseq["6"]);
                mySerialPort.Write(message);
            }
        }

        /// <summary>
        /// User Interface buttons for 6 pin input
        /// </summary>
        private void One_Click(object sender, RoutedEventArgs e)
        {
            if (oneset)
            {
                oneset = false;
                one.Content = "";
                buttonseq["1"] = 0;
                value--;
            }
            else
            {
                oneset = true;
                one.Content = value.ToString();
                buttonseq["1"] = value;
                value++;
            }
        }
        private void Two_Click(object sender, RoutedEventArgs e)
        {
            if (twoset)
            {
                twoset = false;
                two.Content = "";
                buttonseq["2"] = 0;
                value--;
            }
            else
            {
                twoset = true;
                two.Content = value.ToString();
                buttonseq["2"] = value;
                value++;
            }
        }
        private void Three_Click(object sender, RoutedEventArgs e)
        {
            if (threeset)
            {
                threeset = false;
                three.Content = "";
                buttonseq["3"] = 0;
                value--;
            }
            else
            {
                threeset = true;
                three.Content = value.ToString();
                buttonseq["3"] = value;
                value++;
            }
        }
        private void Four_Click(object sender, RoutedEventArgs e)
        {
            if (fourset)
            {
                fourset = false;
                four.Content = "";
                buttonseq["4"] = 0;
                value--;
            }
            else
            {
                fourset = true;
                four.Content = value.ToString();
                buttonseq["4"] = value;
                value++;
            }
        }
        private void Five_Click(object sender, RoutedEventArgs e)
        {
            if (fiveset)
            {
                fiveset = false;
                five.Content = "";
                buttonseq["5"] = 0;
                value--;
            }
            else
            {
                fiveset = true;
                five.Content = value.ToString();
                buttonseq["5"] = value;
                value++;
            }
        }
        private void Six_Click(object sender, RoutedEventArgs e)
        {
            if (sixset)
            {
                sixset = false;
                six.Content = "";
                buttonseq["6"] = 0;
                value--;
            }
            else
            {
                sixset = true;
                six.Content = value.ToString();
                buttonseq["6"] = value;
                value++;
            }
        }

        /// <summary>
        /// REST API Account Updates 
        /// </summary>
        private void SenderPublicKey_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (mySerialPort.IsOpen)
            {
                Debug.WriteLine(nodeurl + "/account/" + Sender.getpublickey(), "REST Result: ");
                string result = RESTAPIExecute(nodeurl + "/account/" + Sender.getpublickey());
                Debug.WriteLine(result, "REST Result: ");
                JObject root = JObject.Parse(result); // parse as array  
                string address = (String)root["account"]["address"];
                string mosaics = (String)root["account"]["mosaics"][0]["id"][0];
                string mosaicsamount = (String)root["account"]["mosaics"][0]["amount"][0];
                SenderAddress.Content = address;
                SenderMosaics.Content = mosaics + "," + mosaicsamount;
                Sender.setmosaicID(mosaics);
            }
            else
            {
                //Reset Label parameters
                SenderPrivateKey.Content = string.Empty;
                SenderAddress.Content = string.Empty;
                SenderMosaics.Content = string.Empty;
            }
        }
        private string RESTAPIExecute(string url)
        {
            var client = new RestClient(url);

            var response = client.Execute(new RestRequest());

            return response.Content;
        }

        /// <summary>
        /// Transaction
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ReceiverAddress.Text!=""||ReceiverMosaics.Text!="")
            {
                Receiver.setAddress(ReceiverAddress.Text);
                Receiver.setMosaicamount(int.Parse(ReceiverMosaics.Text));
                beginTransact();
            }
        }

        private void beginTransact()
        {
            // full path of nodejs interpreter 
            string nodejs = "node";

            // js app to call
            string mynodejsApp = @"..\..\transact.js"; //directory for create.js
            //string mynodejsApp = @"..\..\transfer.js"; //directory for create.js

            // dummy parameters to send javascript  
            //string texttoencrypt = Sender.getprivatekey();
            string privatekey = Sender.getprivatekey();
            string mosaicID = Sender.getmosaicID();
            int amount = Receiver.getMosaicamount();
            string address = Receiver.getAddress();

            // Create new process start info 
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(nodejs);

            // make sure we can read the output from stdout 
            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.RedirectStandardOutput = true;
            myProcessStartInfo.CreateNoWindow = true;

            // start javascript app with 1 arguments  
            // 1st arguments is pointer to itself,  
            // 2nd and 3rd are actual arguments we want to send 
            myProcessStartInfo.Arguments = mynodejsApp + " " + privatekey + " " + mosaicID + " " + amount + " " + address;
            //myProcessStartInfo.Arguments = mynodejsApp;

            Process myProcess = new Process();
            // assign start information to the process 
            myProcess.StartInfo = myProcessStartInfo;
            myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // start the process
            myProcess.Start();

            // Read the standard output of the app we called.  
            // in order to avoid deadlock we will read output first 
            // and then wait for process terminate: 
            StreamReader myStreamReader = myProcess.StandardOutput;
            string Status = myStreamReader.ReadLine();
            string hash = myStreamReader.ReadLine();
            Console.WriteLine(Status);
            Console.WriteLine(hash);

            myProcess.WaitForExit();
            myProcess.Close();
            if (Status != "Transaction Success!")
            {
                MessageBox.Show("Transaction Failed", "Transaction Status");
            }
            else
            {
                MessageBoxResult result = MessageBox.Show(Status + System.Environment.NewLine + "Hash: " + hash, "Transaction Status");
                Process.Start(nodeurl + "/transaction/" + hash + "/status");
                Refresh.IsEnabled = true;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            string result = RESTAPIExecute(nodeurl+ "/account/" + Sender.getpublickey());
            Debug.WriteLine(result, "REST Result: ");
            JObject root = JObject.Parse(result); // parse as array  
            string address = (String)root["account"]["address"];
            string mosaics = (String)root["account"]["mosaics"][0]["id"][0];
            string mosaicsamount = (String)root["account"]["mosaics"][0]["amount"][0];
            SenderAddress.Content = address;
            SenderMosaics.Content = mosaics + "," + mosaicsamount;
            Sender.setmosaicID(mosaics);
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            if (mySerialPort.IsOpen)
            {
                for (int i = 1; i < buttonseq.Count + 1; i++)
                {
                    buttonseq[i.ToString()] = 1;
                }
                SerialDataSend(buttonseq);
                mySerialPort.Close();
            }
            await ClosingTasks();
        }

        private async Task ClosingTasks()
        {
            await Task.Delay(1000);
            this.Close();
        }
    }

}
