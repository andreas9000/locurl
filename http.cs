// Andreas Hansson 2013

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Web;
using System.Reflection;

namespace locurl
{


    public class hpr
    {
        static public Dictionary<string, string> mime = Form1.content();

        Stream iS;
        StreamWriter oS;
        TcpClient socket;
        HServer srv;
        String http_method, http_url, http_protocol_versionstring;
        Hashtable httpHeaders = new Hashtable();

        private static int MAX_POST_SIZE = 8096000;

        public hpr(TcpClient s, HServer srv)
        {
            this.socket = s;
            this.srv = srv;
        }

        private string srl(Stream inS)
        {
            int nchar;
            string data = string.Empty;
            while (true)
            {
                nchar = inS.ReadByte();
                if (nchar == '\n') { break; }
                if (nchar == '\r') { continue; }
                if (nchar == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(nchar);
            }
            return data;
        }
        public void process()
        {
            long content_len = 0;
            string fname = string.Empty;
            iS = new BufferedStream(socket.GetStream());
            oS = new StreamWriter(new BufferedStream(socket.GetStream()));
            
            try
            {
                parseRequest();
                rHeaders();
                if (http_method.Equals("GET"))
                {
                    handleGET();
                }
                else if (http_method.Equals("POST"))
                {
                    srv.ff.append(String.Format("POST from {0}", srv.lastip));
                    if ((srv.lastip != "127.0.0.1" && !settingsg.uplan) || (!(srv.lastip.StartsWith("192.168.") ||
                srv.lastip == "127.0.0.1") && !settingsg.internet))
                    {
                        if (key.iplist.ContainsKey(srv.lastip) && settingsg.upkey)
                        {
                            if ((DateTime.Now - key.iplist[srv.lastip]).Minutes < 15)
                            {
                                goto skip;
                            }
                            else
                            {
                                key.iplist.Remove(srv.lastip);
                            }
                        }
                        goto skip2;
                    }
                skip:
                    Form1.action++;
                    handlePOST(out content_len, out fname);
                    Form1.action--;
                skip2: { }
                }
                oS.Flush();
            }
            catch (Exception)
            {
                fail();
            }

            iS = null;
            oS = null;
            socket.Close();
            
            if (gotfile)
            {
                gotfile = false;
                if (!Directory.Exists(srv.lastip))
                    Directory.CreateDirectory(srv.lastip);
                int fc = 0;
            filename:
                if (File.Exists(String.Format("{0}\\{2}{1}", srv.lastip, fname, (fc == 0 ? string.Empty : String.Format("({0})", fc.ToString())))))
                {
                    fc++;
                    goto filename;
                }
            File.Move("tempc.dat", String.Format("{0}\\{2}{1}", srv.lastip, fname, (fc == 0 ? string.Empty : String.Format("({0})", fc.ToString()))));
                srv.ff.append(String.Format("{0} recieved from {1}", fname, srv.lastip));
            }
        }

        bool gotfile = false;

        public void parseRequest()
        {
            String request = srl(iS);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                fail();
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];
        }

        public void rHeaders()
        {
            String line;
            while ((line = srl(iS)) != null)
            {
                if (line.Equals(string.Empty))
                    return;

                int separator = line.IndexOf(':');
                if (separator == -1)
                    return;
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                    pos++;
                httpHeaders[name] = line.Substring(pos, line.Length - pos);
            }
        }

