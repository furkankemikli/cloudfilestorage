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



public struct files
{
    public string filename, date, size;
    //private List<String> canSeeList; // TODO:step3  

    public files(string f,string d,string s) 
    { 
        filename = f;
        date = d;
        size = s;
    }
}
public struct user
{
    public string username, userpath;
    public List<files> file;

    public user(string name, string p) {
        username = name;
        userpath = p;
        file = new List<files>();
    }


}

namespace ProjectServer
{
    
    public partial class Form1 : Form
    {
        static string upload = "UPLD", text = "TEXT", request = "RQST",
            delete = "DELE", rename = "RNAM", download = "DOWN", name = "NAME", rejected = "RJEC";
        Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
     
        int cliPort;
        string cliHost;
        private List<Socket> socketList = new List<Socket>();
        private List<String> nameList = new List<string>();
        private List<user> userList = new List<user>();
        

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
            boxName.Items.Clear();
           
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            terminating = false;
            accept = false;

            for (int i = 0; i < socketList.Count; i++)
            {
                socketList[i].Close();
            }
            while (nameList.Count > 0) 
            { 
                socketList.RemoveAt(0);
                nameList.RemoveAt(0);
            }
            sck.Close();
            btnConnect.Enabled = true;
            btnStop.Enabled = false;
            boxName.Items.Clear();
            
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            
            cliHost = boxPort.Text;
            Int32.TryParse(cliHost, out cliPort);

