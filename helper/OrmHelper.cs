using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace l.core
{
    public class OrmConfig {
        public OrmHelper End { get; set; }
        public string[] PKey;
        public string[] Fields;
        public object Object;
        public string TableName { get; set; }
        public Dictionary<string, string> matchSetting;

        public OrmConfig()
        {
            matchSetting = new Dictionary<string, string>();
        }

        public OrmConfig MF(string propName, string fieldName)
        {
            matchSetting[propName] = fieldName;
            return this;
        }

        public OrmConfig PK(params string[] field)
        {
            this.PKey = field;
            return this;
        }

        public OrmConfig F(params string[] field)
        {
            this.Fields = field;
            return this;
        }

        public OrmConfig Obj(object obj)
        {
            this.Object = obj;
            return this;
        }


        public string[] DBFields(object obj = null)
        { 
            obj = obj?? Object;
            return (((Fields == null) || (Fields.Count() == 0)) ? obj.GetType().GetProperties().Select(p => p.Name) : Fields)
                .Select(p => matchSetting.ContainsKey(p) ? matchSetting[p] : p).Where(p=> !string.IsNullOrEmpty(p)).ToArray() ;
        }

        
    }

    public class OrmHelper
    {
        private bool frm;
        private List<OrmConfig> config;
        private bool cloud;

        public OrmHelper Cloud(bool support) {
            cloud = support;
            return this;
        }

        static public OrmConfig From(string tableName, bool frm = true) {
            var h = new OrmHelper { config = new List<OrmConfig> { new OrmConfig { TableName = tableName } } ,frm = frm};
            h.config[0].End = h;

            return h.config[0];
        }

        public OrmConfig SubFrom(string tableName)
        {
            OrmConfig c = new OrmConfig { TableName = tableName, End = this , 
                PKey = config[0].PKey}; //从表不用设置PKey， 用主表的
            this.config.Add(c);
            return c;
        }

        public OrmHelper() {
            config = new List<OrmConfig>();
            cloud = true;
        }

        private Dictionary<string, DBParam> obj2DbParams(OrmConfig c, string[] fields, params object[] objs ) {
            var result = new Dictionary<string, DBParam>();
            if (fields != null)
                fields.ToList().ForEach(f =>{
                    var m = (from i in c.matchSetting where i.Value == f select i);
                    string matchField = m.Count() == 0 ? f : m.First().Key;
                    if (!string.IsNullOrEmpty( matchField)){
                        object v = null;
                        foreach(var obj in objs){
                            if (v==null) {
                                var p = obj.GetType().GetProperty(matchField);
                                if (p !=null) {
                                    v =p.GetValue(obj, null);
                                    break;
                                }
                            }
                        }
                        result[f] = new DBParam { ParamValue = v };
                    }
                });
            return result;
        }

        class sqlItem { public string script; public Dictionary<string, DBParam> @params;};
        private List<sqlItem > saveSQL( ) {
            var sqls = new List<sqlItem > ();
            
            int i = 0;
            config.ForEach(c => {
                sqls.Add(new sqlItem{ script =  SQLHelper.From(c.TableName).PK(c.PKey).Delete(),  @params = obj2DbParams(c, config[0].PKey, config[0].Object)});
                if (i == 0)  {
                    var fields = c.DBFields();
                    sqls.Add(new sqlItem{ script =  SQLHelper.From(c.TableName).Fields(fields).Insert(), @params = obj2DbParams(c, fields, c.Object)});
                }
                else  {
                    if (c.Object != null)
                        foreach (object p in (c.Object as dynamic)) {//逐条插入 
                            var fields = c.DBFields(p);
                            fields = fields.Union(c.PKey).ToArray();
                            sqls.Add(new sqlItem{ script =  SQLHelper.From(c.TableName).Fields(fields).Insert(), @params = obj2DbParams(c, fields,  p, config[0].Object)});
                        }
                }
                i++;
            });
            return sqls;
        }

        public void Save(  ) {
            Saves(null);
        }

        public void Saves( Func<object, bool> beforeCommit) {
            using (var conn = Project.Current != null && cloud? (frm?Project.Current.GetFrmConn(): Project.Current.GetConn()): DBHelper.GetConnection(frm?0:1))   {
                var trans = DBHelper.GetTranscation(conn);
                try{
                    foreach (var i in saveSQL()) {
                        try {
                            DBHelper.ExecuteSql(conn, i.script, i.@params);
                        } catch (Exception e){
                            throw new Exception(string.Format("持久化框架配置发生错误.\n{0}\n{1}\n{2}",
                            e.Message, i.script, string.Join("\n", i.@params.Select(p=> p.Key + "=" + p.Value.ParamValue))));
                        }
                    };
                    if (beforeCommit != null && !beforeCommit(null/*config[0].Obj*/)) throw new Exception("持久化框架配置发生外部错误.");
                    DBHelper.CommitTranscation(trans);
                }
                catch {
                    DBHelper.RollbackTranscation(trans);
                     throw ;
                }
            }
        }

        public string UpdateSQL()
        {
            List<MetaColumn> fieldsMeta = new List<MetaColumn>();
            config.ForEach(p=> fieldsMeta = fieldsMeta.Union(SysTableMeta.SysTables.Find(pp=>pp.TableName == p.TableName).Columns).ToList()); 
            
            var sqls = saveSQL();
            for (int i = 0; i < sqls.Count; i++) 
                foreach (var j in sqls[i].@params){
                    var c = fieldsMeta.Find(p => p.ColumnName == j.Key) ?? new MetaColumn { Type = ColumnType.ctStr };
                    sqls[i].script = sqls[i].script.Replace(":" + j.Key,
                        c.Type == ColumnType.ctNumber ? Convert.ToInt32( j.Value.ParamValue).ToString():
                        (c.Type == ColumnType.ctBool ? (Convert.ToBoolean(j.Value.ParamValue) ? "1" : "0") : ("'" + j.Value.ParamValue.ToString().Replace("'", "''") + "'")));
                }
            return string.Join("\n", from i in sqls select  i.script);
        }

        private DataTable select(OrmConfig c, object obj)
        {
            using (var conn = Project.Current != null && cloud ? (frm ? Project.Current.GetFrmConn() : Project.Current.GetConn()) : DBHelper.GetConnection(frm ? 0 : 1))   
            {
                var pkFields = c.PKey;//.Select(p => c.matchSetting.ContainsKey(p) ? c.matchSetting[p] : p).ToArray();
                var pkValues = new Dictionary<string, DBParam>();
                if (c.PKey != null)
                    foreach(var f in c.PKey){
                        var m = (from i in c.matchSetting where i.Value == f select i);
                        string matchField = m.Count() == 0 ? f : m.First().Key;
                        //pkValues[matchField] = new DBParam { ParamValue = obj.GetType().GetProperty(f).GetValue(obj, null) };
                        pkValues[f] = new DBParam { ParamValue = obj.GetType().GetProperty(matchField).GetValue(obj, null) };
                    }
                return DBHelper.ExecuteQuery(conn, SQLHelper.From(c.TableName).PK(pkFields).Select(),  pkValues);
            }
        }

        private object dr2Obj(OrmConfig c, DataRow dr, object obj)
        {
            foreach (var i in obj.GetType().GetProperties())
            {
                string matchField = c.matchSetting.ContainsKey(i.Name) ? c.matchSetting[i.Name] : i.Name;
                if (!string.IsNullOrEmpty(matchField)) 
                    if (dr.Table.Columns.Contains(matchField))
                        i.SetValue(obj, dr[matchField] == DBNull.Value? null:dr[matchField], null);
            }
            return obj;
        }

        public bool Setup( )
        {
            object main = config[0].Object;
            DataTable dt = select(config[0], main);
            if (dt.Rows.Count == 0) return false;
            else{
                dr2Obj(config[0], dt.Rows[0], config[0].Object);

                foreach (var c in config.Where(p => p.Object != config[0].Object)) {
                    var t = c.Object.GetType();
                    //先删掉数组。这样可以多次 Setup。否则会越来越多
                    t.GetMethod("RemoveRange").Invoke(c.Object, new object[]{0, Convert.ToInt32( t.GetProperty("Count").GetValue(c.Object, null))  });
                    dt = select(c,main);
                    (from DataRow r in dt.Rows select r).ToList().ForEach(p =>{
                        var obj = c.Object.GetType().GetGenericArguments()[0].Assembly.CreateInstance(
                                c.Object.GetType().GetGenericArguments()[0].FullName);
                        c.Object.GetType().GetMethod("Add").Invoke(c.Object, new object[] {dr2Obj(c, p, obj)});
                        });
                }
                return true;
            }
        }

        //待优化
        public void BatchSetup(IEnumerable<object> obj) {
            foreach (var o in obj) {
                DataTable dt = select(config[0], o);
                if (dt.Rows.Count >0) dr2Obj(config[0], dt.Rows[0], o);
            }
        
        }


        //----------
        static public void CreateTemp(params object[] listProp) {
            foreach (var i in listProp) {
                var typ = i.GetType().GetGenericArguments();
                if ((typ != null)&&(typ.Count()>0)) {
                    var o = typ[0].Assembly.CreateInstance(typ[0].FullName);
                    i.GetType().GetMethod("Add").Invoke(i, new object[] { o });
                }
            }
        }
        static public void CreateTempProp(object obj)
        {
            if (obj != null)
            {
                var listProps = obj.GetType().GetProperties().Select(p => p.GetValue(obj, null)).Where(p => p != null).ToArray();
                CreateTemp(listProps);
            }
        }
    }
}
