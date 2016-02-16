// Andreas Hansson 2013

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Windows.Forms;

namespace locurl
{
    public struct Folder2 // Dictionary<string, string> files, Dictionary<string, Folder2>
    // folders;
    {
        //Displayname, key
        public Dictionary<string, string> shortFiles;
        public Dictionary<string, string> shortFolders;
        public List<string> realpaths;
        public string ownpath;

        public Folder2(string path, int top, bool root, ref ListBox l)
        {
            shortFolders = new Dictionary<string, string>();
            shortFiles = new Dictionary<string, string>();
            realpaths = new List<string>();
            ownpath = path;
            foreach (string f in Directory.GetFiles(path))
            {
                string[] q = f.Split('\\');
                string p = f.Substring(top, f.Length - top);
                shortFiles.Add(q[q.Length - 1], p);
                topFiles.files.Add(p.Replace("\\", "/"), f);
                //l.Items.Add(q[q.Length - 1]);

                realpaths.Add(f);
            }
            foreach (string d in Directory.GetDirectories(path))
            {
                string[] q = d.Split('\\');
                string p = d.Substring(top, d.Length - top);
                shortFolders.Add(q[q.Length - 1], p);
                topFiles.folders.Add(p.Replace("\\", "/"), new Folder2(d, top, false, ref l));
                topFiles.crossCheck.Add(d);

                realpaths.Add(d);
            }
            if (root)
            {
                string a = path.Substring(top, path.Length - top).Replace("\\", "/");
                topFiles.rootfolders.Add(a, this);
                l.Items.Add(a);
            }
        }
    }
    static class topFiles
    {
        public static Dictionary<string, string> files =
            new Dictionary<string, string>(); // call path, real path
        public static Dictionary<string, Folder2> folders =
            new Dictionary<string, Folder2>(); // call path, linked folder
        // temporary solution
        public static Dictionary<string, string> rootfiles =
            new Dictionary<string, string>(); // call path, real path
        public static Dictionary<string, Folder2> rootfolders =
            new Dictionary<string, Folder2>(); // call path, linked folder

        public static List<string> crossCheck = new List<string>();
        //static string format(string top, string full)

    }
}
