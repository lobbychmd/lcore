using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Web;
using System.Reflection;

namespace l.core
{
    public class Log
    {
        //static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static ILog log = LogManager.GetLogger("");
        static private string Convert(object obj) {
            if (obj == null) return string.Empty;
            else if (typeof(System.String) == obj.GetType())
                return obj.ToString();
            else if (typeof(System.Data.SqlClient.SqlParameterCollection) == obj.GetType())
            {
                string r = "";
                foreach (System.Data.SqlClient.SqlParameter i in (System.Data.SqlClient.SqlParameterCollection)obj)
                    r += i.ParameterName + "=" + i.Value.ToString() + ";";
                return r;
            }
            else if (typeof(System.Collections.Specialized.NameValueCollection) == obj.GetType()){
                string r = "";
                var o = (System.Collections.Specialized.NameValueCollection)obj;
                foreach(string k in o.AllKeys){
                    r+= k + "=" + o[k] + "\n";
                }
                return r;
            }
            //else if (typeof(.DataParams) == obj.GetType())
            //{
            //    string r = "";
            //    foreach (l.core.DataParam i in ((DataParams)obj).Params)
            //        r += i.ParamName + "=" + System.Convert.ToString(i.ParamValue) + ";";
            //    return r;
            //}
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            //return (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(obj);
        }

        static private string Prefix(string prefix ) {
            return prefix == null ? "" : string.Format("<{0}> ", prefix);
        }
        static public void Info(object data, string summary = null, string prefix = null)
        {
            if (summary != null) log.Info(Prefix(prefix) + summary);
            log.Info(Prefix(prefix) + Convert(data));
        }

        static public void Debug(object data, string summary = null, string prefix = null)
        {
            if (summary != null) log.Debug(Prefix(prefix) + summary);
            log.Debug(Prefix(prefix) + Convert(data));
        }
    }
}
