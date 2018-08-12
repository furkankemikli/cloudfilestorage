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
    public List<String> canSeeList;

    public files(string f, string d, string s)
    {
        filename = f;
        date = d;
        size = s;
        canSeeList = new List<String>();//dosyayı kimler görebiliyor
    }
}

public struct shared_files
{
    public string filename, username, date, size;

    public shared_files(string f, string n, string d, string s)
    {
        filename = f;
        username = n;
        date = d;
        size = s;
    }
}

public struct user
{
    public string username, userpath;
    public List<files> file;
    public List<shared_files> shared;

    public user(string name, string p)
    {
        username = name;
        userpath = p;
        file = new List<files>();
        shared = new List<shared_files>();
    }


}

namespace ProjectServer
{

    public partial class Form1 : Form
    {
        static string upload = "UPLD", text = "TEXT", request = "RQST", shareDownload = "SDWN", unshare = "UNSH", userRequest = "URQT",
            delete = "DELE", rename = "RNAM", download = "DOWN", name = "NAME", rejected = "RJEC", share = "SHAR";
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
            GetBeginningInfo();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            boxLog.Items.Clear();
            boxName.Items.Clear();
            TextBox.CheckForIllegalCrossThreadCalls = false;
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
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sck.Bind(new IPEndPoint(IPAddress.Any, cliPort));
                boxLog.Items.Add("listening....");
                sck.Listen(100);
                accept = true;
                thrAccept = new Thread(new ThreadStart(Accept));
                thrAccept.Start();
                //Thread.Sleep(100);
                btnStop.Enabled = true;
                btnConnect.Enabled = false;
                listening = true;
            }
            catch
            {
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

                    if (code == upload)//done
                    {
                        int size = Int32.Parse(message.Substring(4, message.IndexOf("\0")-4));
                        boxLog.Items.Add(nm + " is  sending a file");
                        FileReceive(n , nm, size);
                    }
                    else if (code == request)//done
                    {
                        bool empty = false;
                        boxLog.Items.Add(nm + " wants the list of his files.");
                        string fCollection = request;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (nm == userList[i].username)
                            {
                                if (userList[i].file.Count == 0 && userList[i].shared.Count == 0)
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
                                                + userList[i].file[k].date + "*" + userList[i].file[k].size + "*" + "-";
                                        else
                                            fCollection += userList[i].file[k].filename + "*"
                                                + userList[i].file[k].date + "*" + userList[i].file[k].size + "*" + "-" +  "<";
                                    }
                                    for (int j = 0; j < userList[i].shared.Count; j++)
                                    {
                                        if (j == 0 && userList[i].file.Count != 0)
                                        {
                                            fCollection += "<" + userList[i].shared[j].filename + "*" + userList[i].shared[j].date + 
                                                "*" + userList[i].shared[j].size + "*" + userList[i].shared[j].username + "<";
                                        }
                                        else if (j == (userList[i].shared.Count - 1))
                                        {
                                            fCollection += userList[i].shared[j].filename + "*" + userList[i].shared[j].date +
                                                "*" + userList[i].shared[j].size + "*" + userList[i].shared[j].username;
                                        }
                                        else 
                                        {
                                            fCollection += userList[i].shared[j].filename + "*" + userList[i].shared[j].date +
                                                "*" + userList[i].shared[j].size + "*" + userList[i].shared[j].username + "<";
                                        }                                    
                                    }
                                }
                                i = userList.Count;
                            }
                        }
                        if (empty == false)
                        {
                            genericSend(fCollection, n);
                        }
                    }
                    else if (code == userRequest)//
                    {
                        boxLog.Items.Add(nm + " wants to user list.");
                        string userCollection = "";
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (userList[i].username != nm)
                            {
                                if (i == userList.Count - 1)
                                {
                                    userCollection += userList[i].username;
                                }
                                else
                                {
                                    userCollection += userList[i].username + "<";
                                }
                            }
                        }
                        if (userCollection != "")
                        {
                            genericSend(userRequest + userCollection, n);
                            genericSend(text + "Server: User list is sent to you.", n);
                        }
                        else
                        {
                            genericSend(text + "Server: User list is empty.", n);
                        }
                    }
                    else if (code == unshare)//
                    {
                        
                        boxLog.Items.Add(nm + " wants to unshare his file.");
                        String fname = message.Substring(4, message.IndexOf("*") - 4);
                        String uname = message.Substring(message.IndexOf("*") + 1, message.IndexOf("\0") - 5 - fname.Length);
                        bool exist = false, shared = false;
                        shared_files dummy = new shared_files(fname,nm,null,null);
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (uname == userList[i].username)
                            {
                                for (int j = 0; j < userList.Count; j++)
                                {
                                    if (userList[j].username == nm)
                                    {                                       
                                        for (int k = 0; k < userList[j].file.Count; k++)
                                        {
                                            if (userList[j].file[k].filename == fname)
                                            {
                                                exist = true;
                                                userList[j].file[k].canSeeList.Remove(uname);
                                                dummy.date = userList[j].file[k].date;
                                                dummy.size = userList[j].file[k].size;
                                                if (userList[i].shared.Remove(dummy))
                                                {
                                                    shared = true;                                                    
                                                    String sContent = "";
                                                    sContent += userList[i].username;
                                                    //sContent.Add(userList[i].username);
                                                    // File.WriteAllText(Path.Combine(serverFolder, sContent[0]+".txt"));
                                                    //public shared_files(string f, string n, string d, string s)
                                                    for (int w = 0; w < userList[i].shared.Count(); w++)
                                                    {
                                                        sContent += ("\n" + userList[i].shared[w].filename);
                                                        sContent += ("\n" + userList[i].shared[w].username);
                                                        sContent += ("\n" + userList[i].shared[w].date);
                                                        sContent += ("\n" + userList[i].shared[w].size);
                                                    }
                                                    File.WriteAllText(Path.Combine(serverFolder, userList[i].username + ".txt"), sContent);
                                                    boxLog.Items.Add(nm + " unshared " + fname + " with " + uname);
                                                    genericSend(text + fname+" is unshared with " + uname, n);
                                                    //TODO:
                                                    // kiminle share edildiyse ona da mesaj gönder ve onun listesini güncelle, txtye yaz
                                                    for (int u = 0; u < nameList.Count; u++)
                                                    {
                                                        if (nameList[u] == uname)
                                                        {
                                                            genericSend(text + nm + " unshared " + fname + " with you." , socketList[u]);
                                                            u = nameList.Count;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (exist == false)
                        {
                            boxLog.Items.Add(nm + "'s file could not be found to unshare.");
                            genericSend(text + "File that you want to unshare could not be found.", n);
                        }
                        else if (shared == false)
                        {
                            boxLog.Items.Add(nm + " couldn't be unshared " + fname + " with " + uname);
                            genericSend(text + "File that you want to unshare haven't been shared.", n);
                        }
                    }
                    else if (code == delete)//
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
                                            for (int j = 0; j < files.Length; j++)
                                            {
                                                if (files[j] == System.IO.Path.Combine(userList[i].userpath, fDelete))
                                                {
                                                    //File.SetAttributes(fDelete, FileAttributes.Normal);
                                                    File.Delete(files[j]);
                                                    for (int l = 0; l < userList[i].file[j].canSeeList.Count; l++)
                                                    {
                                                        string date = userList[i].file[j].date;
                                                        string size = userList[i].file[j].size;
                                                        string usr_name = userList[i].file[j].canSeeList[l];
                                                        for (int h = 0; h < userList.Count; h++)//kiminle shared edildiyse onun listesinden sil
                                                        {
                                                            if (userList[h].username == usr_name)
                                                            {
                                                                shared_files temp = new shared_files(fDelete, nm, date, size);
                                                                userList[h].shared.Remove(temp);
                                                                // nameList,socketList sıraları aynı
                                                                String sContent = "";
                                                                sContent += userList[h].username;
                                                                //sContent.Add(userList[i].username);
                                                                // File.WriteAllText(Path.Combine(serverFolder, sContent[0]+".txt"));
                                                                //public shared_files(string f, string n, string d, string s)
                                                                for (int w = 0; w < userList[h].shared.Count(); w++)
                                                                {
                                                                    sContent += ("\n" + userList[h].shared[w].filename);
                                                                    sContent += ("\n" + userList[h].shared[w].username);
                                                                    sContent += ("\n" + userList[h].shared[w].date);
                                                                    sContent += ("\n" + userList[h].shared[w].size);
                                                                }
                                                                File.WriteAllText(Path.Combine(serverFolder, userList[h].username + ".txt"), sContent);
                                                            }
                                                        }
                                                        for (int f = 0; f < nameList.Count; f++)//mesaj göndermek için socket buluyor
                                                        {
                                                            if (nameList[f] == usr_name)
                                                            {
                                                                genericSend(text + nm + " deleted shared file " + fDelete, socketList[f]);
                                                            }
                                                        }
                                                    }
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
                                    boxLog.Items.Add(nm + "'s file could not be deleted.");
                                    genericSend(text + "File that you want to delete could not be found.", n);
                                }
                                i = userList.Count;
                            }
                        }
                    }
                    else if (code == rename)//
                    {
                        //TODO
                        files f = new files();
                        String existFile = message.Substring(4, message.IndexOf("*") - 4);
                        String fRename = message.Substring(message.IndexOf("*") + 1, message.IndexOf("\0") - message.IndexOf("*") - 1);
                        boxLog.Items.Add(nm + " wants to rename " + existFile + " with " + fRename);
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

                                        for (int j = 0; j < userList[i].file.Count; j++)
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
                                            for (int l = 0; l < userList[i].file[k].canSeeList.Count; l++)
                                            {
                                                string date = userList[i].file[k].date;
                                                string size = userList[i].file[k].size;
                                                string usr_name = userList[i].file[k].canSeeList[l];
                                                for (int h = 0; h < userList.Count; h++)//kiminle shared edildiyse onun listesinden sil
                                                {
                                                    if (userList[h].username == usr_name)
                                                    {
                                                        for (int m = 0; m < userList[h].shared.Count; m++)
                                                        {
                                                            if (userList[h].shared[m].filename == existFile)
                                                            {
                                                                shared_files temp = new shared_files(fRename, nm, date, size);
                                                                userList[h].shared[m] = temp;
                                                                String sContent = "";
                                                                sContent += userList[h].username;
                                                                //sContent.Add(userList[i].username);
                                                                // File.WriteAllText(Path.Combine(serverFolder, sContent[0]+".txt"));
                                                                //public shared_files(string f, string n, string d, string s)
                                                                for (int w = 0; w < userList[h].shared.Count(); w++)
                                                                {
                                                                    sContent += ("\n" + userList[h].shared[w].filename);
                                                                    sContent += ("\n" + userList[h].shared[w].username);
                                                                    sContent += ("\n" + userList[h].shared[w].date);
                                                                    sContent += ("\n" + userList[h].shared[w].size);
                                                                }
                                                                File.WriteAllText(Path.Combine(serverFolder, userList[h].username + ".txt"), sContent);
                                                            }
                                                        }
                                                        // nameList,socketList sıraları aynı
                                                    }
                                                }
                                                for (int g = 0; g < nameList.Count; g++)//mesaj göndermek için socket buluyor
                                                {
                                                    if (nameList[g] == usr_name)
                                                    {
                                                        genericSend(text + nm + " renamed shared file " + existFile, socketList[g]);
                                                    }
                                                }
                                            }
                                            boxLog.Items.Add(nm + "'s " + existFile + " file is renamed with " + fRename + ".");
                                            k = userList[i].file.Count;
                                        }

                                    }
                                }
                                if (exist == false)
                                {
                                    genericSend(text + "There is no file exists with this name.", n);
                                }
                                i = userList.Count;
                            }
                        }
                    }
                    else if (code == share)//
                    {
                        boxLog.Items.Add(nm + " wants to share his file.");
                        String fname = message.Substring(4, message.IndexOf("*") - 4);
                        String uname = message.Substring(message.IndexOf("*") + 1, message.IndexOf("\0") - 5 - fname.Length);
                        bool exist = false;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (uname == userList[i].username)
                            {
                                shared_files temp = new shared_files(fname, nm, null, null);
                                for (int j = 0; j < userList.Count; j++)
                                {
                                    if (userList[j].username == nm)
                                    {
                                        for (int k = 0; k < userList[j].file.Count; k++)
                                        {
                                            if (userList[j].file[k].filename == fname)
                                            {
                                                exist = true;
                                                userList[j].file[k].canSeeList.Add(uname);
                                                temp.date = userList[j].file[k].date;
                                                temp.size = userList[j].file[k].size;
                                                if (!userList[i].shared.Contains(temp))
                                                {
                                                    userList[i].shared.Add(temp);
                                                    String sContent = "";
                                                    sContent += userList[i].username;
                                                    //sContent.Add(userList[i].username);
                                                    // File.WriteAllText(Path.Combine(serverFolder, sContent[0]+".txt"));
                                                    //public shared_files(string f, string n, string d, string s)
                                                    for (int w = 0; w < userList[i].shared.Count(); w++)
                                                    {
                                                        sContent += ("\n" + userList[i].shared[w].filename);
                                                        sContent += ("\n" + userList[i].shared[w].username);
                                                        sContent += ("\n" + userList[i].shared[w].date);
                                                        sContent += ("\n" + userList[i].shared[w].size);
                                                    }
                                                    File.WriteAllText(Path.Combine(serverFolder, userList[i].username + ".txt"), sContent);

                                                    boxLog.Items.Add(nm + " shared " + fname + " with " + uname);
                                                    genericSend(text + fname + " is shared with " + uname, n);

                                                    //TODO:
                                                    // kiminle share edildiyse ona da mesaj gönder ve onun listesini güncelle, txtye yaz
                                                    for (int u = 0; u < nameList.Count; u++)
                                                    {
                                                        if (nameList[u] == uname)
                                                        {
                                                            genericSend(text + nm + " shared " + fname + " with you.", socketList[u]);
                                                            u = nameList.Count;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    genericSend(text + fname + " is already shared with " + uname, n);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (exist == false)
                        {
                            boxLog.Items.Add(nm + "'s file couldn't be shared with " + uname);
                            genericSend(text + "File that you want to share could not be found.", n);
                        }
                    }
                    else if (code == download)//
                    {
                        String fname = message.Substring(4, message.IndexOf("\0") - 4);
                        boxLog.Items.Add(nm + " wants to download " + fname + ".");

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
                                        //genericSend(text + "Server: Your file " + fname + " is being sent to you.", n);
                                        FileSend(n, nm, fname, nm);
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
                    else if (code == shareDownload)//
                    {
                        String fname = message.Substring(4, message.IndexOf("*") - 4);
                        boxLog.Items.Add(nm + " wants to download a shared file " + fname + ".");
                        int test1 = message.IndexOf("*");
                        int test2 = message.IndexOf("\0") - 5 - fname.Length;
                        string sharedBy = message.Substring(message.IndexOf("*") + 1, message.IndexOf("\0") - 5 - fname.Length);

                        bool exist = false;
                        for (int i = 0; i < userList.Count; i++)
                        {
                            if (sharedBy == userList[i].username)
                            {
                                //TODO check empty list
                                for (int k = 0; k < userList[i].file.Count; k++)
                                {
                                    if (userList[i].file[k].filename == fname)
                                    {
                                        for (int j = 0; j < userList[i].file[k].canSeeList.Count; j++)
                                        {
                                            if(userList[i].file[k].canSeeList[j] == nm)
                                            {
                                                exist = true;
                                                //genericSend(text + "Server: " + sharedBy + "'s file " + fname + " is being sent to you.", n);
                                                FileSend(n, nm, fname, sharedBy);
                                            }
                                        }
                                        k = userList[i].file.Count;
                                    }
                                }
                                if (exist == false)
                                {
                                    genericSend(text + "File that you want to download could not be sent.", n);
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

        private void GetBeginningInfo()
        {

            if (Directory.Exists(serverFolder) && Directory.EnumerateFiles(serverFolder).Any())
            {
                foreach (string dirName in Directory.GetDirectories(serverFolder))//servera daha önceden girmişlerin bilgilerini alıyor.
                {

                    String uname = dirName.Substring(dirName.LastIndexOf("\\") + 1, dirName.Length - dirName.LastIndexOf("\\") - 1);
                    user newUser = new user(uname, dirName);
                    System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(dirName);
                    // Get the root directory and print out some information about it.
                    // Get the files in the directory and print out some information about them.
                    System.IO.FileInfo[] fileNames = di.GetFiles("*.*");

                    foreach (System.IO.FileInfo fi in fileNames)//kişinin kendi dosyalarını ekliyor.
                    {
                        long s = fi.Length;
                        string fsize;
                        if (s > 1000000)
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
                        else
                        {
                            fsize = Convert.ToString(s) + " KB";
                        }
                        files f = new files(fi.Name, fi.LastAccessTime.ToString(), fsize);
                        newUser.file.Add(f);
                    }
                    userList.Add(newUser);
                }
                foreach (string serFi in Directory.EnumerateFiles(serverFolder, "*.txt"))//her kişinin shared txt dosyasına ulaşıyor.
                {
                    String[] contents = File.ReadAllLines(serFi);
                    String name = contents[0];
                    int userNum = 0;
                    for (int j = 0; j < userList.Count; j++)
                    {
                        if (userList[j].username == name)
                        {
                            userNum = j;
                            j = userList.Count;
                        }
                    }
                    for (int i = 1; i < contents.Length; i += 4)//Her satırda bir info var bir dosya infosu 4 ayrı line'da
                    {  //o kişiyle paylaşılan dosyaları onun bilgilerine ekliyor.
                        //public shared_files(string f, string n, string d, string s)
                        String filename = contents[i], fOwner = contents[i + 1], fDate = contents[i + 2], fSize = contents[i + 3];
                        shared_files dummyFile = new shared_files(filename, fOwner, fDate, fSize);
                        userList[userNum].shared.Add(dummyFile);
                        for (int j = 0; j < userList.Count; j++)
                        {
                            if (userList[j].username == fOwner)
                            {
                                for (int k = 0; k < userList[j].file.Count; k++)
                                {
                                    if (userList[j].file[k].filename == filename)
                                    {
                                        userList[j].file[k].canSeeList.Add(name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool userListContains(string name)
        {
            for (int i = 0; i < userList.Count; i++)
            {
                if (userList[i].username.ToLower() == name.ToLower())
                {
                    user temp = userList[i];//if lowercase-uppercase change in name so update it ex: ali - ALI
                    temp.username = name;
                    userList[i] = temp;
                    return true;
                }
            }
            return false;
        }

        private void Accept()
        {
            while (accept)
            {
                try
                {
                    bool user_exist = false;
                    socketList.Add(sck.Accept());
                    byte[] name = new byte[64];
                    socketList[socketList.Count - 1].Receive(name);
                    String userName = Encoding.Default.GetString(name);
                    userName = userName.Substring(4, userName.IndexOf("\0") - 4).ToLower();

                    foreach (string ex in nameList)
                    {
                        if (ex.ToLower() == userName.ToLower())
                            user_exist = true;
                    }

                    if (user_exist == true)
                    {
                        //nameList.Add(userName);
                        genericSend(rejected, socketList[socketList.Count - 1]);
                        boxLog.Items.Add("A user wants to login using name of another user.");
                        socketList[socketList.Count - 1].Close();
                        socketList.RemoveAt(socketList.Count - 1);
                        user_exist = false;
                    }
                    else
                    {
                        if (!userListContains(userName))//daha önce hiç girmediyse
                        {
                            string upath = System.IO.Path.Combine(serverFolder, userName);
                            System.IO.Directory.CreateDirectory(upath);

                            user newUser = new user(userName, upath);

                            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(upath);
                            // Get the root directory and print out some information about it.
                            //System.IO.DirectoryInfo dirInfo = di.RootDirectory;

                            // Get the files in the directory and print out some information about them.
                            System.IO.FileInfo[] fileNames = di.GetFiles("*.*");

                            foreach (System.IO.FileInfo fi in fileNames)
                            {
                                long s = fi.Length;
                                string fsize;
                                if (s > 1000000)
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
                                else
                                {
                                    fsize = Convert.ToString(s) + " KB";
                                }

                                files f = new files(fi.Name, fi.LastAccessTime.ToString(), fsize);

                                newUser.file.Add(f);
                            }
                            userList.Add(newUser);
                        }
                        string str = userName + " connected to the server";
                        boxLog.Items.Add(str);
                        nameList.Add(userName);
                        boxName.Items.Add(userName);
                        genericSend(text + "Server: You are connected.", socketList[socketList.Count - 1]);

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

        private void FileSend(Socket n, String nm, String fname, String shareWho)
        {
            string path;
            if (nm != shareWho)
            {
                path = System.IO.Path.Combine(serverFolder, shareWho);
            }
            else
            {
                path = System.IO.Path.Combine(serverFolder, nm);
            }
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

                boxLog.Items.Add(fname + " is being sent to " + nm);
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
                if (s > 1000000)
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
                else
                {
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
                genericSend(text + "Server: Your file " + fName + " is received.", n);
            }
            catch
            {
                boxLog.Items.Add(nm + "'s file couldn't received:(");
                genericSend(text + "Server: Your file couldn't not received.", n);
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
