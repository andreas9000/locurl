using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace locurl
{
    public partial class Form1 : Form
    {
        static public string ip = IP.fetch(), inip;

        int displayExt = 1;
        int[] sets = { 8080, 1, 0 };
        StreamWriter wr = new StreamWriter("log.txt", true);

        public Form1(string[] a)
        {
            InitializeComponent();
            settingsg.loadfile();
            listBox1.AllowDrop = true;
            listBox1.DragDrop += new DragEventHandler(ddrop);
            listBox1.DragEnter += new DragEventHandler(dent);

            label3.AllowDrop = true;
            label3.DragDrop += new DragEventHandler(ddrop);
            label3.DragEnter += new DragEventHandler(dent);

            this.Closing += new CancelEventHandler(Form1_Closing);

            inip = ca();
            textBox3.Text += String.Format("Your internal IP is {0}{2}Your external IP is {1}{2}", inip, ip, Environment.NewLine);

            if (a.Length == 1)
            {
                short p = 0;
                if (Int16.TryParse(a[0], out p))
                    settingsg.port = a[0];
            }
            textBox2.Text = String.Format("http://{0}{1}", ip, settingsg.port == "80" ? string.Empty : ":" + settingsg.port);
#if DEBUG
            System.Windows.Forms.MessageBox.Show("Server start");
#endif
            HServer httpServer = new HServer(this, ip);
            Thread t = new Thread(new ThreadStart(httpServer.listen));
            t.Start();
            //ca();

            //addFileForce(@"C:\Users\\Desktop\classtyle.css");
        }
        


        public static Dictionary<string, int> white = new Dictionary<string, int>();


        public static List<string> urls = new List<string>();

        public static string lastToken;
        
        volatile static public bool running = false;

        public static void genToken()
        {
            lastToken = string.Empty;
            Random r = new Random();
            for (int i = 0; i < 10; i++)
                lastToken += (char)r.Next(0x30, 0x39);

        }
        public static string rnd()
        {
            string l = string.Empty;
            Random r = new Random();
            for (int i = 0; i < 10; i++)
                l += (char)r.Next(0x30, 0x39);
            return l;
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && listBox1.SelectedIndex != -1)
            {
                //index.Remove(listBox1.SelectedItem.ToString());
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);

                switch (displayExt)
                {
                    case 1:
                        textBox2.Text = ip;
                        break;
                    case 2:
                        textBox2.Text = inip;
                        break;
                    case 3:
                        textBox2.Text = "http://xetal.ddns.net";
                        break;
                }

                //button2.Enabled = false;
            }
        }
        private void dent(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }
        void addFileForce(string i)
        {
            string[] q = i.Split('\\');
            listBox1.Items.Add(q[q.Length - 1]);
            topFiles.files.Add(q[q.Length - 1], i);
            topFiles.rootfiles.Add(q[q.Length - 1], i);
        }
        void ddrop(object e, DragEventArgs d)
        {
            string[] f = (string[])d.Data.GetData(DataFormats.FileDrop);
            foreach (string i in f)
            {
                if (topFiles.crossCheck.Contains(i) || topFiles.files.ContainsValue(i))
                    continue;
                if (Directory.Exists(i))
                {
                    string[] q = i.Split('\\');
                    //if (q[q.Length - 1] == "ink")
                        //continue;
                    topFiles.folders.Add(q[q.Length - 1], 
                        new Folder2(i, i.Length - q[q.Length - 1].Length, true, ref listBox1));
                    topFiles.crossCheck.Add(i);
                }
                else if (File.Exists(i))
                {
                    string[] q = i.Split('\\');
                    if (q[q.Length - 1] == "favicon.ico")
                        continue;
                    listBox1.Items.Add(q[q.Length - 1]);
                    topFiles.files.Add(q[q.Length - 1], i);
                    topFiles.rootfiles.Add(q[q.Length - 1], i);
                }
            }
            if (f.Length == 1)
            {
                string[] q = f[0].Split('\\');

                string sip = "";

                switch (displayExt)
                {
                    case 1:
                        sip = ip;
                        break;
                    case 2:
                        sip = inip;
                        break;
                    case 3:
                        sip = "octavius.ddns.net";
                        break;
                }

                textBox2.Text = String.Format("http://{0}{1}/{2}", sip, settingsg.port == "80"|| displayExt == 3 ? string.Empty : ":" + settingsg.port,
                    Uri.EscapeUriString(q[q.Length - 1]));
            }
            label3.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox2.Text);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string sip = "";

            switch (displayExt)
            {
                case 1:
                    sip = ip;
                    break;
                case 2:
                    sip = inip;
                    break;
                case 3:
                    sip = "octavius.ddns.net";
                    break;
            }

            if (listBox1.SelectedIndex != -1)
                textBox2.Text = String.Format("http://{0}{1}/{2}", sip, settingsg.port == "80" || displayExt == 3 ? string.Empty : ":" + settingsg.port,
                    Uri.EscapeUriString(listBox1.Items[listBox1.SelectedIndex].ToString()));
        }

        public void append(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(append), new object[] { value });
                return;
            }
            if (!value.StartsWith("-log-"))
            {
                textBox3.Text += value + Environment.NewLine;
                textBox3.SelectionStart = textBox3.Text.Length;
                textBox3.ScrollToCaret();
            }
            if (settingsg.log)
            {
                wr.WriteLine(value);
            }
        }
        public void appendn(int value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<int>(appendn), new object[] { value });
                return;
            }
            switch (value)
            {
                case 0:
                    ups++;
                    break;
                case 1:
                    ups--;
                    break;
                case 2:
                    dls++;
                    break;
                case 3:
                    dls--;
                    break;
            }
        }

        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (action > 0)
            {
                if (DialogResult.Yes == MessageBox.Show(this, "There are still ongoing connections, are you sure you want to stop the server and exit?", "Locurl", MessageBoxButtons.YesNo))
                    Environment.Exit(0);
                else
                    e.Cancel = true;
            }
            else Environment.Exit(0);
        }
        static public volatile int action = 0;
        public static Dictionary<string, string> content()
        {
#if DEBUG
            System.Windows.Forms.MessageBox.Show("MIME");
#endif
            Dictionary<string, string> r = new Dictionary<string, string>();
            if (File.Exists("headers"))
                using (StreamReader s = new StreamReader("headers"))
                {
                    char[] sep = { ' ', '	' };
                    int cc = 0;
                    while (!s.EndOfStream)
                    {
                        string[] i = s.ReadLine().Split(sep);
                        if (i.Length == 3)
                            if (!r.ContainsKey(i[2]))
                            r.Add(i[2], i[0]);
                        cc++;
                    }
                }
            return r;
        }
        public static string ca()
        {
            NetworkInterface[] ifs = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface i in ifs)
            {
                if (i.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                var unic = i.GetIPProperties().UnicastAddresses;
                foreach (var u in unic)
                {
                    string b = string.Empty;
                    b = u.Address.ToString();
                    int c;
                    if (int.TryParse(u.Address.ToString().Split('.')[0], out c))
                        return u.Address.ToString();
                }
            }
            return "localhost";
        }
        public static string getLast(string i, char c)
        {
            int p = i.LastIndexOf(c);
            return i.Substring(p + 1, i.Length - p - 1);
        }

        public static Dictionary<string, string> files2;
        public static Dictionary<string, Folder2> folders2;

        private void button1_Click(object sender, EventArgs e)
        {
            Form f = new settingsg(ref button3);
            f.Show();
        }
        int dls2 = 0;
        public int dls
        {
            get { return dls2; }
            set
            {
                dls2 = value;
                //label1.Text = "Downloads in progrss: " + value.ToString();
            }
        }
        int ups2 = 0;
        public int ups
        {
            get { return ups2; }
            set
            {
                ups2 = value;
                //label2.Text = "Uploads in progrss: " + value.ToString();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            genToken();
            textBox1.Text = lastToken;
            key.buffer.Add(lastToken);

            textBox1.SelectAll();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            topFiles.files.Clear();
            topFiles.folders.Clear();
            topFiles.rootfiles.Clear();
            topFiles.rootfolders.Clear();
            topFiles.crossCheck.Clear();
            label3.Visible = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Process[] p = Process.GetProcesses();
            bool running = false;

            foreach (Process pr in p)
            {
                if (pr.ProcessName == "tor")
                {
                    running = true;
                    break;
                }
            }
            if (!running)
            {
                if (File.Exists(Directory.GetCurrentDirectory() + "\\tor\\tor.exe"))
                {
                    Process t = new Process();
                    t.StartInfo.FileName = "\\tor\\tor.exe";
                    t.StartInfo.CreateNoWindow = true;
                    t.StartInfo.RedirectStandardOutput = true;

                    t.Start();

                    new Thread(() => invoke_tor_output(t.StandardOutput)).Start();
                }
                else
                {
                    MessageBox.Show("Files missing", "Locurl");
                }
            }
            button7.Visible = true;

        }
        void invoke_tor_output(StreamReader r)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<StreamReader>(invoke_tor_output), new object[] { r });
                return;
            }
            textBox3.Text += r.ReadToEnd();
            textBox3.SelectionStart = textBox3.Text.Length;
            textBox3.ScrollToCaret();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int l = 0;
            string sip = "";

            displayExt = (displayExt % 3) + 1;

            switch (displayExt)
            {
                case 1:
                    l = ip.Length + 7;
                    sip = ip;
                    break;
                case 2:
                    l = inip.Length + 7;
                    sip = inip;
                    break;
                case 3:
                    sip = "octavius.ddns.net";
                    l = sip.Length + 7;
                    break;
            }


            textBox2.Text = String.Format("http://{0}{1}", sip,
            textBox2.Text.Substring(l, textBox2.Text.Length - l));


        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            //textBox2.Text.Replace("213.113.113.173:9000", "xetal.ddns.net");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var bm = cap.Snip();

            if (bm == null)
                return;

            string s2 = new string(DateTime.Now.ToString().Where(c => char.IsDigit(c)).ToArray());

            string nshort = s2 + ".png",
                nlong = "screenshots\\" + nshort;

            bm.Save(nlong);

            listBox1.Items.Add(nshort);
            topFiles.files.Add(nshort, nlong);
            topFiles.rootfiles.Add(nshort, nlong);

            Clipboard.SetText(String.Format("http://octavius.ddns.net/{0}", nshort));

        }
    }
    
    

}
