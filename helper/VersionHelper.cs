using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    public interface IVersionHelper
    {
        bool CheckNewAs<T>(object obj, string metaType, string[] keyFields, bool updateLocal);
        object GetAs<T>(string metaType, Dictionary<string, string> keyValues);
        string GetStr(string metaType, Dictionary<string, string> keyValues);
        bool Suspend { get; set; }
        List<string> Action { get; set; }

        void InvokeRec<T>(object obj, string metaType, string[] keyFields, string ParamsValue, int timeCost);
    }

    public class VersionHelper {
        public static IVersionHelper Helper { get; set; }
        
    }
}
