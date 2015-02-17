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
            /*using (var conn = l.core.DBHelper.GetConnection("FirebirdDB"))
            {
                l.core.DBHelper.ExecuteSql(conn, @"
                    insert into Logs( id, nLevel, sWhen, sWhat, sWho, sWhere, sData)
                              values(@id, @Level, @When, null, null, null, null)
                ", new Dictionary<string, DBParam> { 
                    {"@id", new DBParam{ParamValue = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), DbType = System.Data.DbType.String}},
                    {"@Level", new DBParam{ParamValue =  level, DbType = System.Data.DbType.Int32}},
                    {"@When", new DBParam{ParamValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DbType = System.Data.DbType.String}},
                   // {"@What", new DBParam{ParamValue = what, DbType = System.Data.DbType.String}},
                   // {"@Who", new DBParam{ParamValue = who, DbType = System.Data.DbType.String}},
                   // {"@Where", new DBParam{ParamValue = where, DbType = System.Data.DbType.String}},
                    //{"@Data", new DBParam{ParamValue = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), DbType = System.Data.DbType.String}},
                 });
            }*/


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
