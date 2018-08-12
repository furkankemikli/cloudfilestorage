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
            delete = "DELE", rename = "RNAM" , download = "DOWN", name = "NAME", rejected = "RJEC", 
            share = "SHAR", unshare = "UNSH", userRequest = "URQT", shareDownload = "SDWN"; // STEP3
        bool terminating = false;
        Thread thrReceive = null;
        Socket sck = null;
        Socket udpSck = null;
       // string downloadFolder = @"c:\userDownload";
        string pathToDownload =  @"c:\Downloads";

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
            userList.Items.Clear();
            btnDisconnect.Enabled = false;
            btnDelete.Enabled = false;
            btnDownload.Enabled = false;
            shareBtn.Enabled = false;
            unshareBtn.Enabled = false;
            userListBtn.Enabled = false;
            btnGetFiles.Enabled = false;
            btnRename.Enabled = false;
            refreshUserBtn.Enabled = false;
            btnUpload.Enabled = false;
            btnOpenfile.Enabled = false;
              
            TextBox.CheckForIllegalCrossThreadCalls = false;
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
                //boxLog.Items.Add("Connected");
                thrReceive = new Thread(new ThreadStart(Receive));
                thrReceive.Start();
                
                btnConnect.Enabled =    false;
                btnDisconnect.Enabled = true;
                btnDisconnect.Enabled = true;
                btnDelete.Enabled =     true;
                btnDownload.Enabled =   true;
                shareBtn.Enabled =      true;
                unshareBtn.Enabled =    true;
                userListBtn.Enabled =   true;
                btnGetFiles.Enabled =   true;
                btnRename.Enabled =     true;
                refreshUserBtn.Enabled =true;
                btnUpload.Enabled =     true;
                btnOpenfile.Enabled =   true;
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
          //  thrReceive.Abort();

            btnDisconnect.Enabled = false;
            btnConnect.Enabled = true;
            btnDelete.Enabled = false;
            btnDownload.Enabled = false;
            shareBtn.Enabled = false;
            unshareBtn.Enabled = false;
            userListBtn.Enabled = false;
            btnGetFiles.Enabled = false;
            btnRename.Enabled = false;
            refreshUserBtn.Enabled = false;
            btnUpload.Enabled = false;
            btnOpenfile.Enabled = false;
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
                   
                }
                catch
                {
                    boxLog.Items.Add("Opps! Item cannot be sent, check your connection.");
                }

            }
        }

        private void btnGetFiles_Click(object sender, EventArgs e)
        {
            listViewFiles.Items.Clear();
            genericSend(request);
        }

        private void btnDelete_Click(object sender, EventArgs e)//done
        {
            String fname = labelSelected.Text;
            genericSend(delete+fname);
            listViewFiles.Items.Clear();
            genericSend(request);
        }

        private void btnDownload_Click(object sender, EventArgs e)//done
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
               pathToDownload = fbd.SelectedPath;
            }

            String fname = labelSelected.Text;
            try
            {    
                if (listViewFiles.SelectedIndices.Count <= 0)
                {
                    return;
                }
                ListViewItem item = listViewFiles.SelectedItems[0];

                if (listViewFiles.SelectedIndices.Count >= 0)
                {
                   if( item.SubItems[3].Text == "-")
                    {
                        genericSend(download + fname);
                    }
                    else
                    {
                        string uname = item.SubItems[3].Text;
                        genericSend(shareDownload + fname + "*" + uname);
                    }
                }
            }
            catch 
            {
                boxLog.Items.Add("An error occurred while you select file!");
            }

        }

        private void btnRename_Click(object sender, EventArgs e)//done
        {
            String fname = labelSelected.Text;
            String newname = boxRename.Text;
            genericSend(rename + fname + "*" + newname);
            listViewFiles.Items.Clear();
            genericSend(request);
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
                    labelSelectedFile.Text = listViewFiles.Items[intselectedindex].Text; //STEP3
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
            bool welcome = true;
            while (connected)
            {
                try
                {
                    byte[] buffer = new byte[2048];

                    if (welcome == true)
                    {
                        welcome = false;
                        genericSend(text + userName + " is connected.");
                    }

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
                        string[] tokens = fileList.Split('<'), fileDetails;
                        
                        for (int i = 0; i < tokens.Length; i++)
                        {
                            fileDetails = tokens[i].Split('*');
                            ListViewItem lvi = new ListViewItem(fileDetails);
                            listViewFiles.Items.Add(lvi);
                        }                        
                    }
                    else if (code == userRequest)
                    {
                        string uList = message.Substring(4, message.IndexOf("\0") - 4);
                        string[] tokens = uList.Split('<');

                        for (int i = 0; i < tokens.Length; i++)
                        {
                            userList.Items.Add(tokens[i]);
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
                    MethodInfo methodOnClick = typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    methodOnClick.Invoke(btnDisconnect, new Object[] { EventArgs.Empty });
                }
            }
        }

        public void FileReceive(int size)
        {
            try
            {
                byte[] buffer = new byte[size];
                sck.Receive(buffer);
                            
               //string path = System.IO.Path.Combine(downloadFolder, userName);
                System.IO.Directory.CreateDirectory(pathToDownload);


                int fNameLen = BitConverter.ToInt32(buffer, 0);
                

                String fName = Encoding.ASCII.GetString(buffer, 4, fNameLen);
                BinaryWriter write = new BinaryWriter(File.Open(pathToDownload + "/" + fName, FileMode.Append));
                write.Write(buffer, 4 + fNameLen, size - 4 - fNameLen );
               
                write.Close();

                boxLog.Items.Add(fName + " is downloaded:)");
            }
            catch
            {
                boxLog.Items.Add("File couldn't be downloaded:(");
            } 
        }

        private void userList_SelectedIndexChanged(object sender, EventArgs e) //STEP3
        {
            try
            {
                if (userList.SelectedIndices.Count <= 0)
                {
                    return;
                }
                else
                {
                    labelSelectedUser.Text = userList.GetItemText(userList.SelectedItem);
                }
            }
            catch
            {
                boxLog.Items.Add("An error occurred while you select user!");
            }
        }

        private void userListBtn_Click(object sender, EventArgs e) //STEP3
        {
            userList.Items.Clear();
            genericSend(userRequest);
        }

        private void refreshUserBtn_Click(object sender, EventArgs e) //STEP3
        {
            userList.Items.Clear();
            genericSend(userRequest);
        }

        private void shareBtn_Click(object sender, EventArgs e) //STEP3
        {
            String fileName = labelSelectedFile.Text;
            String userName = labelSelectedUser.Text;
            genericSend(share + fileName + "*" + userName);
        }

        private void unshareBtn_Click(object sender, EventArgs e) //STEP3
        {
            String fileName = labelSelectedFile.Text;
            String userName = labelSelectedUser.Text;
            genericSend(unshare + fileName + "*" + userName);
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
                        MethodInfo methodOnClick = typeof(Button).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        methodOnClick.Invoke(btnDisconnect, new Object[] { EventArgs.Empty });
                    }
                    Environment.Exit(Environment.ExitCode);
                    //if (thrReceive != null)
                    //    thrReceive.Abort();
                    break;
            }
        }
    }
}
