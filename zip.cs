// Andreas Hansson 2013

using System;
using System.Collections.Generic;
using System.IO;

namespace locurl
{
    class zip
    {
        public int name_length = 0, cdir_pointer = 0, size_pointer;
        public long total_size = 0;
        List<string> stringpaths = new List<string>(),
            realpaths = new List<string>();
        List<zfile> chunks = new List<zfile>();
        public byte[] cdir;
        
        public zip(List<string> dirs, out List<zfile> segments, out uint tsize, out byte[] cd)
        {
            foreach (string i in dirs)
                iterate(i, i.LastIndexOf('\\') + 1);

            cdir = new byte[(stringpaths.Count * 46) + name_length + 22];

            foreach (zfile i in chunks)
            {
                byte[] h = reform(i);

                for (int j = 0; j < h.Length; j++)
                    cdir[cdir_pointer + j] = h[j];
                
                cdir_pointer += h.Length;
            }
            // EOCD
            cdir[cdir_pointer] = 0x50;
            cdir[cdir_pointer + 1] = 0x4b;
            cdir[cdir_pointer + 2] = 5;
            cdir[cdir_pointer + 3] = 6;

            byte[] dc = BitConverter.GetBytes((Int16)stringpaths.Count), 
                offset = BitConverter.GetBytes((int)(total_size + name_length + (30 * stringpaths.Count))), 
                cdirsize = BitConverter.GetBytes((int)(name_length + (46 * stringpaths.Count)));

            for (int i = 0; i < 4; i++)
            {
                cdir[cdir_pointer + 8 + i] = dc[i % 2];
                cdir[cdir_pointer + 16 + i] = offset[i];
                cdir[cdir_pointer + 12 + i] = cdirsize[i];
            }
            cd = cdir;
            tsize = (uint)(total_size + (name_length * 2) + (76 * stringpaths.Count) + 22);
            segments = chunks;
        }

        void iterate(string path, int mainpath)
        {
            if (File.Exists(path))
                fileadd(path, mainpath);
            else if (Directory.Exists(path))
            {
                foreach (string g in Directory.GetFiles(path))
                    fileadd(g, mainpath);
                
                foreach (string g in Directory.GetDirectories(path))
                    iterate(g, mainpath);
            }
        }
        void fileadd(string path, int mainpath)
        {
            FileInfo info = new FileInfo(path);

            if (info.Length >= 4294967296)
                return;

            string l = path.Substring(mainpath, path.Length - mainpath);
            stringpaths.Add(l);
            realpaths.Add(path);
            name_length += l.Length;
            total_size += info.Length;

            chunks.Add(new zfile(path, l, info.Length, info.LastWriteTime.Hour,
                info.LastWriteTime.Minute, info.LastWriteTime.Second, info.LastWriteTime.Year,
                info.LastWriteTime.Month, info.LastWriteTime.Day, this));
        }

        public static byte[] datetime(int[] times) // h, min, sec, year, month, day
        {
            times[0] /= 2;
            times[5] -= 1980;
            int[] size = { 5, 6, 5, 7, 4, 5 },
                bits = new int[32],
                conv = { 128, 64, 32, 16, 8, 4, 2, 1 };
            byte[] ret = new byte[4];
            int t = 0;

            for (int i = 0; i < 6; i++)
            {
                int[] ins = getBinary(times[i], size[i]);

                for (int j = 0; j < size[i]; j++)
                    bits[t + j] = ins[j];

                t += size[i];
            }
            for (int i = 0; i < 32; i++)
            {
                ret[i / 8] += (byte)(bits[i] * conv[i % 8]);
            }
            return ret;
        }
        public static int[] getBinary(int v, int pad) // pad <= 7
        {
            int[] r = { 64, 32, 16, 8, 4, 2, 1 },
                ret = new int[pad];

            for (int i = 7 - pad; i < 7; i++)
            {
                if (v - r[i] >= 0)
                {
                    v -= r[i];
                    ret[i - 7 + pad] = 1;
                }
            }
            return ret;
        }
        static byte[] reform(zfile z)
        {
            byte[] hold = new byte[z.b.Length + 16];
            Array.Copy(new byte[] { 0x50, 0x4b, 1, 2, 0xA, 0, 0xA }, hold, 7);

            for (int i = 10; i < 28; i++)
                hold[i + 2] = z.b[i];

            for (int i = 30; i < z.b.Length; i++)
                hold[i + 16] = z.b[i];

            for (int i = 0; i < 4; i++)
                hold[i + 42] = z.rel_start[i];
            return hold;
        }
    }
    struct zfile
    {
        public string path;
        public byte[] b, rel_start;

        public zfile(string realpath, string stringpath, long size,
            int sec, int min, int h, int day, int mon, int year, zip zf)
        {
            path = realpath;
            b = new byte[30 + stringpath.Length];

            b[0] = 0x50; b[1] = 0x4b;
            b[2] = 3; b[3] = 4; b[4] = 0xA;

            FileStream f = File.Open(realpath, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] dt = zip.datetime(new int[] { sec, min, h, day, mon, year }),
                crc32 = new Crc32().ComputeHash(f),
                s = BitConverter.GetBytes((uint)size),
                fns = BitConverter.GetBytes(stringpath.Length);

            rel_start = BitConverter.GetBytes(zf.size_pointer);

            zf.size_pointer += stringpath.Length + 30 + (int)size;
            f.Dispose();
            Array.Reverse(crc32);

            if (BitConverter.IsLittleEndian && false)
            {
                Array.Reverse(s);
                Array.Reverse(fns);
            }
            for (int i = 0; i < 4; i++)
            {
                b[10 + i] = dt[i];
                b[14 + i] = crc32[i];
                b[18 + i] = s[i];
                b[22 + i] = s[i];
            }
            b[26] = fns[0];
            b[27] = fns[1];

            for (int i = 0; i < stringpath.Length; i++)
                b[i + 30] = (byte)stringpath[i];
            
        }
    }
}
