using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;

namespace locurl
{
    class IP
    {
        static internal string fetch()
        {
#if DEBUG
            System.Windows.Forms.MessageBox.Show("IP");
#endif

            using (WebClient c = new WebClient())
            {
                string i = string.Empty;

                try { i = c.DownloadString("http://what-is-my-ip.net/?text"); }
                    catch (Exception) { }

                return i;
            }
        }
    }
}
