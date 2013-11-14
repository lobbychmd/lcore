using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System.Web;
using Newtonsoft.Json;

namespace l.core
{
    public enum LogLevel{llInfo, llDebug, llWarning, llError, llFatalError}

    public class LogItem{
        public ObjectId _id{get;set;}
        public LogLevel Level {get;set;}
        public string When {get;set;}
        public string What {get;set;}
        public string Who {get;set;}
        public string Where {get;set;}
        public string Data {get;set;}
    }

    public interface LogObj {
        void LogSh(string what, object data, LogLevel level = LogLevel.llInfo, string who = null, string where = null);
    }

    public class LogHelper : LogObj
    {
        static public LogObj Obj;

        static public void Log(string what, object data, LogLevel level = LogLevel.llInfo, string who = null, string where = null){
            if (Obj == null) Obj = new LogHelper();
            Obj.LogSh(what, data, level, who, where);
        }

        public void LogSh(string what, object data, LogLevel level = LogLevel.llInfo, string who = null, string where = null)
        {
            var db =  MongoDBHelper.GetMongoDB(false);
            if (db == null) return;

            var c = db.GetCollection<LogItem>("Logs");
            c.Save(new LogItem { 
                Level = level,
                When = DateTime.Now.ToString(),
                What = what,
                Who = who,
                Where = where,
                Data = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })
            });
        }
    }
}
