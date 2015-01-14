using System.Text.RegularExpressions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using RazorEngine;

namespace l.core
{
    public interface IQuery {
        ParamsHelper SmartParams {get;}
        string QueryName { get; set; }
        ITables ExecQuery(object conn = null, int startRecord = 0, int count = 10);
        ITables PrepareQuery(object conn );
        ITables ExecDataObject(object conn);
        List<QueryParam> Params { get; set; }
        FieldMetaHelper GetParamMeta(Dictionary<string, DBParam> _params = null);
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
            return OrmHelper.From("metaQuery").F("QueryName", "QueryType", "HashCode", "Version", "ConnAlias", "ChartSetting").MF("SysValues", null).PK("QueryName").Obj(this).End
                .SubFrom("metaQueryScript").Obj(Scripts).End
                .SubFrom("metaQueryChecks").F("CheckIdx","CheckType",  "CheckSummary", "ParamToValidate", "ParamToCompare", "CompareType", "Type", "CheckSQL", "CheckEnabled").
                    MF("Type", "ValidateType").Obj(Checks).End
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
        public void Remove() { getOrm().Dels(); }
        public string UpdateSQL()
        {
            return getOrm().UpdateSQL();
        }
        
        static public string[] GetErrors(DataSet ds) {
            return (from DataTable dt in ds.Tables where dt.Columns.Count == 1 && dt.Columns[0].ColumnName == "error" select dt.Columns[0].Caption).ToArray();
        }
        
