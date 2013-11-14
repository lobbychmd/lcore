using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace l.core
{
    public class ScriptHelper
    {
        static public object GetDynamicProp(dynamic obj, string propName)
        {
            var typ = (obj as object).GetType();
            var prop = typ.GetProperties().ToList().Find(p => p.Name == propName);
            return prop != null ? prop.GetValue(obj, null) : null;
        }

        static public string GetHashCode<T>(object obj)
        {
            return JsonConvert.SerializeObject(
                        JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj))
                     ).GetHashCode().ToString();
        }


        //就是反序列化为基类（除去自身的hashcode属性）再求 hashcode
        static public string GetHashCode(object obj)
        {
            return JsonConvert.SerializeObject(
                 typeof(JsonConvert).GetMethods().ToList().FindAll(p => p.Name == "DeserializeObject")[3]
                     .MakeGenericMethod(obj.GetType().BaseType)
                     .Invoke(null, BindingFlags.Static, null, new object[] { JsonConvert.SerializeObject(obj) }, null)
                     ).GetHashCode().ToString();
        }
    }
}
