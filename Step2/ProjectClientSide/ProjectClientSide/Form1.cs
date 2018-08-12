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
        static string upload = "UPLD", text = "TEXT", request = "RQST", 
            delete = "DELE", rename = "RNAM" , download = "DOWN", name = "NAME", rejected = "RJEC";
        bool terminating = false;
        Thread thrReceive;
        Socket sck = null;
        string downloadFolder = @"c:\userDownload";

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
            listViewFiles.Items.Clear();
            btnDisconnect.Enabled = false;

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {   
                boxLog.Items.Add("Connecting..");
                userName = boxName.Text;
                serverIP = boxServer.Text;
                serverHost = boxHost.Text;
                Int32.TryParse(serverHost, out remoteHost);

                sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sck.Connect(IPAddress.Parse(serverIP), remoteHost);
                // sending username
                genericSend(name+userName);
                boxLog.Items.Add("Connected");
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
            if (boxFilepath.Text != "") 
            {
                String fname = txtFilename.Text;
                String path = boxFilepath.Text;
                try
                {
                    string filepath = "";
                    path = path.Replace("\\", "/");
                    while (path.IndexOf("/") > -1)
                    {
                        filepath += path.Substring(0, path.IndexOf("/") + 1);
                        path = path.Substring(path.IndexOf("/") + 1);
                    }
                    boxLog.Items.Add("Buffering...");
                    byte[] fNameByte = Encoding.ASCII.GetBytes(path);
                    byte[] fileData = File.ReadAllBytes(filepath + path);
                    byte[] clientData = new byte[4 + fNameByte.Length + fileData.Length];
                    byte[] fNameLen = BitConverter.GetBytes(fNameByte.Length);
                    genericSend(upload + clientData.Length.ToString());
                   
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
                    if (sck != null)
                    {
                        sck.Close();
                        thrReceive.Abort();
                    }
                    Environment.Exit(Environment.ExitCode);
                    break;
            }
        }

        private void btnGetFiles_Click(object sender, EventArgs e)
        {
            listViewFiles.Items.Clear();
            genericSend(request);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            String fname = labelSelected.Text;
            genericSend(delete+fname);
            listViewFiles.Items.Clear();
            genericSend(request);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            String fname = labelSelected.Text;
            genericSend(download+fname);
        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            String fname = labelSelected.Text;
            String newname = boxRename.Text;
            genericSend(rename + fname + "*" + newname);
        }

        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listViewFiles.SelectedIndices.Count <= 0)
                {
                    return;
                }
                int intselectedindex = listViewFiles.SelectedIndices[0];
                if (intselectedindex >= 0)
                {
                    labelSelected.Text = listViewFiles.Items[intselectedindex].Text;
                    boxRename.Text = listViewFiles.Items[intselectedindex].Text; 
                }
            }
            catch {
                boxLog.Items.Add("An error occurred while you select file!");
            }
          
        }

        public void genericSend(String msgOperation)
        {
            byte[] opBuffer = Encoding.Default.GetBytes(msgOperation);
            sck.Send(opBuffer);
        }

        private void Receive()
        {
            bool connected = true;
            while (connected)
            {
                try
                {
                    byte[] buffer = new byte[1028];

                    int rec = sck.Receive(buffer);

                    if (rec <= 0)
                    {
                        throw new SocketException();
                    }

                    string message = Encoding.Default.GetString(buffer);
                    string code = message.Substring(0, 4);

                    if (code == text)
                    {
                        string msgToBox = message.Substring(4, message.IndexOf("\0") - 4);
                        boxLog.Items.Add(msgToBox);
                    }
                    else if (code == request) // fill the user file list
                    {
                        string fileList = message.Substring(4, message.IndexOf("\0") - 4);
                        boxLog.Items.Add(fileList);
                        string[] tokens = fileList.Split('<'), fileDetails;
                        
                        String[] row = new String[3];

                        for (int i = 0; i < tokens.Length; i++)
                        {
                            fileDetails = tokens[i].Split('*');
                            ListViewItem lvi = new ListViewItem(fileDetails);
                            listViewFiles.Items.Add(lvi);
                        }
                    }
                    else if (code == rejected)
                    {
                        boxLog.Items.Add("Server: This user name is already being used");
                        boxLog.Items.Add("Server: Please disconnect first and try another user name");
                    }
                    else if (code == download) 
                    {
                        int fsize = Int32.Parse(message.Substring(4, message.IndexOf("\0") - 4));
                        FileReceive(fsize);
                    }

                }
                catch
                {
                    if(sck != null)
                        sck.Close();
                    if (!terminating)
                    {
                        boxLog.Items.Add("Connection has been terminated...");
                    }
                    connected = false;
                }
            }
        }

        public void FileReceive(int size)
        {
            try
            {
                byte[] buffer = new byte[size];
                sck.Receive(buffer);

                string path = System.IO.Path.Combine(downloadFolder, userName);
                System.IO.Directory.CreateDirectory(path);

                int fNameLen = BitConverter.ToInt32(buffer, 0);
                
                String fName = Encoding.ASCII.GetString(buffer, 4, fNameLen);
                BinaryWriter write = new BinaryWriter(File.Open(path + "/" + fName, FileMode.Append));
                write.Write(buffer, 4 + fNameLen, size - 4 - fNameLen);
               
                write.Close();

                boxLog.Items.Add(fName + " is downloaded:)");
            }
            catch
            {
                boxLog.Items.Add("File couldn't be downloaded:(");
            } 
        }

    }
}
