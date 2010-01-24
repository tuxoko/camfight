using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Timers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Mypacket;

namespace CFServer
{
    public class listener2
    {
        private Hashtable _client_table = new Hashtable();
        private Hashtable _client_password = new Hashtable();
        //private Hashtable _client_IP_table = new Hashtable();
        //private Hashtable _client_port_table = new Hashtable();

        private TcpListener _tcpl;

        //private static Mutex mutChat = new Mutex();
        private static Mutex mutTable = new Mutex();
        //private static Mutex mutCmd = new Mutex();
        private static Mutex mutpac = new Mutex();

        public System.Timers.Timer aTimer = new System.Timers.Timer();

        private Queue<packet> packetQ = new Queue<packet>();
        private ArrayList availableusers = new ArrayList();
        private ArrayList match = new ArrayList();

        //private int ping_seq = 0;
        private string path = @"c:\MyTest.txt";

        public listener2(TcpListener tcpl)
        {
            this._tcpl = tcpl;
        }

        public void startup()
        {
            _tcpl.Start(100);
            aTimer.Elapsed += new ElapsedEventHandler(ping);
            aTimer.Interval = 5000;
            aTimer.Enabled = true;

            try
            {
                using (StreamReader sr = File.OpenText(path))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        try
                        {
                            string[] info = s.Split();
                            _client_password.Add(info[0], info[1]);
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                FileStream fs = new FileStream(path, FileMode.Create);
                fs.Close();
            }
            Thread cmdthr = new Thread(new ThreadStart(cmdThread));
            cmdthr.Start();

     
                while (true)
                {
                    
                    TcpClient clientcp = _tcpl.AcceptTcpClient();
                    NetworkStream nets = clientcp.GetStream();
                    IFormatter formatter = new BinaryFormatter();
                    packet logindata = (packet)formatter.Deserialize(nets);

                    string userpass = _client_password[logindata.Name] as string;
                    
                    mutTable.WaitOne();
                    if (_client_table.ContainsKey(logindata.Name) == true)    // username has already existed
                    {
                        mutTable.ReleaseMutex();

                        packet error = new packet("e", null, null, -1,0,0,false);
                        formatter.Serialize(nets, error);
                        clientcp.Close();
                    }
                    else if (_client_password.ContainsKey(logindata.Name) == true && userpass != logindata.Msg)
                    {
                        packet mistake = new packet("m", null, null, -1,0,0,false);
                        formatter.Serialize(nets, mistake);
                        clientcp.Close();
                    }
                    else
                    {
                        _client_table.Add(logindata.Name, clientcp);
                        availableusers.Add(logindata.Name);
                        mutTable.ReleaseMutex();
                        mutpac.WaitOne();
                        packetQ.Enqueue(logindata);
                        mutpac.ReleaseMutex();

                    }
                }
         
        }
        private void sendPacket(string user,packet senddata)
        {
            try
            {
                TcpClient client = _client_table[user] as TcpClient;
                NetworkStream nets = client.GetStream();
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(nets, senddata);
            }
            catch { }
        }
        //listen and enque commands
        public void listenthread(object obj)
        {
            mutTable.WaitOne();
            TcpClient client = _client_table[obj] as TcpClient;
            mutTable.ReleaseMutex();
            while (true)
            {
                try
                {
                    NetworkStream nets = client.GetStream();
                    IFormatter formatter = new BinaryFormatter();
                    packet receiveobj = (packet)formatter.Deserialize(nets);

                    mutpac.WaitOne();
                    packetQ.Enqueue(receiveobj);
                    mutpac.ReleaseMutex();
                }
                catch
                {
                    packet exit = new packet("q!", obj as string, null, -1, 0, 0, false);
                    mutpac.WaitOne();
                    packetQ.Enqueue(exit);
                    mutpac.ReleaseMutex();
                    Thread.CurrentThread.Abort();
                }
            }
        }

        //handle all commands
        private void cmdThread()
        {
            while (true)
            {
                mutpac.WaitOne();
                if (packetQ.Count == 0)
                {
                    mutpac.ReleaseMutex();
                    Thread.Sleep(10);
                    continue;
                }
                else
                {
                    packet data = packetQ.Dequeue();
                    mutpac.ReleaseMutex();
                    if (data.Cmd == null)
                    {
                        continue;
                    }
                    switch (data.Cmd)
                    {
                        case "l":
                            userlogin(data);
                            Thread thr = new Thread(new ParameterizedThreadStart(listenthread));
                            thr.Start(data.Name);
                            break;
                        case "q":
                            userlogout(data, true);
                            break;
                        case "q!":
                            userlogout(data, false);
                            break;
                        default:
                            defaultcmd(data);
                            break;
                    }
                }
            }
        }
        private void defaultcmd(packet data)
        {
            mutTable.WaitOne();
            try
            {
                sendPacket(data.Name,data);
                foreach (string[] ss in match)
                {
                    if (ss.Contains(data.Name))
                    {
                        string oppo = (ss[0] == data.Name) ? ss[1] : ss[0];
                        sendPacket(oppo, data);
                    }
                }
            }
            catch { }
            mutTable.ReleaseMutex();
        }

        /*private void game(packet data)
        {
            mutTable.WaitOne();
            try
            {
                Socket player = _client_table[cmd[1]] as Socket;
                string msg = cmd[0];
                for (int i = 2; i < cmd.Length; i++)
                {
                    msg += " " + cmd[i];
                }
                player.Send(Encoding.Unicode.GetBytes(msg));
            }
            catch { }
            mutTable.ReleaseMutex();
        }*/

        private void userlogin(packet data)
        {
            //string msg = string.Format("u+ {0},", cmd[1]);                //new comer's login msg

            string userpass = _client_password[data.Name] as string;
            if (_client_password.ContainsKey(data.Name) == false)
            {
                _client_password.Add(data.Name, data.Msg);
                using (StreamWriter sw = File.CreateText(path))
                {
                    foreach (DictionaryEntry de in _client_password)
                    {
                        string s = string.Format(de.Key + " " + de.Value);
                        sw.WriteLine(s);
                    }
                }

            }

            mutTable.WaitOne();
            Console.WriteLine("{0} 已登入，線上人數:{1}\n", data.Name, _client_table.Count);

            mutTable.ReleaseMutex();
        }
        private void userlogout(packet data, bool expected)
        {
            mutTable.WaitOne();
            if (_client_table.ContainsKey(data.Name))
            {
                try
                {
                    availableusers.Remove(data.Name);
                }
                catch { }

                string[] temp=null;
                foreach (string[] ss in match)
                {
                    if (ss.Contains(data.Name))
                    {
                        string oppo = (ss[0] == data.Name) ? ss[1] : ss[0];
                        packet p = new packet("q", null, null, 0, 0, 0, false);
                        sendPacket(oppo, p);
                        temp = ss;
                    }
                }
                try
                {
                    match.Remove(temp);
                }
                catch { }

                TcpClient client = _client_table[data.Name] as TcpClient;

                _client_table.Remove(data.Name);
                string msg = string.Format("{0} {1}，現在線上人數: {2} \n", data.Name, (expected ? "已離線" : "意外中止"), _client_table.Count);
                Console.WriteLine(msg);
                client.Close();
            }
            mutTable.ReleaseMutex();
        }

        private Random myrand = new Random();
        private void ping(object source, ElapsedEventArgs e)
        {
            mutTable.WaitOne();
            while ((availableusers.Count / 2) != 0)
            {
                try
                {
                    string player1 = availableusers[0] as string;
                    string player2 = availableusers[1] as string;
                    availableusers.Remove(player1);
                    availableusers.Remove(player2);

                    match.Add(new string[2] { player1, player2 });

                    Console.WriteLine("match");
                    int p1type=myrand.Next(0, 100) % 4;
                    int p2type=myrand.Next(0, 100) % 4;
                    packet p1 = new packet("match", player2, "", p1type, p2type, 0, false);
                    packet p2 = new packet("match", player1, "", p2type, p1type, 0, false);
                    sendPacket(player1, p1);
                    sendPacket(player2, p2);
                }
                catch { }
            }
            mutTable.ReleaseMutex();
        }

    }
}