        public void handlePOST(out long content_len, out string fname)
        {
            content_len = 0;
            Stream fs = File.Create("tempc.dat");
            byte[] cm = { 0xD, 0xA, 0xD, 0xA },
                    str = { 0x66, 0x69, 0x6c, 0x65, 0x6e, 0x61, 0x6d, 0x65, 0x3d, 0x22 };
            int hspot = 0;
            fname = string.Empty;

            long buf_size = 1024;

            if (this.httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);

                byte[] buf = new byte[buf_size];
                long to_read = content_len;

                while (to_read > 0)
                {
                    int numread = this.iS.Read(buf, 0, (int)Math.Min(buf_size, to_read));
                    if (numread == 0 && to_read == 0)
                        break;
                    if (to_read == content_len) // first
                    {
                        for (int i = 0, p = 0, tp = 0; i < buf_size && p < 4; i++)
                        {
                            if (tp < 10)
                            {
                                if (buf[i] == str[tp])
                                {
                                    tp++;
                                }
                                else
                                    tp = 0;
                            }
                            else
                            {
                                if (buf[i + 1] == 0x22)
                                    tp = 0;
                                fname += (char)buf[i];
                            }
                            if (buf[i] == cm[p])
                            {
                                p++;
                                hspot = i + 1;
                            }
                            else
                                p = 0;
                        }
                        fs.Write(buf, hspot, content_len > buf_size ?
                            numread - hspot : numread - hspot - 46);
                    }
                    else if (numread == buf_size && to_read < buf_size + 46)
                    {
                        fs.Write(buf, 0, (int)to_read - 46);
                    }
                    else if (numread < 1024)
                    {
                        if (numread > 46)
                            fs.Write(buf, 0, numread - 46);
                    }
                    else
                        fs.Write(buf, 0, numread);
                    to_read -= numread;
                    //srv.ff.backgroundWorker1.ReportProgress(100 * (BUF_SIZE / content_len));
                }
                writeSuccess();

                fs.Seek(0, SeekOrigin.Begin);
                fs.Dispose();
                
                gotfile = true;
            }
        }

        public void writeSuccessBase(string mime, long size = 0)
        {
            System.Text.ASCIIEncoding ec = new System.Text.ASCIIEncoding();
            string bstore = String.Format("HTTP/1.1 200 OK\r\nContent-Type: {0}\r\n{1}Connection: close\r\n\r\n", mime, size == 0 ? string.Empty :
                "Content-Length: " + size.ToString());
            oS.BaseStream.Write(ec.GetBytes(bstore), 0, bstore.Length);
        }
        public void writeSuccess(string content_type = "text/html", int length = 0)
        {
            oS.WriteLine("HTTP/1.1 200 OK");
            oS.WriteLine("Content-Type: " + content_type);
            //if (length != 0)
                //outputStream.WriteLine("Content-Length: " + length.ToString());
            //
            oS.WriteLine("Connection: close");
            oS.WriteLine("");
        }


        public void fail()
        {
            oS.WriteLine("HTTP/1.1 404 File not found");
            oS.WriteLine("Connection: close");
            oS.WriteLine("");
        }

        string[] exclude = { "ink/main.css", "ink/dropzone.css", "ink/dropzone.js", "ink/Folder32.png", "ink/gradient.jpg", "ink/spritemap.png", "ink/spritemap@2x.png", "ink/upload.png", "favicon.ico", "ink/archive-2-64.png", "ink/video.dev.js", "ink/video.js", "ink/video-js.css", "ink/video-js.min.css", "ink/video-js.swf" },
            cmime = { "text/css", "text/css", "text/javascript", "image/png", "image/jpeg", "image/png", "image/png", "image/png", "image/x-icon", "image/png", "text/javascript", "text/javascript", "text/css", "text/css", "application/x-shockwave-flash" },
            fname = { "main.css", "dropzone.css", "dropzone.js", "Folder32.png", "gradient.jpg", "spritemap.png", "spritemap@2x.png", "upload.png", "favicon.ico", "archive-2-64.png", "video.dev.js", "video.js", "video-js.css", "video-js.min.css", "video-js.swf" },
            rname = { "main", "dropzone", "dropzone1", "Folder32", "gradient", "spritemap", "spritemap_2x", "upload", "favicon", "archive_2_64", "video.dev.js", "video.js", "video-js.css", "video-js.min.css", "video-js.swf" };

