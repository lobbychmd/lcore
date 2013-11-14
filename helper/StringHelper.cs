using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    static public class StringHelper
    {
        static public string Summary(this string str, int length = 128) {
            int l = str.Length;
            string result = str.Substring(0, System.Math.Min(l, length));
            if (l > length) result += "...";
            return result;
        }

        static public string[] SmartSplit(this  string str)
        {
            var s = str.Split('.').ToList();
            if (s.Count() == 1) s.Add(str);
            return s.ToArray();
        }
    }
}
