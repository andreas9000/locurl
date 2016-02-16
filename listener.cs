// Andreas Hansson 2013

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace locurl
{
    public class HServer
    {
        public Form1 ff;
        public string lastip, ip;
        TcpListener listener;

        volatile bool portchanged = false;

        public HServer(Form1 f, string ip)
        {
            this.ip = ip;
            this.ff = f;
        }

        public void listen()
        {
            while (true)
            {
                portchanged = false;
                listener = new TcpListener(IPAddress.Any, Convert.ToInt32(settingsg.port));
                listener.Start();
                while (!portchanged)
                {
                    TcpClient s = listener.AcceptTcpClient();
                    lastip = ((IPEndPoint)s.Client.RemoteEndPoint).Address.ToString();
                    //if (lastip.StartsWith("127") || lastip.StartsWith("192.168"))
                    {
                        hpr p = new hpr(s, this);
                        new Thread(new ThreadStart(p.process)).Start();
                    }
                    //else
                    {
                        //s.Close();
                        //ff.append(String.Format("{1}- Rejected external client {0}", lastip, DateTime.Now));
                    }
                }

                listener.Stop();
            }
        }
    }
}
