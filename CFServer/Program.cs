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

namespace CFServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress serverip = IPAddress.Parse("140.112.18.202");
            int port = 800;
            IPEndPoint serverhost = new IPEndPoint(serverip, port);
            TcpListener tcpl = new TcpListener(serverhost);
            Console.WriteLine("Server started at: " + serverhost.Address.ToString() + " port:" + port);

            listener2 lc = new listener2(tcpl);
            lc.startup();
        }
    }
}
