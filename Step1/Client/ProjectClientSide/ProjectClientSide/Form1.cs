using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Reflection;

// Eda Deniz Caner 17915, Furkan Kemikli 17846, Ufuk Akgeyik 17865

namespace ProjectClientSide
{
    public partial class Form1 : Form
    {
        bool terminating = false;
        Thread thrReceive;
        Socket sck;

        string userName, serverIP, serverHost;
        int remoteHost;
        OpenFileDialog openFileDialog = new OpenFileDialog();


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            boxLog.Items.Clear();
            btnDisconnect.Enabled = false;

            
           // sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        public void SendFile(string path, string filename)
        {
            try
            {   
                string filepath="";
                path = path.Replace("\\", "/");
                while (path.IndexOf("/") > -1)
                {
                    filepath += path.Substring(0, path.IndexOf("/") + 1);
                    path = path.Substring(path.IndexOf("/") + 1);
                }
                byte[] fNameByte = Encoding.ASCII.GetBytes(path);
                boxLog.Items.Add("Buffering...");
                byte[] fileData = File.ReadAllBytes(filepath+path);
                byte[] clientData = new byte[4 + fNameByte.Length + fileData.Length];                
                byte[] fNameLen = BitConverter.GetBytes(fNameByte.Length);
                fNameLen.CopyTo(clientData, 0);
                fNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fNameByte.Length);
                boxLog.Items.Add("File is sending..");
                sck.Send(clientData);
                boxLog.Items.Add("File is sent:)");
            }
            catch 
            {
                boxLog.Items.Add("Opps! Item cannot be sent, check your connection.");
            }
        }
        private void sendUsername()
        {
            byte[] usernameBuffer = Encoding.Default.GetBytes(userName);
            sck.Send(usernameBuffer);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                userName = boxName.Text;
                serverIP = boxServer.Text;
                serverHost = boxHost.Text;
                Int32.TryParse(serverHost, out remoteHost);

                sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sck.Connect(IPAddress.Parse(serverIP), remoteHost);
                sendUsername();
                boxLog.Items.Add("Connecting..");
                thrReceive = new Thread(new ThreadStart(Receive));
                thrReceive.Start();
                

                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            }
            catch 
            {
                boxLog.Items.Add("Cannot connected to the specified server");
                boxLog.Items.Add("terminating");
            }
        }


        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            boxLog.Items.Add("Disconnecting..");
            
            sck.Close();
            thrReceive.Abort();

            btnDisconnect.Enabled = false;
            btnConnect.Enabled = true;
        }

        

        private void btnOpenfile_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
            txtFilename.Text = (Path.GetFileName(openFileDialog.FileName));
            boxFilepath.Text = openFileDialog.FileName;
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            if (boxFilepath.Text != "") {
                SendFile(boxFilepath.Text, txtFilename.Text);
            }
        }

        private void Receive()
        {
            bool connected = true;
            while (connected)
            {
                try
                {
                    byte[] buffer = new byte[64]; 
                   
                    int rec = sck.Receive(buffer);
                   
                    if (rec <= 0)
                    {
                        throw new SocketException();
                    }
                   
                    string newmessage = Encoding.Default.GetString(buffer);
                    newmessage = newmessage.Substring(0, newmessage.IndexOf("\0"));
                    if (newmessage.Equals("rejected"))
                    {
                        boxLog.Items.Add("Server: this user name is already being used" );
                        boxLog.Items.Add("Server: please disconnect first and try another user name");
                        
                        //btnDisconnect_Click(btnDisconnect, EventArgs.Empty);
                    }
                }
                catch
                {
                    sck.Close();
                    if (!terminating)
                    {
                        boxLog.Items.Add("Connection has been terminated...");
                    }
                    connected = false;
                }

            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            // Confirm user wants to close
            switch (MessageBox.Show(this, "Are you sure you want to close Client program?", "Closing", MessageBoxButtons.YesNo))
            {
                case DialogResult.No:
                    e.Cancel = true;
                    break;
                default:

                    sck.Close();
                    Environment.Exit(Environment.ExitCode);
                    break;
            }
        }
    }
}
