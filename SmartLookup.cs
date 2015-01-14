using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace l.core
{
    public class SmartLookup
    {
        //private string searchField;
        public bool PickList { get; set; } // = true 代表SearchQuery 不会弹出，而是填充下拉框

        public bool Check { get; set; }
        public bool Main { get; set; }
        public string Table { get; set; }
        public string[] KeyFields { get; set; }
        public string[] LookupFields { get; set; }

        public string SearchQuery { get; set; }
        public string SearchScript { get; set; }
        public Dictionary<string, string> FieldsMatch { get; set; }
        public string Expression { get; set; }
        //public string SearchField { get { return searchField ?? ((KeyFields.Count() > 0)? KeyFields.Last():null); } set { searchField = value; } } //放按钮的字段
        public string LookupQuery { get; set; }
        //public MetaQuery PickListQuery { get; set; }
        private System.Data.DataSet pickListDataSet;
        public System.Data.DataSet PickListDataSet { get { return pickListDataSet; } }

        //1) 构造缺少的字段
        //2) 处理 PickList
        //  a) 数据字典的 SQL ，或者 StoreNO 等类型
        //  b) 通用单据也有不弹出，而是构造下拉的应用
        private string match(string field){
            return FieldsMatch == null? field: (FieldsMatch.ContainsKey(field)? FieldsMatch[field]: field);
        }
        private string reMatch(string field)
        {
            if (FieldsMatch != null)  {
                var fs = from i in FieldsMatch where i.Value == field select i.Key;
                if (fs.Count() > 0) return fs.First(); else return field;
            }
            else return field;
        }

        public SmartLookup Ready(DataTable table, IEnumerable< FieldMeta> fms) {

            if ((SearchQuery != null) && (KeyFields.Count() == 1) && (KeyFields[0].IndexOf("TypeID") > 0)) {
                PickList = true; //应用在 共通资料上的StdQuery 作为填充应用
            } 
            if (PickList) //填充
                using (var conn = Project.Current== null?  DBHelper.GetConnection(1):l.core.Project.Current.GetConn()) {
                    var mf = (from i in fms where i.FieldName == KeyFields[0] select i).First();
                    l.core.MetaQuery SearchListQuery =
                        (SearchQuery != null )? new l.core.Query(SearchQuery).Load()
                        : new MetaQuery { Scripts = new List<QueryScript> { new QueryScript { ScriptIdx = 0, Script = mf.Selection } } };
                    pickListDataSet = SearchListQuery.ExecuteQuery(conn, 0, 0, false);
                    
                    mf.Selection = "";
                    foreach (System.Data.DataRow dr in PickListDataSet.Tables[0].Rows)
                        if (PickListDataSet.Tables[0].Columns.Count == 1) mf.Selection += dr[0].ToString() + ";";
                        else mf.Selection += dr[0].ToString() + "." + dr[1].ToString();
                }
            
            //构造字段(lookup)
            if ((table != null) && (!String.IsNullOrEmpty(LookupQuery) && (LookupQuery!="DO"))) {
                using (var conn = Project.Current== null?  DBHelper.GetConnection(1):l.core.Project.Current.GetConn()) {
                    var q = new l.core.Query(LookupQuery).Load();
                    DataSet ds = q.Prepare(conn, false);
                    foreach (string s in LookupFields) {
                        var ss = match(s);
                        if (!table.Columns.Contains(s))
                            table.Columns.Add(s, ds.Tables[0].FindColumn(ss).DataType).ExtendedProperties["lookup"] = "true";
                    }
                }
            }

            //构造字段(expression)
            if ((table != null) && (!String.IsNullOrEmpty(Expression))) {
                foreach (string s in LookupFields) { 
                    if (!table.Columns.Contains(s))
                        table.Columns.Add(s, typeof(decimal)).ExtendedProperties["lookup"] = "true";
                }
            }
            return this;
        }

        private bool needBind(DataTable table) {
            return ((from string s in LookupFields where table.Columns[s].ExtendedProperties.ContainsKey("lookup") select 1).Count() > 0);
        }
        //就是立即关联
        public void BindData(DataTable table, Dictionary<string, object> sysParamValues, bool ignoreLastRow = true) {
            if ((table != null) && (!String.IsNullOrEmpty(LookupQuery) && (LookupQuery != "DO"))) {
                if (needBind(table)) {
                    using (var conn = Project.Current== null?  DBHelper.GetConnection(1):l.core.Project.Current.GetConn())  {
                        var q = new l.core.Query(LookupQuery).Load();
                        for (int i = 0; i < table.Rows.Count - (ignoreLastRow ? 1 : 0); i++) {
                            foreach (string s in KeyFields)
                                foreach (var par in q.Params){
                                    var localField = reMatch(par.ParamName);
                                    q.SmartParams.SetParamValue(par.ParamName, sysParamValues != null && sysParamValues.ContainsKey(par.ParamName) ? sysParamValues[par.ParamName] : (table.Columns.Contains(localField) ? table.Rows[i][localField] : (table.DataSet.Tables.Count > 1 && table.DataSet.Tables[0].Columns.Contains(localField) ? table.DataSet.Tables[0].Rows[0][localField] : null)));
                                }
                            //q.SmartParams.SetParamValue(s, table.Rows[i][s]);
                            DataSet ds = q.ExecuteDataObject(conn, false);
                            if (ds.Tables[0].Rows.Count > 0)
                                foreach (string ss in LookupFields)  {
                                    if (table.Columns[ss].ExtendedProperties.ContainsKey("lookup")) table.Rows[i][ss] = ds.Tables[0].Rows[0][match( ss)];
                                }
                        }
                    }
                }
            }

            /*if ((table != null) && (!String.IsNullOrEmpty(Expression))) {
                if (needBind(table)) {
                    var exp = new Expression(Expression);
                    for (int i = 0; i < table.Rows.Count - (ignoreLastRow ? 1 : 0); i++) {
                        foreach (string ss in LookupFields)
                            table.Rows[i][ss] = exp.Eval(KeyFields.ToDictionary(p=>p, q=> table.Rows[i][q]));
                    }
                }
            }*/
        }
        
        //一致性检查
        public bool CheckLookup(DataTable table) {
            if (!string.IsNullOrEmpty(LookupQuery)) { 
                var q = new l.core.Query(LookupQuery).Load();
                var fullParams = true;
                foreach (string s in KeyFields.Union(new []{"Operator", "OperName", "LocalStoreNO"})) {
                    if (q.Params.Find(p=>p.ParamName == s) != null){
                        var v = table.Rows[0][s];
                        if (v == DBNull.Value || v == null || Convert.ToString(v) == string.Empty) fullParams = false;
                        else q.SmartParams.SetParamValue(s, v);
                    }
                }
                if (fullParams) {
                    var ds1 = q.ExecuteDataObject();
                    return ds1.Tables[0].Rows.Count > 0;
                }
                else return true;
            }
            else return true;
        }

        //暂时提供的功能
        static public List<SmartLookup> GetLookupFromFieldMeta(IEnumerable< FieldMeta > fms){
            var r = new List<SmartLookup> ();
            foreach (var fm in fms.Where(p=>!string.IsNullOrEmpty(p.Selection))) {
                if (new System.Text.RegularExpressions.Regex("[Ss][Ee][Ll][Ee][Cc][Tt].*[Ff][Rr][Oo][Mm]\\s").IsMatch(fm.Selection)){
                    var ll = new SmartLookup
                    {
                        KeyFields = new[] { fm.FieldName },
                        LookupFields = new[] { fm.FieldName },
                        PickList = true,
                    }.Ready(null, fms);
                    r.Add(ll);
                }
                else if (new System.Text.RegularExpressions.Regex("^\\w+$").IsMatch(fm.Selection)) {
                    var ll = new SmartLookup{
                        KeyFields = new[] { fm.FieldName },
                        LookupFields = new[] { fm.FieldName },
                        SearchQuery = fm.Selection
                    };//.Ready(null, fms); 
                    r.Add(ll);
                    fm.Selection = "";
                }
            }
            return r;
        }
    }
}
