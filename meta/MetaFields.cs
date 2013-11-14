using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace l.core
{
    //管理一批
    public class FieldMetaHelper {
        private List<FieldMeta> fieldsmeta;
        private int pointer;
        public List<FieldMeta> All { get { return fieldsmeta; } }

        public FieldMetaHelper() {
            pointer = -1;
            fieldsmeta = new List<FieldMeta>();
        }

        public FieldMeta Get(string fieldName) {
            var r = from FieldMeta f in fieldsmeta where f.FieldName == fieldName select f;
            return r.Count() > 0 ? r.First() : new FieldMeta { FieldName = fieldName, DisplayLabel = fieldName, EditorType = ""};
        }

        public FieldMetaHelper Ready(DataTable dt, string context = "") {
            IEnumerable<string> fields = (from DataColumn dc in dt.Columns 
                                            //防止重复ready
                                            where fieldsmeta.Find(p=>p.FieldName == dc.ColumnName) == null 
                                          select dc.ColumnName );

            Ready(fields, context);
            foreach(var f in fieldsmeta) 
                if (dt.Columns.Contains(f.FieldName)){
                    dt.Columns[f.FieldName].Caption = (string.IsNullOrEmpty(f.DisplayLabel)?f.FieldName:f.DisplayLabel ).Trim();
                    if (string.IsNullOrEmpty(f.EditorType))
                        if (dt.Columns[f.FieldName].DataType == typeof(bool)) f.EditorType = "Boolean";
                        else if (dt.Columns[f.FieldName].DataType == typeof(DateTime)) f.EditorType = "DateTime";
                }

            return this;
        }

        public FieldMetaHelper Ready(DataSet ds, string context = "") {
            foreach(DataTable dt in ds.Tables) Ready(dt, context);
            return this;
        }

        public FieldMetaHelper Ready(ITables ds, string context = ""){
            foreach (ITable dt in ds) Ready(dt.Keys, context);
            return this;
        }

        public FieldMetaHelper Ready(IEnumerable<string> fields, string context ="") {
            fieldsmeta = fieldsmeta.Union(FieldMeta.Get(null, context, fields.ToArray())).ToList();
            return this;
        }

        //为只取一个字段的调用提供便利
        public FieldMeta One(string fieldName, string context =""){
            fieldsmeta = FieldMeta.Get(null, context, new []{fieldName}).ToList();
            return Get(fieldName);
        }
        
        public void EditorTypeFromColumnType(string fieldName, ColumnType columnType){
            var r = from FieldMeta f in fieldsmeta where f.FieldName == fieldName select f;
            if (r.Count() > 0) if (string.IsNullOrEmpty(r.First().EditorType)) r.First().EditorType = columnType.ToString().Substring(2);
        }

        public FieldMetaHelper Ready(FieldMeta mf) {
            if (pointer >= 0) {
                fieldsmeta.Insert(pointer, mf);
                pointer++;
            }
            else fieldsmeta.Add(mf);
            return this;
        }

        public FieldMetaHelper Ready(IEnumerable< FieldMeta> mfs) {
            foreach(var mf in mfs) Ready(mf);
            return this;
        }

        public void SetPointer(FieldMeta mf) {
            pointer = All.IndexOf(mf);
        }

        public FieldMeta NameField { get {
            var l = (from i in All where i.IsName select i);
            if (l.Count() > 0) return l.First();
            else return null;// throw new Exception("没有定义 [Name] 字段.");
        }}

        public FieldMeta IDField { get {
            var l = (from i in All where i.IDInfo != null && !i.IDInfo.Internal && i.IDInfo.IsKey select i);
            if (l.Count() > 0) return l.First();
            else {
                var ll =  (from i in All where i.IDInfo != null && i.IDInfo.Internal select i);
                if (ll.Count() > 0) return ll.First();  
                else return null;// throw new Exception("没有定义 [Name] 字段.");
            }
        }}

        public FieldMeta ParentField { get {
            var l = (from i in All where !string.IsNullOrEmpty(i.ParentOf) select i);
            if (l.Count() > 0) return l.First();
            else return null;//  throw new Exception("没有定义 [TreeParent] 字段.");
        }}

        public FieldMeta StatusField { get {
            var l = (from i in All where i.StatusDesc != null select i);
            if (l.Count() > 0) return l.First();
            else return null;//  throw new Exception("没有定义状态字段.");
        }}
    }

    //通用模型加上ORM 设定
    public class FieldMeta : MetaField {
        public string HashCode { get; set; }

        static private OrmHelper getOrm(Object obj) {
            return OrmHelper.From("metaColumn").PK("ColumnName", "At").MF("DropDownList", null)
                .MF("UIType", null).MF("FieldName", "ColumnName").MF("Context", "At").MF("DisplayLabel", "Caption").
                MF("Caption", null).MF("Regex", "Format").
                MF("Summary", null).MF("QueryInfo", null).
                MF("LookupInfo", null).MF("IDInfo", null).
                MF("IsArray", null).MF("IsDictionary", null).
                MF("Browse", null).MF("ParentOf", null).
                MF("IsName", null).MF("DataType", null).
                MF("ReadOnlyStatus", null).MF("Number", null).
                MF("SubData", null).MF("Model", null).
                Obj(obj).End;
        }

        public string UpdateSQL()
        {
            return getOrm(this).UpdateSQL();
        }


        public void Save(){
            FieldMeta.getOrm(this).Save();
        }

        #region helper 方法
        static public FieldMeta[] Get (FieldMeta[] fieldsMeta, string context, params string[] fields){
            var result = fields.Select(p => new FieldMeta { Context = context, FieldName = p, CharLength=-1001 }).ToArray();
            if (VersionHelper.Helper != null){ //不批量
                foreach (FieldMeta fm in result) {
                    getOrm(fm).Setup();
                    if (!VersionHelper.Helper.CheckNewAs<FieldMeta>(fm, "MetaField", new []{"FieldName", "Context"}, true)) getOrm(fm).Setup();
                    if (fm.CharLength == -1001) {
                        fm.Context = "";
                        getOrm(fm).Setup();
                        if (!VersionHelper.Helper.CheckNewAs<FieldMeta>(fm, "MetaField", new[] { "FieldName", "Context" }, true)) //不管是否需要更新都要重新 Setup,因为情景变了
                            getOrm(fm).Setup();
                    }
                } 
                result = result.Where(p => p.CharLength != -1001).ToArray();
            }
            else {
                getOrm(null).BatchSetup(result); //批量
                var r = (from i in result where i.CharLength == -1001 select new FieldMeta { Context = "", FieldName = i.FieldName,CharLength=-1001 }).ToArray();
                getOrm(null).BatchSetup(r); //批量
                result = result.Where(p => p.CharLength != -1001).Union(r.Where(p => p.CharLength != -1001)).ToArray();
            }

            return (fieldsMeta == null?result: fieldsMeta.Union(result )).ToArray();
        }
        #endregion

    }

    public class MetaFieldQuery {
        public bool PartialMatch { get; set; }
        public bool Range { get; set; }
        public bool Sort { get; set; }
    }

    public class MetaFieldID {
        public string Key { get; set; }
        public short Length { get; set; }
        public char FillChar { get; set; }
        public string StartWord { get; set; }
        public short Step { get; set; }
        public bool Internal { get; set; }
        public bool IsKey { get; set; }
    }


    public class MetaFieldLookup {
        public bool IsLookupField { get; set; }
        public bool Main { get; set; } 

        public string Class { get; set; }
        public string TableName { get; set; }
        public string[] LookupFields { get; set; }
        public string IdField { get; set; }
        public bool PickList { get; set; }
        public object Custom { get; set; }
    }
    public class SubData {
        public string SubClass { get; set; }
    }
    public class NumberInfo {
        public short Precision { get; set; }
        public bool Money { get; set; }
    }
    public enum SysModelType { smUpdateTime, smOperator, smState, smUnknown }
    public class ModelInfo : System.Attribute
    {
        public string Model { get; set; }
        public SysModelType SysType { get; set; }
    }

    //下面是通用模型
    public class MetaField
    {
        [Required]
        public string FieldName { get; set; }
        public string Context { get; set; }
        [Required]
        public string DisplayLabel { get; set; }
        //public string Caption { get { return string.IsNullOrEmpty(DisplayLabel) ? string.Format("[{0}]", FieldName) : DisplayLabel; } }
        public string Regex { get; set; }
        public string Inherited { get; set; }
        public int CharLength { get; set; }
        public string Selection { get; set; }
        public string EditorType { get; set; }
        public string DisplayFormat { get; set; }
        public string DicNO { get; set; }
        public string Version { get; set; }
        public string HashCode { get; set; }

        #region shop 用的元数据
        public bool Summary { get; set; }
        public MetaFieldQuery QueryInfo { get; set; }
        public MetaFieldLookup LookupInfo { get; set; }
        public MetaFieldID IDInfo { get; set; }
        public bool IsArray { get; set; }
        public bool IsDictionary { get; set; }
        public bool Browse { get; set; }
        public string ParentOf { get; set; }
        public bool IsName { get; set; }
        public Type DataType { get; set; }
        public Dictionary<string, string> StatusDesc;
        public string ReadOnlyStatus { get; set; }
        public NumberInfo Number { get; set; }
        public SubData SubData { get; set; }
        public ModelInfo Model { get; set; }
        #endregion

        private Dictionary<string, string> list;
        public Dictionary<string, string> DropDownList { get {
            if (list == null) {
                list = string.IsNullOrEmpty(Selection) ||
                        !new System.Text.RegularExpressions.Regex("(\\w+;)+").IsMatch(Selection) 
                ? new Dictionary<string, string>() : Selection.Split(';').ToDictionary(
                    p => string.IsNullOrEmpty(p) ? "" : p.Split('.')[0],
                    q => string.IsNullOrEmpty(q) ? "" : q.Split('.').Count() == 1 ? q : q.Substring(q.IndexOf('.') + 1)
                );
                if (!string.IsNullOrEmpty(DicNO))  {
                    var dic = new Dic(DicNO).Load();
                    dic.Dtl.ForEach(p => list[p.DicDtlNO + "." + p.DicDtlValue] = p.DicDtlValue);
                }
            }
            return list;
        }
            set { list = value; }
        }

        public Dictionary<string, string> List() { return DropDownList; }
        public object Format(object value) {
            if (!string.IsNullOrEmpty(DisplayFormat))
                return string.Format("{0:" + DisplayFormat + "}", value);
            else return value;
        }
        public Query CheckSQLList (bool fill){
            Query q = null;
            if (Selection == null) return q;
            if (new System.Text.RegularExpressions.Regex("[Ss][Ee][Ll][Ee][Cc][Tt].*[Ff][Rr][Oo][Mm]\\s").IsMatch(Selection))
            {
                q = new Query( "QueryDic" + FieldName + Context);
                q.Scripts.Add(new  QueryScript{ ScriptIdx = 1, ScriptType = "SQL", Script = Selection} );
            }
            else if (new System.Text.RegularExpressions.Regex("^\\w+$").IsMatch(Selection))
                try { q = new Query(Selection).Load(); }
                catch (Exception e) {
                    throw new Exception(string.Format("数据字典 \"{0}\" 下拉按查询 \"{1}\"执行出错\n", FieldName, Selection) + e.Message);
                }
            if (q != null && fill){
                var ds = q.ExecuteQuery(null, 0, 0, false);
                foreach(DataRow dr in ds.Tables[0].Rows){
                    if (ds.Tables[0].Columns.Count > 1)
                        DropDownList[dr[0].ToString()] = dr[ 1].ToString();
                    else {
                        var v = dr[0].ToString().Split('.');
                        DropDownList[v[0]] = v[v.Count() == 1?0:1];
                    }
                }
            }
            return q;
        }
    }


}
