// Andreas Hansson 2013

using System;
using System.Collections.Generic;
using System.IO;

namespace locurl
{
    class settings
    {
        public static bool read(string file, out int[] s)
        {
            s = new int[4];
            if (!File.Exists(file))
                return false;
            using (StreamReader r = new StreamReader(file))
            {
                if (r.ReadLine() != "[locurl]")
                    return false;
                for (int i = 0; i < 3; i++)
                    s[i] = Int32.Parse(r.ReadLine().Split('=')[1]);
            }
            return true;
        }

        public static void write(int port = 8080, int index = 1, string greeting = "File index:", int lan = 0)
        {
            string[] w = { "[locurl]", "port=" + port.ToString(), "index=" +
                             index.ToString(), "greeting=" + greeting, "lan_only=" + lan.ToString() };
            using (StreamWriter s = new StreamWriter("config.ini"))
            {
                foreach (string i in w)
                    s.WriteLine(i);
            }
        }
    }
}