        public void handleGET()
        {
            http_url = System.Uri.UnescapeDataString(http_url).Replace("\\", "/");
            if (http_url.Length > 1)
                http_url = http_url.Substring(1, http_url.Length - (http_url.EndsWith("/") ? 2 : 1));

            bool cont = false;

            foreach (string cmp in exclude)
                if (cmp == http_url)
                    cont = true;
            srv.ff.append(String.Format("{3}{2}- {0} requested from {1}", http_url, srv.lastip, DateTime.Now, cont ? "-log- " : string.Empty));

            #region commented
            /*
            if (http_url.StartsWith("/?"))
            {
                if (http_url.Substring(2, http_url.Length - 2) == Form1.lastToken)
                {
                    if (Form1.white.ContainsKey(srv.lastip))
                        Form1.white[srv.lastip] += 5;
                    else Form1.white.Add(srv.lastip, 5);

                    outputStream.WriteLine("<html><body><p>Access added to {0}</p></body>", srv.lastip);

                    Form1.genToken();
                    srv.ff.append("?" + Form1.lastToken);
                }
                else
                    outputStream.WriteLine("<html><body><p>Invalid key</p></body>", srv.lastip);
            }
            else if (http_url.StartsWith("/rediradd/"))
            {
                if (Form1.white.ContainsKey(srv.lastip))
                {
                    if (Form1.white[srv.lastip] > 0)
                    {
                        int p = http_url.LastIndexOf('/') + 1;
                        string link = http_url.Substring(p, http_url.Length - p),
                            newurl = string.Empty;
                        if (!(link.StartsWith("http://")))
                            link = "http://" + link;

                        if (Form1.redir.ContainsValue(link) && false)
                            Form1.redir.Remove(link);
                        else
                        {
                            int p2 = http_url.Substring(1,
                                http_url.Length - 1).IndexOf('/') + 2;
                            newurl = http_url.Substring(p2, http_url.Length - p - p2 + 3);
                            Form1.redir.Add(newurl, link);
                        }
                        outputStream.WriteLine("<html><body><p><a href={0}</a></p></body>", srv.ip + "/redir/" + newurl);
                        srv.ff.append(newurl);
                        Form1.white[srv.lastip]--;
                    }
                    else
                        outputStream.WriteLine("<html><body><p>Access denied</p></body>");
                }
                else
                    outputStream.WriteLine("<html><body><p><a href=../>Access denied!</a></p></body>");
            }
            else if (http_url.StartsWith("/redir/"))
            {
                string key = http_url.Substring(7, http_url.Length - 7);
                if (Form1.redir.ContainsKey(key))
                    outputStream.WriteLine("<html><head><meta http-equiv=\"refresh\" content=\"0; url={0}\"</body>",
                        Form1.redir[key]);
                else
                    outputStream.WriteLine("<html><body><p>{0} not found on server!</p></body></form>", http_url);
            }
            else
            {
                switch (http_url)
                {
                    case "/old":
                        {
                            if (srv.index == 1)
                            {
                                outputStream.WriteLine("<html><body><h1>File index:</h1><ul>");
                                foreach (KeyValuePair<string, string> pr in Form1.index)
                                    outputStream.WriteLine(String.Format("<li><p><a href=\"{0}\">{0}</a></p>", pr.Key));
                                outputStream.WriteLine("</ul>");
                            }
                            else
                                writeFailure();
                            break;
                        }
                    case "/box":
                        {
                            outputStream.WriteLine("<form id=\"uploadbanner\" type=\"multipart/form-data\" method=\"post\" action=\"#\"><input typw=\"hidden\" name=\"Content-Length\" /><input id=\"fileupload\" name=\"myfile\" type=\"file\" /><input type=\"submit\" value=\"submit\" id=\"submit\" /></form>");
                            break;
                        }
                    case "/ip":
                        {
                            outputStream.WriteLine("<html><body><h1>Your IP is {0}</h1></body>", srv.lastip);
                            break;
                        }
                    case "/trap":
                        {
                            string t = Form1.rnd();
                            Form1.urls.Add(t);
                            outputStream.WriteLine("<html><body><p>Send the user this link: {0}\nClick <a href={1}>here</a> to check the status</p></body>",
                                String.Format("http://", Form1.ip, ":", Form1.port, "/", "xaxaxa&", t),
                                String.Format("check&", t));
                            break;
                        }
                }
            }*/
            #endregion


            if (http_url.Contains("?key="))
            {
                string k = http_url.Substring(5, http_url.Length - 5);

                if (key.buffer.Contains(k))
                {
                    key.buffer.RemoveAt(key.buffer.IndexOf(k));
                    if (!key.iplist.ContainsKey(srv.lastip))
                        key.iplist.Add(srv.lastip, DateTime.Now);
                    else
                        key.iplist[srv.lastip] = DateTime.Now;
                }
                if (http_url.Length > 15)
                {
                    http_url = http_url.Substring(0, 15);
                }
            }
            for (int i = 0; i < exclude.Length; i++)
            {
                if (http_url == exclude[i])
                {
                    writeSuccessBase(cmime[i]);
                    Stream f = File.Open(@"Raw\" + fname[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                    f.CopyTo(oS.BaseStream);
                    oS.BaseStream.Flush();
                    f.Dispose();
                    /*
                    using (var fhold = Assembly.GetEntryAssembly().GetManifestResourceStream("Properties.Resources." + rname[i]))
                    {
                        fhold.CopyTo(outputStream.BaseStream);
                    }
                    outputStream.BaseStream.Flush();*/
                    return;
                }
            }
            if ((srv.lastip != "127.0.0.1" && !settingsg.lan) || (!(srv.lastip.StartsWith("192.168.") ||
                srv.lastip == "127.0.0.1") && !settingsg.internet))
            {
                if (key.iplist.ContainsKey(srv.lastip))
                {
                    //if ((DateTime.Now - key.iplist[srv.lastip]).Minutes < 15)
                        goto skip;
                    //else
                        key.iplist.Remove(srv.lastip);
                }
                writeSuccess();
                oS.WriteLine("<!DOCTYPE html><html><head><title>Locurl</title><link rel=\"stylesheet\" type=\"text/css\" href=\"ink/main.css\"></head><body><form name=\"input\" action=\"/\" method=\"get\"><input type=\"text\" name=\"key\" id=\"keybox\"><input type=\"submit\" value=\"Submit\" id=\"button\"></form></body></html>");
                return;
            }
        skip:
            if (http_url == "upload")
            {
                writeSuccess();
                oS.WriteLine("<!DOCTYPE html><html><head><title>Locurl</title><link rel=\"stylesheet\" type=\"text/css\" href=\"/ink/dropzone.css\"><style>body { background-image:url('/ink/gradient.jpg'); }</style>");
                oS.WriteLine("</head><body><script src=\"/ink/dropzone.js\"></script><form action=\"/file-upload\" class=\"dropzone\"><div class=\"fallback\"><input name=\"file\" type=\"file\" multiple /></div></form></body></html>");
                return;
            }
            else if (http_url.StartsWith("video/"))
            {
                writeSuccess();
                oS.WriteLine("<!DOCTYPE html><html><head><title>Locurl</title><link rel=\"stylesheet\" type=\"text/css\" href=\"ink/main.css\"><link href=\"ink/video-js.css\" rel=\"stylesheet\" type=\"text/css\"><script src=\"ink/video.js\"></script><script>videojs.options.flash.swf = \"ink/video-js.swf\";</script></head><body><div class=\"center\" id=\"root\"><video id=\"videoform\" class=\"video-js vjs-default-skin\" controls preload=\"none\" width=\"640\" height=\"264\" data-setup=\"{{}}\"><source src=\"/{0}\" type='video/mp4' /></video></body></html>", http_url.Substring(6, http_url.Length - 6));
                return;
            }

            if ((http_url == "/" || http_url.StartsWith("?")) && settingsg.index)
            {
                writeSuccess();
                //string baseHTML = "<!DOCTYPE html><html><head><title>Locurl</title><link rel=\"stylesheet\" type=\"text/css\" href=\"ink/main.css\"></head><body><div class=\"center\"><ul id=\"sub\">{0}</ul></div><p id=\"intro\">LOCURL</p></body></html>";
                oS.WriteLine("<!DOCTYPE html><html><head><title>Locurl</title><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" /><link rel=\"stylesheet\" type=\"text/css\" href=\"ink/main.css\"></head><body><div class=\"center\" id=\"root\"><ul id=\"sub\">");
                foreach (KeyValuePair<string, Folder2> k in topFiles.rootfolders)
                {
                    oS.WriteLine("<li class=\"folder\"><a href={0}>{1}</a></li>", k.Key.Replace(" ", "%20"), k.Key);
                    //srv.ff.append(k.Key);
                }
                foreach (KeyValuePair<string, string> k in topFiles.rootfiles)
                {
                    oS.WriteLine("<li class=\"file\"><a href={0}>{1}</a></li>", k.Key.Replace(" ", "%20"), k.Key.Length <= 40 ? k.Key : k.Key.Substring(0, 40) + "...");//k.Key.Substring(0, Math.Min(40, k.Key.Length)));
                    //srv.ff.append(k.Key);
                }
                oS.WriteLine("</ul><span title=\"Download as zip\"><a id=\"zip\" href=\"/index.zip?zip\">Download as zip</a></span></div><!-- <p id=\"intro\">LOCURL</p> --><span title=\"Upload files\"><a id=\"upl\" href=\"/upload\"></a></span><p id=\"cred\">Locurl created by Andreas Hansson</p></body></html>");
                return;
            }
            else
            {
                if (topFiles.files.ContainsKey(http_url))
                {
                    //srv.ff.appendn(2);
                    string[] lgkt = http_url.Split('.');
                    lgkt[lgkt.Length - 1] = lgkt[lgkt.Length - 1].ToLower();

                    Stream f = File.Open(topFiles.files[http_url],
                        FileMode.Open, FileAccess.Read, FileShare.Read);

                    if (mime.ContainsKey(lgkt[lgkt.Length - 1]))
                    {
                        writeSuccessBase(mime[lgkt[lgkt.Length - 1]], f.Length);
                    }
                    else
                    {
                        writeSuccessBase("application/octet-stream", f.Length);
                    }
                    //Form1.action++;

                    f.CopyTo(oS.BaseStream);
                    oS.BaseStream.Flush();
                    f.Dispose();
                    //srv.ff.appendn(3);
                    //Form1.action--;
                    return;

                }
                else if (topFiles.folders.ContainsKey(http_url) && settingsg.index)
                {
                    writeSuccess();
                    oS.WriteLine("<!DOCTYPE html><html><head><title>Locurl</title><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" /><link rel=\"stylesheet\" type=\"text/css\" href=\"/ink/main.css\"></head><body><div class=\"center\"><ul id=\"sub\">");
                    foreach (KeyValuePair<string, string> k in topFiles.folders[http_url].shortFolders)
                    {
                        oS.WriteLine("<li class=\"folder\"><a href=/{0}>{1}</a></li>", k.Value.Replace(" ", "%20"), k.Key);
                    }
                    foreach (KeyValuePair<string, string> k in topFiles.folders[http_url].shortFiles)
                    {
                        oS.WriteLine("<li class=\"file\"><a href=/{0}>{1}</a></li>", System.Uri.EscapeDataString(k.Value), k.Key.Length <= 40 ? k.Key : k.Key.Substring(0, 40) + "...");
                    }
                    oS.WriteLine("</ul><span title=\"Download as zip\"><a id=\"zip\" href=\"/{0}.zip?zip\">Download as zip</a></span></div><!-- <p id=\"intro\">LOCURL</p> --><a id=\"upl\" href=\"/upload\"></a></body></html>", http_url);
                    return;
                }
                else if (http_url.EndsWith("?zip") && settingsg.index)
                {
                    byte[] b;
                    uint t;
                    List<zfile> l;
                    if (http_url == "index.zip?zip")
                    {
                        http_url = "/";
                        var c = new List<string>();
                        foreach (KeyValuePair<string, string> k in topFiles.rootfiles)
                            c.Add(k.Value);
                        foreach (KeyValuePair<string, Folder2> k in topFiles.rootfolders)
                            c.Add(k.Value.ownpath);
                        new zip(c, out l, out t, out b);
                    }
                    else
                    {
                        http_url = http_url.Substring(0, http_url.Length - 8);
                        new zip(topFiles.folders[http_url].realpaths, out l, out t, out b);
                    }

                    //if (topFiles.folders.ContainsKey(http_url))
                    {

                        writeSuccessBase("application/zip", t);
                        foreach (zfile i in l)
                        {
                            oS.BaseStream.Write(i.b, 0, i.b.Length);
                            Stream s = File.Open(i.path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            s.CopyTo(oS.BaseStream);
                            s.Dispose();
                        }
                        oS.BaseStream.Write(b, 0, b.Length);
                        oS.BaseStream.Flush();
                    }
                    return;
                }
                else
                {
                    writeSuccess();
                    oS.WriteLine("<!DOCTYPE html><html><head><title>Locurl</title><link rel=\"stylesheet\" type=\"text/css\" href=\"/ink/main.css\"></head><body><div class=\"center\">");
                    oS.WriteLine("</div><p id=\"intro\">404 NOT FOUND</p><a id=\"upl\" href=\"/upload\"></a></body></html>");
                    return;
                }
            }

        }
    }
    

}