            try
            {
                sck.Bind( new IPEndPoint(IPAddress.Any, cliPort));
                boxLog.Items.Add("listening....");
                sck.Listen(100);
                accept = true;
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

        private void genericSend(String op, Socket s) 
        {
            byte[] opBuffer = Encoding.Default.GetBytes(op);
            s.Send(opBuffer);
        }

        private void genericReceive()
        {
            bool connected = true;

            Socket n = socketList[socketList.Count - 1];
            String nm = nameList[socketList.Count - 1];

            int index = socketList.Count - 1;
            while (connected)
            {
                try
                {
                    byte[] buffer = new byte[2048];

                    int receivedByteLen = n.Receive(buffer);
                    if (receivedByteLen <= 0)
                    {
                        throw new SocketException();
                    }

                    string message = Encoding.Default.GetString(buffer);
                    string code = message.Substring(0, 4);

                    if (code == upload)
                    {
                        int size = Int32.Parse(message.Substring(4, message.IndexOf("\0")-4));
                        boxLog.Items.Add(nm + " is  sending a file");
                        FileReceive(n , nm, size);
                    }
                    else if (code == request)
                    {
                        bool empty = false;
                        boxLog.Items.Add(nm + " wants the list of his files.");
                        string fCollection = request;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (nm == userList[i].username)
                            {
                                if (userList[i].file.Count == 0)
                                {
                                    empty = true;
                                    genericSend(text + "Server : Your file list is empty.", n);
                                }
                                else
                                {
                                    for (int k = 0; k < userList[i].file.Count; k++)
                                    {
                                        if (k == (userList[i].file.Count - 1))
                                            fCollection += userList[i].file[k].filename + "*"
                                                + userList[i].file[k].date + "*" + userList[i].file[k].size;
                                        else
                                            fCollection += userList[i].file[k].filename + "*"
                                                + userList[i].file[k].date + "*" + userList[i].file[k].size + "<";
                                    }
                                }
                                i = userList.Count;
                            }
                        }
                        if(empty == false)
                            genericSend(fCollection, n);
                    }
                    else if (code == delete)
                    {
                        string fDelete = message.Substring(4, message.IndexOf("\0") - 4);
                        boxLog.Items.Add(nm + " wants to delete " + fDelete + ".");
                        bool exist = false;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (nm == userList[i].username)
                            {
                                //TODO check empty list
                                for (int k = 0; k < userList[i].file.Count; k++)
                                {
                                    if (userList[i].file[k].filename == fDelete)
                                    {
                                        exist = true;
                                        try
                                        {
                                            string[] files = Directory.GetFiles(userList[i].userpath);
                                            for(int j=0; j< files.Length; j++)
                                            {
                                                if (files[j] == System.IO.Path.Combine(userList[i].userpath, fDelete))
                                                {
                                                    //File.SetAttributes(fDelete, FileAttributes.Normal);
                                                    File.Delete(files[j]);
                                                    userList[i].file.RemoveAt(k);
                                                    genericSend(text + "Your file is deleted", n);
                                                    boxLog.Items.Add(nm + "'s file is deleted.");
                                                    k = userList[i].file.Count;
                                                }
                                            }
                                            
                                        }
                                        catch
                                        {
                                            boxLog.Items.Add(nm + "'s file could not be deleted.");
                                            genericSend(text + "File that you requested could not be deleted.", n);
                                        }
                                    }
                                }
                                if (exist == false)
                                {
                                    genericSend(text + "File that you want to delete could not be found.", n);
                                }
                                i = userList.Count;
                            }
                        } 
                    }
                    else if (code == rename)
                    {
                        //TODO
                        files f = new files();
                        String existFile = message.Substring(4, message.IndexOf("*")-4);
                        String fRename = message.Substring(message.IndexOf("*")+1, message.IndexOf("\0") - message.IndexOf("*")-1 );
                        boxLog.Items.Add(nm + " wants to rename "+ existFile + " with " + fRename);
                        bool renm = true, exist = false;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (nm == userList[i].username)
                            {
                                for (int k = 0; k < userList[i].file.Count; k++)
                                {
                                    if (userList[i].file[k].filename == existFile)
                                    {
                                        f = userList[i].file[k];
                                        exist = true;
                     
                                        for(int j = 0; j < userList[i].file.Count;j++)
                                        {
                                            if (fRename == userList[i].file[j].filename)
                                            {
                                                j = userList[i].file.Count;
                                                renm = false;
                                                genericSend(text + "Your new name is already used.", n);
                                                k = userList[i].file.Count;
                                            }
                                        }
                                        if (renm == true)
                                        {
                                            String oldPath = System.IO.Path.Combine(serverFolder, nm, existFile);
                                            String newPath = System.IO.Path.Combine(serverFolder, nm, fRename);
                                           
                                            File.Move(oldPath, newPath);
                                            f.filename = fRename;
                                            userList[i].file[k] = f;
                                            genericSend(text + "Your file is renamed", n);
                                            boxLog.Items.Add(nm + "'s " + existFile + " file is renamed with " + fRename + ".");
                                            k = userList[i].file.Count;
                                        }

                                    }
                                  
                                }
                            }
                            i = userList.Count;
                            if (exist == false)
                            {
                                genericSend(text + "There is no file exists with this name.", n);
                            }
                        }
                    }
                    else if (code == download)
                    {
                        String fname = message.Substring(4, message.IndexOf("\0") - 4);
                        boxLog.Items.Add(nm + " wants to download "+ fname +".");

                        bool exist = false;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (nm == userList[i].username)
                            {
                                //TODO check empty list
                                for (int k = 0; k < userList[i].file.Count; k++)
                                {
                                    if (userList[i].file[k].filename == fname)
                                    {
                                        exist = true;
                                        genericSend(text + "Server: Your file " + fname + " is being sent to you.", n);
                                        FileSend(n, nm, fname);
                                        k = userList[i].file.Count;                                                
                                    }
                                }
                                if (exist == false)
                                {
                                    genericSend(text + "File that you want to download could not be found.", n);
                                }
                                i = userList.Count;
                            }
                        } 
                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        boxLog.Items.Add(nm + " has disconnected...");
                        nameList.Remove(nm);
                    }
                    n.Close();
               
