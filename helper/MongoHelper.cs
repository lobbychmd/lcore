using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using MongoDB.Driver;



namespace l.core
{
    public class MongoDBHelper
    {
        //public static List<string> MongoDBConnectionSetting = ConfigurationManager.AppSettings["MongoDBConnectionSetting"] != null ?
        //    ConfigurationManager.AppSettings["MongoDBConnectionSetting"].Split(';').ToList() : null;

        public static List<string> MongoDBConnectionSetting(){
            return ConfigurationManager.AppSettings["MongoDBConnectionSetting"] != null ?
            ConfigurationManager.AppSettings["MongoDBConnectionSetting"].Split(';').ToList() : null;
        }
        private static MongoServer server;
        private static MongoDatabase db;
        public static MongoDatabase GetMongoDB(bool require = true)
        {
            if (db == null)
            {
                var sets = MongoDBConnectionSetting();
                if (sets == null) {
                    if (require) throw new Exception("mongodb not configured in web.config !");
                }
                else {
                    var conn = new MongoConnectionStringBuilder();
                    conn.Server = new MongoServerAddress(sets[0].Split(':')[0], Convert.ToInt32(sets[0].Split(':')[1]));
                    sets.RemoveAt(0);
                    Dictionary<string, string> dic = sets.ToDictionary(p => p.Split('=')[0], q => q.Split('=')[1]);
                    if (server == null)
                    {
                        // 连接到一个MongoServer上
                        if (dic.ContainsKey("username")) conn.Username = dic["username"];
                        if (dic.ContainsKey("password")) conn.Password = dic["password"];
                        if (dic.ContainsKey("database")) conn.DatabaseName = dic["database"];
                        server = MongoServer.Create(conn);
                    }
                    // 打开数据库testdb
                    if (!dic.ContainsKey("database")) throw new Exception("mongodb configure missing 'database' in web.config !");
                    else db = server.GetDatabase(dic["database"]);
                }
            }
            return db;
        }
        /// <summary>
        /// 通过DBRef获取对象
        /// </summary>
        /// <param name="dbRef"></param>
        /// <returns></returns>
        public static T GetObjectByDBRef<T>(MongoDBRef dbRef)
        {
            MongoDatabase db = MongoDBHelper.GetMongoDB();
            return db.FetchDBRefAs<T>(dbRef);
        }
        /// <summary>
        /// 通过多个DBRef获取数据
        /// </summary>
        /// <param name="dbRefList">多个DBRef</param>
        /// <returns>List<Object>数据</returns>
        public static List<T> GetListByDBRef<T>(List<MongoDBRef> dbRefList)
        {
            List<T> list = new List<T>();
            MongoDatabase db= MongoDBHelper.GetMongoDB();
            if (dbRefList != null)
            {
                foreach (MongoDBRef dbRef in dbRefList)
                {
                    list.Add( db.FetchDBRefAs<T>(dbRef));
                }
            }
            return list;
        }
        /// <summary>
        /// 通过多个DBRef获取数据
        /// </summary>
        /// <param name="dbRefList">多个DBRef</param>
        /// /// <param name="skip">开始位置，不包括该项</param>
        /// /// <param name="limit">取记录数</param>
        /// <returns>List<Object>数据</returns>
        public static List<T> GetListByDBRef<T>(List<MongoDBRef> dbRefList,int skip,int limit)
        {
            List<T> list = new List<T>();
            MongoDatabase db = MongoDBHelper.GetMongoDB();
            if (dbRefList != null)
            {
                for(int i=skip;i<=(skip+limit);i++)
                {
                    list.Add(db.FetchDBRefAs<T>(dbRefList[i]));
                }
            }
            return list;
        }
    }
}
