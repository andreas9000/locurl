// Andreas Hansson 2013

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace locurl
{
    class tor
    {
        public static NetworkStream Stream(string ip, int port, TcpClient t)
        {
            string[] split = ip.Split('.');
            byte[] ipb = new byte[4];

            for (int i = 0; i < 4; i++)
                ipb[i] = byte.Parse(split[i]);

            t.Connect("localhost", 9050);

            NetworkStream n = t.GetStream();

            byte[] bout = new byte[128];
            n.Write(new byte[] { 5, 1, 0 }, 0, 3);
            n.Read(bout, 0, bout.Length);

            if (bout[0] != 5 || bout[1] != 0)
                throw new Exception();

            n.Write(new byte[] { 5, 1, 0, 1, ipb[0], ipb[1], ipb[2], ipb[3], (byte)(port / 256), (byte)(port % 256) }, 0, 10);
            n.Read(bout, 0, bout.Length);

            if (bout[0] != 5 || bout[1] != 0)
                throw new Exception();

            return n;
        }
    }
}