                    socketList.Remove(n);
                    nameList.Remove(nm);
                    for (int m = boxName.Items.Count - 1; m >= 0; --m)
                    {
                        string removelistitem = nm;
                        if (boxName.Items[m].ToString().Contains(removelistitem))
                        {
                            boxName.Items.RemoveAt(m);
                            m = -1;
                        }
                    }                   
                    connected = false;
                }
            }  
        }

        private void GetDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                GetDirectory(dir);
            }

            Directory.Delete(target_dir, false);
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
                    String userName = Encoding.Default.GetString(name);
                    userName = userName.Substring(4, userName.IndexOf("\0") -4);
                   
                    if (nameList.Contains(userName)) {
                        nameList.Add(userName);
                        genericSend(rejected, socketList[socketList.Count - 1]);
                        
                        socketList[socketList.Count - 1].Close();
                        socketList.RemoveAt(socketList.Count - 1);
                    }
                    else
                    {
                        string upath = System.IO.Path.Combine(serverFolder, userName);
                        System.IO.Directory.CreateDirectory(upath);

                        string str = userName + " connected to the server";
                        user newUser = new user(userName, upath);

                        System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(upath);
                       // Get the root directory and print out some information about it.
                        //System.IO.DirectoryInfo dirInfo = di.RootDirectory;
 
                        // Get the files in the directory and print out some information about them.
                        System.IO.FileInfo[] fileNames = di.GetFiles("*.*");

                        foreach (System.IO.FileInfo fi in fileNames)
                        {
                            files f = new files (fi.Name, fi.LastAccessTime.ToString(), fi.Length.ToString());
                            newUser.file.Add(f);
                        }
                        userList.Add(newUser);

                        boxLog.Items.Add(str);
                        nameList.Add(userName);
                        boxName.Items.Add(userName);
                      
                        thrReceive = new Thread(new ThreadStart(genericReceive));
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

        private void FileSend(Socket n, String nm, String fname) 
        {
            string path = System.IO.Path.Combine(serverFolder, nm);
            path = System.IO.Path.Combine(path, fname);
            try
            {
                string filepath = "";
                path = path.Replace("\\", "/");
                while (path.IndexOf("/") > -1)
                {
                    filepath += path.Substring(0, path.IndexOf("/") + 1);
                    path = path.Substring(path.IndexOf("/") + 1);
                }
                boxLog.Items.Add("Buffering " + fname);
                byte[] fNameByte = Encoding.ASCII.GetBytes(path);
                byte[] fileData = File.ReadAllBytes(filepath + path);
                byte[] clientData = new byte[4 + fNameByte.Length + fileData.Length];
                byte[] fNameLen = BitConverter.GetBytes(fNameByte.Length);
                
                genericSend(download + clientData.Length.ToString(), n);

                fNameLen.CopyTo(clientData, 0);
                fNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fNameByte.Length);

                boxLog.Items.Add(fname + " is being sent to " + nm );
                n.Send(clientData);
                boxLog.Items.Add("Heyy " + fname + " is sent to " + nm + " :)");
            }
            catch
            {
                boxLog.Items.Add("Opps! Item cannot be sent, check your connection.");
            }        
        }

        private void FileReceive(Socket n, String nm, int size)
        {
            try
            {
                byte[] buffer = new byte[size];
                n.Receive(buffer);
                
                string path = System.IO.Path.Combine(serverFolder, nm);
                

                int fNameLen = BitConverter.ToInt32(buffer, 0);
                String fName = Encoding.ASCII.GetString(buffer, 4, fNameLen);
                // if there exist a file with same name
                String extension = fName.Substring(fName.IndexOf("."), fName.Length - fName.IndexOf("."));//extension
                String tempName = fName.Substring(0, fName.IndexOf("."));//fname without extension
                int count = 1;
                for (int i = 0; i < userList.Count; i++)
                {
                    if (nm == userList[i].username)
                    {
                        //check filename exists or not
                        for (int k = 0; k < userList[i].file.Count; k++)
                        {
                            if (userList[i].file[k].filename == fName)
                            {
                                fName = tempName + "(" + count.ToString() + ")" + extension;
                                k = -1;
                                count++;
                            }
                        }
                    }
                }
                
                BinaryWriter write = new BinaryWriter(File.Open(path + "/" + fName, FileMode.Append));
                write.Write(buffer, 4 + fNameLen, size - 4 - fNameLen);
                write.Close();
                
                DateTime d = DateTime.Now;
                int s = size - 4 - fNameLen;
                string fsize;
                if(s > 1000000)
                {
                    float fls = s / (float)1000000;
                    fsize = Convert.ToString(fls) + " MB";
                    string.Format("{0:N2}", fsize);
                }
                else if (s > 1000)
                {
                    float fls = s / (float)1000;
                    fsize = Convert.ToString(fls) + " KB";
                    string.Format("{0:N2}", fsize);
                }
                else {
                    fsize = Convert.ToString(s) + " KB";
                }

                files newFile = new files(fName, d.ToString(), fsize);
               
                for (int i = 0; i < userList.Count; i++) 
                {
                    if (nm == userList[i].username)
                    {
                        userList[i].file.Add(newFile);
                        i = userList.Count;
                    }
                }
                boxLog.Items.Add("Heyy " + nm + "'s file is received:)");
                 
            }
            catch 
            {
                boxLog.Items.Add(nm + "'s file couldn't received:(");
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
                        thrReceive.Abort();
                    }
                    
                    Environment.Exit(Environment.ExitCode);
                    break;
            }
        }
        
    }
}
