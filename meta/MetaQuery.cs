using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace l.core
{
    public interface IQuery {
        ParamsHelper SmartParams {get;}
        string QueryName { get; set; }
        ITables ExecQuery(object conn = null, int startRecord = 0, int count = 10);
        ITables PrepareQuery(object conn );
        ITables ExecDataObject(object conn);
        List<QueryParam> Params { get; set; }
        FieldMetaHelper GetParamMeta();
        string ParamsAsString(l.core.FieldMetaHelper fieldMeta);
        List<string> Groups();
    }

    //通用模型加上ORM 设定
    public class Query : MetaQuery, IQuery {
        public string HashCode { get; set; }
        new public ParamsHelper SmartParams { get { return base.SmartParams; } }
        public Query(string queryName) {
            this.QueryName = queryName;
            Scripts = new List<QueryScript>();
            Params = new List<QueryParam>();

            //不能加这句，否则反序列的时候也会去load db,更新的时候就死循环
            //if (!string.IsNullOrEmpty(queryName)) Load();
        }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaQuery").F("QueryName", "QueryType", "HashCode", "Version", "ConnAlias").PK("QueryName").Obj(this).End
                .SubFrom("metaQueryScript").Obj(Scripts).MF("ScriptType", null).End
               // .SubFrom("metaQueryScript").Obj(ScriptsMeta).F("ScriptsMeta").End
                .SubFrom("metaQueryParams").Obj(Params).MF("Default", null).End;
        }

        public Query Load() {
            var loaded = getOrm().Setup();

            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<Query>(this, "MetaQuery", new[] { "QueryName" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Query \"{0}\" does not exist.", QueryName));
            else if (Scripts.Count == 0) throw new Exception(string.Format("Query \"{0}\" does not include any statements.", QueryName));
            //checkHashCode();
            SmartParams.SetParams(Params);

            if (Params.Find(p => !string.IsNullOrEmpty(p.ParamGroups)) != null)
                if (Params.Find(p => p.ParamName == "ParamGroup") == null) 
                    Params.Add(new QueryParam { ParamName = "ParamGroup" });
            return this;
        }

        public string UpdateSQL()
        {
            return getOrm().UpdateSQL();
        }
        
        static public string[] GetErrors(DataSet ds) {
            return (from DataTable dt in ds.Tables where dt.Columns.Count == 1 && dt.Columns[0].ColumnName == "error" select dt.Columns[0].Caption).ToArray();
        }

        override protected void SaveMeta() {
            if (MetaChanged )
                using (var conn = Project.Current == null ? DBHelper.GetConnection(0) : Project.Current.GetFrmConn()) {
                    Scripts.ForEach(p=>
                        DBHelper.ExecuteSql(conn, "update metaQueryScript set MetaColumn = :MetaColumn where QueryName = :QueryName and ScriptIdx = :ScriptIdx", new Dictionary<string, DBParam> { 
                            {"MetaColumn", new DBParam{ ParamValue = p.MetaColumn}},
                            {"QueryName", new DBParam{ ParamValue =QueryName}},
                            {"ScriptIdx", new DBParam{ ParamValue =p.ScriptIdx}}
                        })
                    );
                }
        }

        public void Save()
        {

            getOrm().Save();
        }
    }

    //下面是通用模型
    public class MetaQuery
    {
        [Required]
        public string QueryName { get; set; }
        [Required]
        public int QueryType { get; set; }
        public string ConnAlias { get; set; }
        public string Version { get; set; }
        public List<QueryScript> Scripts { get; set; }
        //public List<QueryScriptMeta> ScriptsMeta { get; set; }
        public List<QueryParam> Params { get; set; }

        protected bool MetaChanged;
        protected ParamsHelper SmartParams;

        public MetaQuery() { 
            Scripts = new List<QueryScript>();
            //ScriptsMeta = new List<QueryScriptMeta>();
            Params = new List<QueryParam>();
            SmartParams = new ParamsHelper() ;
        }

        public FieldMetaHelper GetParamMeta(){
            var mfs = new FieldMetaHelper().Ready(Params.Select(p => p.ParamName), QueryName + "_p");
            Params.ForEach(p =>   {
                mfs.EditorTypeFromColumnType(p.ParamName, p.ParamType);
                mfs.Get(p.ParamName).CheckSQLList(true);
            });
            return mfs;
        }

        //所有的分组
        public List<string> Groups() {
            List<string> groups = groups = new List<string>(); 
            foreach (var s in Params) 
                if (!string.IsNullOrEmpty(s.ParamGroups))
                    foreach (string ss in s.ParamGroups.Split(';')) 
                        if ((groups.IndexOf(ss) == -1) && (!string.IsNullOrEmpty(ss)))
                            groups.Add(ss);
            return groups.Count == 0 ? null : groups;
        }

        private List<QueryParam> ParamsWithGroup(){
            return Params.Union(new []{new QueryParam{ ParamName = "ParamGroup"}}).ToList();
        }

        public string ParamsAsString(l.core.FieldMetaHelper  fieldMeta) {
            return fieldMeta == null? "fieldMeta 未设置": string.Join(";", SmartParams.GetQueryDBParams(Params).Select(p => fieldMeta.Get(p.Key).DisplayLabel + "=" + p.Value.ParamValue));
        }

        virtual protected void SaveMeta() {
        }

        private DataTable executeQuery(IDbConnection conn, QueryScript item, Dictionary<string, DBParam> @params, int p1, int p2, bool allowSqlError){
            var t1 = DateTime.Now;
            try{
                var dt = DBHelper.ExecuteQuery(conn, SmartParams.ParamNamePrefixHandle(Params, item.Script), @params, p1, p2);

                string meta = Newtonsoft.Json.JsonConvert.SerializeObject(
                        from System.Data.DataColumn c in dt.Columns select new { n = c.ColumnName, t = c.DataType.ToString() });
                if (item.MetaColumn != meta) {
                    MetaChanged = true;
                    item.MetaColumn = meta;
                }
                return dt;

            } //处理  sql 里面的 raiserror 信息
            catch (System.Data.SqlClient.SqlException e) {
                if (allowSqlError){
                    DataTable dt = new DataTable();
                    dt.Columns.Add("error").Caption = e.Message;
                    return dt;
                } else throw new Exception(string.Format("执行查询 \"{0}\" 发生sql 错误.\n{1}", QueryName, e.Message));
            }
            finally{
                var t2 = DateTime.Now;
                var t3 = t2 - t1;
                if (l.core.VersionHelper.Helper != null && l.core.VersionHelper.Helper.Action.IndexOf("expim") >= 0)
                    if (!string.IsNullOrEmpty(QueryName))
                        l.core.VersionHelper.Helper.InvokeRec<MetaQuery>(this, "MetaQuery", new[] { "QueryName" }, t3.Milliseconds);
            }
        }

        private DataSet Execute(IDbConnection conn , int queryType, int startRecord, int count, bool allowSqlError) {
            var connection = conn == null ? Project.Current ==null? DBHelper.GetConnection(1): Project.Current.GetConn(string.IsNullOrEmpty( ConnAlias)? null: ConnAlias) : null;
            try {
                DataSet ds = new DataSet();
                int i = 0;
                Scripts.ForEach(p => {
                    bool parialQuery = (queryType == 0) && (i == 0);//数据对象不分页
                    var dt = executeQuery(conn ?? connection, p,
                        SmartParams.GetQueryDBParams(queryType == 0 ? Params /*ParamsWithGroup()*/ : Params),
                        (parialQuery ? startRecord : 0), (parialQuery ? count : 0), allowSqlError);
                
                    dt.TableName = QueryName + (i == 0 ? "" : "." + i.ToString());
                    ds.Tables.Add(dt);
                   
                    i++;
                });
                SaveMeta();
                return ds;
            }
            finally {
                if (conn == null) connection.Dispose();
            }
        }

        public DataSet ExecuteQuery(IDbConnection conn = null, int startRecord = 0, int count = 10, bool allowSqlError = true) {
            if (QueryType != 0) throw new Exception(string.Format("查询类型不匹配，\"{0}\"不是标准查询.", QueryName));
            //else if (Scripts.Count() > 2) throw new Exception(string.Format("标准查询\"{0}\"最多允许2条语句（包括汇总）.", QueryName));
            var ds = Execute(conn, 0, startRecord, count, allowSqlError);
            //if (ds.Tables.Count == 2)
            //    foreach(DataColumn dc in ds.Tables[1].Columns){
            //        var columnName = dc.ColumnName;
            //        if (columnName != "RecordCount")
            //            if ((from DataColumn dc1 in ds.Tables[0].Columns where dc1.ColumnName == columnName select 1).Count() == 0)
            //                throw new Exception(string.Format("标准查询\"{0}\"的汇总语句字段{1}不存在于查询结果字段", QueryName, columnName));
            //    }
            return ds;
        }

        public DataSet Prepare(IDbConnection conn = null, bool allowSqlError = true, bool forceRefreshMeta = false){
            DataSet ds =  new DataSet();
            int j = 0;

            if (forceRefreshMeta || Scripts.Where(p => string.IsNullOrEmpty(p.MetaColumn)).Count() > 0)  {
                ds = Execute(conn, 0, 0, 1, allowSqlError);
                if (ds.Tables.Count > 0) if (ds.Tables[0].Rows.Count == 1) ds.Tables[0].Rows.RemoveAt(0);
            }
            else foreach(var p in Scripts){
                    DataTable dt = new DataTable();
                    foreach (var i in Newtonsoft.Json.JsonConvert.DeserializeObject<List<QueryScriptMeta>>(p.MetaColumn)??new List<QueryScriptMeta>()) {
                        dt.Columns.Add(i.n).DataType = Type.GetType(i.t);
                    }
                    dt.TableName = QueryName + (j == 0 ? "" : "." + j.ToString());
                    ds.Tables.Add(dt);
                j++;
            }

            j = 0;
            foreach (var p in Scripts){
                if (!string.IsNullOrEmpty(p.DefaultValues)) {
                    var dv = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(p.DefaultValues) as Dictionary<string, object>;
                    if (dv != null)
                        foreach (KeyValuePair<string, object> e in dv)
                            if (ds.Tables[0].Columns.Contains(e.Key))
                                if (ds.Tables[0].Columns[e.Key].DataType == typeof(DateTime))
                                    try{
                                        ds.Tables[0].Columns[e.Key].ExtendedProperties["DefaultValue"] = DateTime.Now.AddDays(Convert.ToInt32(e.Value));
                                    }catch {
                                        ds.Tables[0].Columns[e.Key].ExtendedProperties["DefaultValue"] = DateTime.Parse(e.Value.ToString());
                                    }
                                else ds.Tables[0].Columns[e.Key].ExtendedProperties["DefaultValue"] = e.Value;
                }
                j++;
                break; //从表不做
            }
            return ds;
        }

        public DataSet ExecuteDataObject(IDbConnection conn = null, bool allowSqlError = true){
            if (QueryType != 1) throw new Exception(string.Format("查询类型不匹配，\"{0}\"不是数据对象.", QueryName));
            var ds= Execute(conn, 1, 0, 2, allowSqlError); //最多返回2行以判断错误
            if (ds.Tables[0].Rows.Count > 1) //!= 1)
                throw new Exception(string.Format("数据对象\"{0}\"返回的行数是\"{1}\".", QueryName, ds.Tables[0].Rows.Count == 0 ? "0" : "大于1"));
            else  {
                var l = new Dictionary<string, int>();
                foreach (DataTable dt in ds.Tables) {
                    foreach (DataColumn dc in dt.Columns) {
                        if (l.ContainsKey(dc.ColumnName)) l[dc.ColumnName] = l[dc.ColumnName] + 1;
                        else l[dc.ColumnName] = 1;
                    }
                }
                var c = l.Where(p=>p.Value > 1);
                if (c.Count() > 0) 
                    throw new Exception(string.Format("数据对象\"{0}\"存在重复字段\"{1}\".", QueryName, string.Join(",", c.Select(p=>p.Key))));
                return ds;
            }
        }

        //下面3个方法为了NoSQL 和SQL 的统一接口
        public ITables ExecQuery(object conn = null, int startRecord = 0, int count = 10){
            return ExecuteQuery(conn as IDbConnection, startRecord, count).Tables();}
        public ITables PrepareQuery(object conn = null) { return Prepare().Tables(); }
        public ITables ExecDataObject(object conn = null) { return ExecuteDataObject(conn as IDbConnection).Tables(); }
    }

    public class QueryScript
    {
        public int ScriptIdx { get; set; }
        public string ScriptType { get; set; }
        public string Script { get; set; }
        public string MetaColumn { get; set; }
        public string DefaultValues { get; set; }
        //public bool Succeed { get; set; }
    }

    public class QueryScriptMeta
    {
        public string n { get; set; }
        public string t { get; set; }
        public DataType type() {
            return DataType.Text;
        }
    }

    public class QueryParam  : IParam {
        [Required]
        public string ParamName { get; set; }
        public ColumnType ParamType { get; set; }
        public int ParamIdx { get; set; }
        public string ParamGroups { get; set; }
        public bool LikeLeft { get; set; }
        public bool LikeRight { get; set; }
        public string IsNull { get; set; }
        public string DefaultValue { get; set; }

        public object Default() { 
            return DefaultValue ==null?
                null:
                (ParamType == ColumnType.ctDateTime ? 
                    (DefaultValue.ToString().Trim() == ""?
                    null:
                    DateTime.Now.AddDays(Convert.ToInt32(DefaultValue)) as object 
                    ): DefaultValue);
        }
    }

}
