using System;
using System.Collections;
using System.Collections.Generic;

namespace locurl
{
    class key
    {
        public static Dictionary<string, DateTime> iplist = new Dictionary<string, DateTime>();
        public static List<string> buffer = new List<string>();

        void timeclear(int min = 15)
        {
            foreach (KeyValuePair<string, DateTime> i in iplist)
            {
                if ((i.Value - DateTime.Now).Minutes > min)
                    buffer.Remove(i.Key);
            }
        }
    }
}
