using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;

namespace locurl
{
    public partial class settingsg : Form
    {
        public static bool lan = true, internet = false, autostart = true,
            log = true, index = true, save = false, uplan = true,
            upinternet = false, upkey = true;
        public static string port = "80";
        Button pwgen;

        public settingsg(ref Button pwgen)
        {
            InitializeComponent();
            setopen();
            checkBox7.Enabled = false;
            checkBox10.Enabled = false;
            this.pwgen = pwgen;
        }
        void saveclose()
        {
            uplan = checkBox1.Checked;
            upinternet = checkBox2.Checked;
            upkey = checkBox11.Checked;
            lan = checkBox6.Checked;
            internet = checkBox8.Checked;
            autostart = checkBox7.Checked;
            log = checkBox9.Checked;
            save = checkBox10.Checked;
            index = checkBox5.Checked;

            port = numericUpDown1.Value.ToString();
        }
        void setopen()
        {
            checkBox1.Checked = uplan;
            checkBox2.Checked = upinternet;
            checkBox11.Checked = upkey;
            checkBox6.Checked = lan;
            checkBox8.Checked = internet;
            checkBox7.Checked = autostart;
            checkBox9.Checked = log;
            checkBox10.Checked = save;
            checkBox5.Checked = index;

            numericUpDown1.Value = Convert.ToDecimal(port);
        }

        static public void loadfile()
        {
            try
            {
                StreamReader r = new StreamReader("settings");
                lan = Convert.ToBoolean(r.ReadLine());
                internet = Convert.ToBoolean(r.ReadLine());
                autostart = Convert.ToBoolean(r.ReadLine());
                log = Convert.ToBoolean(r.ReadLine());
                index = Convert.ToBoolean(r.ReadLine());
                save = Convert.ToBoolean(r.ReadLine());
                uplan = Convert.ToBoolean(r.ReadLine());
                upinternet = Convert.ToBoolean(r.ReadLine());
                upkey = Convert.ToBoolean(r.ReadLine());

                port = r.ReadLine();

                r.Dispose();
            }
            catch (Exception) { }
        }
        static public void savefile()
        {
            try
            {
                StreamWriter w = new StreamWriter("settings");
                w.WriteLine(lan.ToString());
                w.WriteLine(internet.ToString());
                w.WriteLine(autostart.ToString());
                w.WriteLine(log.ToString());
                w.WriteLine(index.ToString());
                w.WriteLine(save.ToString());
                w.WriteLine(uplan.ToString());
                w.WriteLine(upinternet.ToString());
                w.WriteLine(upkey.ToString());

                w.WriteLine(port);

                w.Dispose();
            }
            catch (Exception) { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (port != numericUpDown1.Value.ToString())
            {
                HServer.portchanged = true;
                TcpClient c = new TcpClient("127.0.0.1", Convert.ToInt32(port));
                c.Close();
            }
            saveclose();
            savefile();
            this.Close();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                checkBox1.Checked = true;
            }
            ichange();
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked)
            {
                checkBox6.Checked = true;
            }
            ichange();
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            ichange();
        }

        void ichange()
        {
            if (pwgen == null)
                return;
            if (checkBox2.Checked && checkBox8.Checked)
            {
                pwgen.Enabled = checkBox11.Checked;
            }
            else
            {
                pwgen.Enabled = true;
            }
        }
       
    }
}
