using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

// Eda Deniz Caner 17915, Furkan Kemikli 17846, Ufuk Akgeyik 17865

namespace ProjectServer
{
    public partial class Form1 : Form
    {

        Socket sck;
     
        int cliPort;
        string cliHost;
        private List<Socket> socketList = new List<Socket>();
        private List<String> nameList = new List<string>();
        Thread thrAccept, thrReceive;
        bool terminating = false, listening = false, accept = true;
        string serverFolder = @"c:\server";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            boxLog.Items.Clear();
           
        }

        private void btnStop_Click(object sender, EventArgs e)
        {

            terminating = false;

            accept = true;
            sck.Close();
            btnConnect.Enabled = true;
            btnStop.Enabled = false;
            thrAccept.Abort();
            //thrReceive.Abort();
            
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            
            cliHost = boxPort.Text;
            Int32.TryParse(cliHost, out cliPort);

            try
            {
                sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sck.Bind( new IPEndPoint(IPAddress.Any, cliPort));
                boxLog.Items.Add("listening....");
                sck.Listen(100);
                
                thrAccept = new Thread(new ThreadStart(Accept));
                thrAccept.Start();

                btnStop.Enabled = true;
                btnConnect.Enabled = false;
                listening = true;
            }
            catch {
                boxLog.Items.Add("Cannot create a server with the specified port number\n Check the port number and try again.");
            }
            
        }

        private void Accept()
        {
            while (accept)
            {
                try
                {
                    socketList.Add(sck.Accept());
                    byte[] name = new byte[64];
                    socketList[socketList.Count - 1].Receive(name);
                    string userName = Encoding.Default.GetString(name);
                    userName = userName.Substring(0, userName.IndexOf("\0"));
                   
                    if (nameList.Contains(userName)) {
                        nameList.Add(userName);
                       
                        String message = "rejected";
                        byte[] rejectMessage = Encoding.Default.GetBytes(message);
                       
                        socketList[socketList.Count-1].Send(rejectMessage);

                        socketList[socketList.Count - 1].Close();
                    }
                    else
                    {
                        nameList.Add(userName);
                        boxLog.Items.Add(userName + " connected to the server");
                        thrReceive = new Thread(new ThreadStart(Receive));
                        thrReceive.Start();
                    }
                    
                }
                catch (Exception ex)
                {
                    if (terminating)
                        accept = false;
                    else
                    {
                        boxLog.Items.Add("Listening socket has stopped working...");
                    }
                }
            }
        }

        private void Receive()
        {
            bool connected = true;
           
            Socket n = socketList[socketList.Count - 1];
            String nm = nameList[socketList.Count - 1];

            int index = socketList.Count - 1;
            while (connected)
            {
                try
                {
                    byte[] buffer =  new byte[2048*50000];
                  
                    int receivedByteLen = n.Receive(buffer);
                   
                    if (receivedByteLen <= 0)
                    {

                        throw new SocketException();
                    }
                    boxLog.Items.Add(nm + " is  sending a file");
                    string path = System.IO.Path.Combine(serverFolder, nm);
                    System.IO.Directory.CreateDirectory(path);

                    int fNameLen = BitConverter.ToInt32(buffer, 0);
                    string fName = Encoding.ASCII.GetString(buffer, 4, fNameLen);
                    BinaryWriter write = new BinaryWriter(File.Open(path + "/" + fName, FileMode.Append));
                    write.Write(buffer, 4 + fNameLen, receivedByteLen - 4 - fNameLen);
                    write.Close();
                    boxLog.Items.Add("Heyy " + nm + "'s file is received:)");
                }
                catch
                {
                    if (!terminating)
                    {     
                        boxLog.Items.Add(nm + " has disconnected...");
                        nameList.Remove(nm);
                        //thrReceive.Abort();

                    }
                    n.Close();
                    socketList.Remove(n);
                    connected = false;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            // Confirm user wants to close
            switch (MessageBox.Show(this, "Are you sure you want to close server program?", "Closing", MessageBoxButtons.YesNo))
            {
                case DialogResult.No:
                    e.Cancel = true;
                    break;
                default:
                    sck.Close();
                    for (int i = 0; i < socketList.Count; i++)
                    {
                        socketList[socketList.Count - 1].Close();
                    }
                    
                    Environment.Exit(Environment.ExitCode);
                    break;
            }
        }
        
    }
}