        static public string GetErrors(DataTable dt)
        {
            return dt.Columns.Count == 1 && dt.Columns[0].ColumnName == "error" ? dt.Columns[0].Caption : null;
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
    public class CheckException: Exception {
        public CheckException(string message)
            : base(message)
        { 
            
        }
    }

    public class YReference {
        public string label { get; set; }
        public string value { get; set; }
    }


    public class ChartSettingY {
        public string field { get; set; }
        public string chartType { get; set; }
        public YReference[] reference { get; set; }
        public bool Percent { get; set; }
    }

    public class ChartSetting {
        public string x { get; set; }
        public string yGroup { get; set; }
        public string yGroupDisplay { get; set; }
        public ChartSettingY[] y { get; set; }
        public string xType { get; set; }
        public string caption { get; set; }

        public bool serverSum { get; set; }

    }

    public class ChartDataY {
        public string yLabel { get; set; }
        public Dictionary<object, double> Data2D { get; set; }
        public string ChartType { get; set; }
        public YReference[] reference { get; set; }
        public bool Percent { get; set; }
        public ChartDataY() {
            Data2D = new Dictionary<object, double>();
        }
    }

    public class ChartData
    {
        public object[] xRange { get; set; }
        public string xLabel { get; set; }
        public string xType { get; set; }
        public List<ChartDataY> YData { get; set; }        

        public ChartData()
        {
            YData = new List<ChartDataY>();
        }
        
        public string Caption { get; set; }
    }

    public class MetaQuery {
        private Dictionary<string, object> sysValues { get; set; }
        private ChartSetting chartSetting ;
        [Required]
        public string QueryName { get; set; }
        [Required]
        public int QueryType { get; set; }
        public string ConnAlias { get; set; }
        public string Version { get; set; }
        public List<QueryScript> Scripts { get; set; }
        public string ChartSetting {get;set;}
        
        //public List<QueryScriptMeta> ScriptsMeta { get; set; }
        public List<QueryParam> Params { get; set; }
        public List<QueryCheck> Checks { get; set; }

        public ChartSetting QueryChartSetting { get {
                if (chartSetting == null) {
                    if (ChartSetting != null && !string.IsNullOrEmpty(ChartSetting.Trim()))
                    {
                        try{
                            if (!string.IsNullOrEmpty(ChartSetting.Trim())) chartSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<ChartSetting>(ChartSetting);
                            else chartSetting = new ChartSetting();
                        }
                        catch (Exception e) { chartSetting = null; throw new Exception(string.Format("查询 \"{0}\" 获取图表设置格式出错.\n", QueryName) + e.Message); }

                    }else chartSetting =  new ChartSetting();
                }
                return chartSetting;
            }
        }

        public Dictionary<string, object> SysValues { get { return sysValues; } set { 
            sysValues = value ;
            foreach (var i in Params) { 
                if (i.DefaultValue != null && i.DefaultValue.IndexOf("@") == 0 && value.ContainsKey(i.DefaultValue.Substring(1))) 
                    i.DefaultValue = value[i.DefaultValue.Substring(1)].ToString();
            }
        } }

        protected bool MetaChanged;
        protected ParamsHelper SmartParams;

        public MetaQuery() { 
            Scripts = new List<QueryScript>();
            //ScriptsMeta = new List<QueryScriptMeta>();
            Params = new List<QueryParam>();
            Checks= new List<QueryCheck>();
            SmartParams = new ParamsHelper() ;
        }
        public string ParamNamePrefixHandle(string sql)
        {
            return SmartParams.ParamNamePrefixHandle(Params, sql);
        }

        public FieldMetaHelper GetParamMeta()
        {
            return GetParamMeta(null);
        }

        public FieldMetaHelper GetParamMeta(Dictionary<string, DBParam> _params ){
            var mfs = new FieldMetaHelper().Ready(Params.Select(p => p.ParamName), QueryName + "_p");
            Params.ForEach(p =>   {
                mfs.EditorTypeFromColumnType(p.ParamName, p.ParamType);
                mfs.Get(p.ParamName).CheckSQLList(true, _params);
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

        private DataTable executeQuery(IDbConnection conn, QueryScript item, Dictionary<string, DBParam> @params, int p1, int p2, bool allowSqlError, bool sqlrazor){
            var t1 = DateTime.Now;
            try{
                string tsql = SmartParams.ParamNamePrefixHandle(Params, item.Script);
                if (sqlrazor){
                    tsql = l.core.SmartScript.Eval(tsql,  @params.ToDictionary(p => p.Key, q => Convert.ToString(q.Value.ParamValue.ToString())) );
                }

                BizResult r = new BizResult ();
                r.Errors =  Validate(null).ToList();
                if (!r.IsValid) throw new CheckException(string.Join("\n", r.Errors.Select(p=>p.ErrorMessage)));

                var dt = DBHelper.ExecuteQuery(conn, tsql, @params, p1, p2);

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
            catch (CheckException e)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("error").Caption = e.Message;
                return dt;
            }
            finally{
                var t2 = DateTime.Now;
                var t3 = t2 - t1;
                if (l.core.VersionHelper.Helper != null && l.core.VersionHelper.Helper.Action.IndexOf("expim") >= 0)
                    if (!string.IsNullOrEmpty(QueryName))
                        l.core.VersionHelper.Helper.InvokeRec<MetaQuery>(this, "MetaQuery", new[] { "QueryName" }, SmartParams.ToString(), t3.Milliseconds);
            }
        }

        private DataSet Execute(IDbConnection conn , int queryType, int startRecord, int count, bool allowSqlError) {
            var connection = conn == null ? Project.Current ==null? DBHelper.GetConnection(1): Project.Current.GetConn((ConnAlias??"").Trim() == ""? null: ConnAlias) : null;
            try {
                DataSet ds = new DataSet();
                int i = 0;
                Scripts.ForEach(p => {
                    bool parialQuery = (queryType == 0) && (i == 0);//数据对象不分页
                    var dt = executeQuery(conn ?? connection, p,
                        SmartParams.GetQueryDBParams(queryType == 0 ? Params /*ParamsWithGroup()*/ : Params),
                        (parialQuery ? startRecord : 0), (parialQuery ? count : 0), allowSqlError, p.ScriptType == "2");
                
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
                        foreach (KeyValuePair<string, object> e in dv){
                            var value = Convert.ToString(e.Value).IndexOf('@') == 0 && SysValues != null && SysValues.ContainsKey(Convert.ToString(e.Value).Substring(1)) ? SysValues[Convert.ToString(e.Value).Substring(1)] : e.Value;
                            if (ds.Tables[0].Columns.Contains(e.Key))
                                if (ds.Tables[0].Columns[e.Key].DataType == typeof(DateTime))
                                    try{
                                        ds.Tables[0].Columns[e.Key].ExtendedProperties["DefaultValue"] = DateTime.Now.AddDays(Convert.ToInt32(value));
                                    }catch {
                                        ds.Tables[0].Columns[e.Key].ExtendedProperties["DefaultValue"] = DateTime.Parse(value.ToString());
                                    }
                                else ds.Tables[0].Columns[e.Key].ExtendedProperties["DefaultValue"] = value;
                        }
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
                //暂不检查
                /*var l = new Dictionary<string, int>();
                foreach (DataTable dt in ds.Tables) {
                    foreach (DataColumn dc in dt.Columns) {
                        if (l.ContainsKey(dc.ColumnName)) l[dc.ColumnName] = l[dc.ColumnName] + 1;
                        else l[dc.ColumnName] = 1;
                    }
                }
                var c = l.Where(p=>p.Value > 1);
                if (c.Count() > 0) 
                    throw new Exception(string.Format("数据对象\"{0}\"存在重复字段\"{1}\".", QueryName, string.Join(",", c.Select(p=>p.Key))));*/
                return ds;
            }
        }

        public IEnumerable<BizValidationResult> Validate(ValidationContext validationContext)
        {
            var pv = SmartParams.GetDBParams(Params);
            foreach (var c in Checks.OrderBy(p => p.CheckIdx).Where(p => p.CheckEnabled))
            {
                var p2v = Params.Find(p => p.ParamName == c.ParamToValidate);
                if (p2v == null) throw new CheckException(string.Format("Query \"{0}\" 的检查 \"{1}\" 参数是 \"{2}\"，但并未定义.", QueryName, c.CheckSummary, c.ParamToValidate));
                var paramsName = (from Match m in new Regex(":([a-zA-z_]+)").Matches(c.CheckSQL ?? "") select m.Value.Substring(1))
                    .Union(new[] { c.ParamToValidate });
                if (!string.IsNullOrEmpty(c.ParamToCompare)) paramsName = paramsName.Union(new[] { c.ParamToCompare });
                object errorMessageEx = null;
                if (!c.Validate(pv, this, "query \"" + QueryName + "\"", out errorMessageEx)) yield return new BizValidationResult(c.CheckSummary, new[] { c.ParamToValidate }, c.CheckType == CheckType.etWarning);
            }
        }

        public ChartData GetChartData(DataTable table)
        {
            try {
                return _getChartData(table);
            }
            catch (Exception e) {
                throw new Exception(e.Message + "\n" + e.StackTrace);
            }
        }
        public ChartData _getChartData(DataTable table) {
            var q  = this;
            var chartSetting = q.QueryChartSetting;
            var chartData = new ChartData() { YData =  new List<ChartDataY>(), Caption = chartSetting.caption,  xType = chartSetting.xType};
            chartData.xRange = string.IsNullOrEmpty(chartSetting.x)?new []{""}:
                (from System.Data.DataRow dr in table.Rows select dr[chartSetting.x].ToString()).ToArray();
            if (chartSetting.serverSum){
                var groups = string.IsNullOrEmpty(chartSetting.yGroup) ? new[] { "" } : 
                    (from System.Data.DataRow dr in table.Rows select dr[chartSetting.yGroup].ToString()).Distinct();

                foreach(var g in groups){
                    foreach(var i in chartSetting.y){
                        var chartDatay = new ChartDataY();
                        chartDatay.reference = i.reference;
                        chartDatay.Data2D = string.IsNullOrEmpty(chartSetting.x)?
                            new Dictionary<object, Double> {{"", 
                                (from System.Data.DataRow dr in table.Rows 
                                    where (string.IsNullOrEmpty (g) || dr[chartSetting.yGroup].ToString() == g)                                select dr).Sum(p=>Convert.ToDouble(p[i.field]))}}
                            :
                                (from System.Data.DataRow dr in table.Rows
                                    where (string.IsNullOrEmpty (g) || dr[chartSetting.yGroup].ToString() == g)
                                    group dr by dr[chartSetting.x] into grouped
                                    select new { x = grouped.Key, y = grouped.Sum(p => Convert.ToDouble(p[i.field])) }).ToDictionary(p => p.x, q1 => q1.y);
                        chartDatay.yLabel = table.Columns[i.field].Caption + (string.IsNullOrEmpty(g)?"":"(" + g + ")");
                        chartDatay.ChartType = i.chartType;

                        chartData.YData.Add(chartDatay);
                    }
                }
            }
            else {
                var ygroups = string.IsNullOrEmpty(chartSetting.yGroup) ? null : chartSetting.yGroup.Split(';');
                
                foreach(var i in chartSetting.y){
                    if (ygroups == null){
                         
                        chartData.YData = chartData.YData.Union(
                                new List<ChartDataY> {
                                new ChartDataY
                                     {
                                         ChartType = i.chartType,
                                         reference = i.reference,
                                         Percent = i.Percent,
                                         yLabel = table.Columns[i.field].Caption,
                                         Data2D =(from System.Data.DataRow dr in table.Rows
                                           select dr).ToDictionary(p => chartSetting.x == null? "default":p[chartSetting.x], q1 => Convert.ToDouble( q1[i.field]))
                                     }
                            }
                        ).ToList();
                    }
                    else {
                        chartData.YData = chartData.YData.Union(
                            (from System.Data.DataRow dr in table.Rows
                                               group dr by string.Format(chartSetting.yGroupDisplay, ygroups.Select(p => dr[p].ToString()).Union(new[] { table.Columns[i.field].Caption }).ToArray()) into grouped
                                               select grouped.Key).Select(
                                yg =>
                                     new ChartDataY
                                     {
                                         ChartType = i.chartType,
                                         yLabel = yg,
                                         Percent = i.Percent,
                                         reference = i.reference,
                                         Data2D = (from System.Data.DataRow dr in table.Rows
                                                   where string.Format(chartSetting.yGroupDisplay, ygroups.Select(p => dr[p].ToString()).Union(new[] { table.Columns[i.field].Caption }).ToArray()) == yg
                                                   group dr by dr[chartSetting.x] into grouped

                                                   select new { x = grouped.Key, y = grouped.Sum(p => Convert.ToDouble(p[i.field])) })
                                                .ToDictionary(p => p.x, q1 => q1.y)
                                     }

                            )
                        ).ToList();
                    }
                }
               

            }
            return chartData;
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

    public class QueryCheck : Check { 
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
                    ) : (ParamType == ColumnType.ctBool ? DefaultValue == "1" || DefaultValue == "true" ||DefaultValue == "True" ?true:false as object : DefaultValue));
        }
    }

}
