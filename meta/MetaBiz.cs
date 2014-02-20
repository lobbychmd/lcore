using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;

namespace l.core
{

    //通用模型加上ORM 设定
    public class Biz: MetaBiz {
        public string HashCode { get; set; }
        new public ParamsHelper SmartParams { get { return base.SmartParams; } }

        public Biz(string bizID) {
            this.BizID = bizID;
            Scripts = new List<BizScript>();
            Params = new List<BizParam>();
            Checks = new List<BizCheck>();
            //base.SmartParams = new ParamsHelper( );
        }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode,
                    "校验码错误.");
        }

        public string UpdateSQL() { return getOrm().UpdateSQL();
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaBiz").F("BizID", "HashCode", "Version").PK("BizID").Obj(this).End
                .SubFrom("metaBizItems").Obj(Scripts).End
                .SubFrom("metaBizChecks").Obj(Checks).F("CheckIdx","CheckType", "CheckRepeated", "CheckSummary", "ParamToValidate", "ParamToCompare", "CompareType", "Type", "CheckSQL", "CheckEnabled", "CheckUpdateFlag", "CheckExecuteFlag").
                    MF("Type", "ValidateType").End
                .SubFrom("metaBizParams").Obj(Params).End;
        }

        public Biz Load() {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null) if (!VersionHelper.Helper.CheckNewAs<Biz>(this,"MetaBiz", new []{"BizID"}, true))  loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Biz \"{0}\" does not exist.", BizID));
            else if (Scripts.Where(p=>p.ProcEnabled).Count() == 0) throw new Exception(string.Format("Biz \"{0}\" does not include any valid statements.", BizID));
            //checkHashCode();
            SmartParams.SetParams(Params);
            return this;
        }

        public void Save()
        {
            getOrm().Save();
        }
    }

    //下面是通用模型
    public class BizValidationResult:  ValidationResult{
        public BizValidationResult(string errorMessage)
            : base(errorMessage){}

        public BizValidationResult(string errorMessage, IEnumerable<string> memberNames)
            : base(errorMessage, memberNames){ }

        public BizValidationResult(string errorMessage, IEnumerable<string> memberNames, bool warning)
            : base(errorMessage, memberNames){ 
            this.Warning  = warning;
        }

        public bool Warning { get; set; }
    }

    public class BizResult {
        public bool IsValid { get { return Errors ==null || Errors.Where(p=> !p.Warning ).Count() == 0; } }
        //public ValidationResult[] Errors { get; set; }
        public List<BizValidationResult> Errors { get; set; }  //list 类型方便添加
        //public Dictionary<BizParam, object> ReturnValues { get; set; }
        public Dictionary<string, object> ReturnValues { get; set; }

        public BizResult() {
            Errors = new List<BizValidationResult>();
        }
    }

    public class BizException : Exception {
        public BizException(string message) : base(message) { 
        }
    }

    public class MetaBiz
    {
        [Required]
        public string BizID { get; set; }
        public string Version { get; set; }
        public string ConnAlias { get; set; }
        public List<BizScript> Scripts { get; set; }
        public List<BizParam> Params { get; set; }
        public List<BizCheck> Checks { get; set; }
        protected ParamsHelper SmartParams;

        public MetaBiz() {
            Checks = new List<BizCheck>();
            Params = new List<BizParam>();
            Scripts = new List<BizScript>();
            SmartParams = new ParamsHelper()  ;
        }
        
        private void handleException(BizResult r, Exception e, string context, bool reThrow ) {
            if (e is BizException) ; //内层的异常，已经处理过了
            else
            {
                if (e is System.Data.SqlClient.SqlException && ((System.Data.SqlClient.SqlException)(e)).Number > 90000) {
                    r.Errors = new List<BizValidationResult> { new BizValidationResult(e.Message) };
                }
                else r.Errors = new List<BizValidationResult> { new BizValidationResult(e.Message + "\n\n" + context + "\n\n" + e.StackTrace) };
            }

            if (reThrow) throw new BizException(e.Message); //抛出异常以便回到最外层处理： 回滚
            
        }

        private string errorContext(string summary, string sql, Dictionary<string, DBParam>  paramValues) {
            return summary + "\n\n" + Newtonsoft.Json.JsonConvert.SerializeObject(paramValues);
            //return sql + "\n\n" + string.Join("\n", paramValues.Select(p1 => p1.Key + "=" + p1.Value.ParamValue));
        }

        private void validateSelf() {
            Checks.ForEach(p => {
                if (p.CheckEnabled){
                    if (p.Type.ToUpper().Equals("COMPARETO") ){
                        if (string.IsNullOrEmpty(p.CompareType))
                            throw new Exception(string.Format("Biz \"{0}\" 的检查 \"{1}\" 类型是比较，但比较类型未设置.", BizID, p.CheckSummary));
                        else if (string.IsNullOrEmpty(p.ParamToCompare))
                            throw new Exception(string.Format("Biz \"{0}\" 的检查 \"{1}\" 类型是比较，但比较参数未设置.", BizID, p.CheckSummary));
                        else if (Params.Find(q=>q.ParamName == p.ParamToCompare) == null)
                            throw new Exception(string.Format("Biz \"{0}\" 的检查 \"{1}\" 类型是比较，但比较参数未定义.", BizID, p.CheckSummary));
                    }
                }
            });
        }

        public BizResult Execute(IDbConnection conn)
        {
            validateSelf();
            var connection = conn == null ? Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn(string.IsNullOrEmpty(ConnAlias) ? null : ConnAlias) : null;
            try {
                BizResult r = new BizResult ();
                var trans = DBHelper.GetTranscation(conn ?? connection);
                try { 
                    r.Errors = Validate(null).ToList();
                    if (r.IsValid){
                        foreach(var script in Scripts.Where(p=>p.ProcEnabled )){
                            var paramsName = (from Match m in new Regex(":([a-zA-z_][a-zA-z_\\d]+)").Matches(script.ProcSQL ?? "") select m.Value.Substring(1))
                                .Union(script.ProcRepeated && (script.ProcUpdateFlag !=null) ? new[] { script.ProcUpdateFlag } : new string[] { });

                            foreach(var p in SmartParams.GetDBParamsSet(Params, paramsName, script.ProcRepeated ?
                                    SmartParams.GetParamCount(script.ProcUpdateFlag ?? paramsName.Where(p => Params.Find(pp => pp.ParamName == p ).ParamRepeated).First()) : 1))  
                            {
                                if (script.ProcRepeated && (!string.IsNullOrEmpty(script.ProcExecuteFlag)) && (("*" + script.ProcExecuteFlag).IndexOf(p[script.ProcUpdateFlag??"UpdateFlag"].ParamValue.ToString()) < 1)) 
                                    continue;
                                int RowsAffected = 0;
                                var paramValues = p;// SmartParams.GetDBParams(Params);
                                if (script.InterActive){
                                    try {
                                        var dt = DBHelper.ExecuteQuery(conn ?? connection, SmartParams.ParamNamePrefixHandle(Params, script.ProcSQL), paramValues);
                                        foreach (System.Data.DataColumn dc in dt.Columns) 
                                            if (Params.Find(q => q.ParamName == dc.ColumnName) !=null) 
                                                SmartParams.SetParamValue(dc.ColumnName, dt.Rows[0][dc]);
                                        if ((dt.Rows.Count >0) && dt.Columns.Contains("RowsAffected")) RowsAffected  = Convert.ToInt32(dt.Rows[0]["RowsAffected"]);
                                    }
                                    catch (Exception e) { handleException(r, e, errorContext(script.ProcSummary, script.ProcSQL, paramValues), true); }
                                }
                                else try { RowsAffected = DBHelper.ExecuteSql(conn ?? connection, SmartParams.ParamNamePrefixHandle(Params, script.ProcSQL), paramValues); }
                                    catch (Exception e) { handleException(r, e, errorContext(script.ProcSummary, script.ProcSQL, paramValues), true); }
                                if ((script.ExpectedRows > 0) && (script.ExpectedRows != RowsAffected))
                                    handleException(r, new Exception(string.Format("返回行数({0})不等于{1}，可能是其他用户已经修改过这条记录了.", RowsAffected, script.ExpectedRows)), errorContext(script.ProcSummary, script.ProcSQL, paramValues), true);
                            }
                        };
                        DBHelper.CommitTranscation(trans);
                    }
                    //r.ReturnValues = Params.ToDictionary(p => p, q => SmartParams.GetParamValue(q.ParamName));
                    r.ReturnValues = Params.Where(p=>p.Output).ToDictionary(p => p.ParamName, q => SmartParams.GetParamValue(q.ParamName));

                }
                catch (Exception e) { 
                    DBHelper.RollbackTranscation(trans); 
                    handleException(r, e, null, false);
                }
                return r;
            }
            finally
            {
                if (conn == null) connection.Dispose();
            }
        }

        public BizResult CheckConfirm(IDbConnection conn)
        {
            validateSelf();
            BizResult r = new BizResult ();
            r.Errors = InternalValidate(null, true, false).ToList();
            return r;
        }

        public IEnumerable<BizValidationResult> Validate(ValidationContext validationContext) {
            return InternalValidate(validationContext, false, true);
        }

        public IEnumerable<BizValidationResult> InternalValidate(ValidationContext validationContext, bool checkConfirm, bool checkNotConfirm) {
            var pv = SmartParams.GetDBParams(Params);
            foreach (var c in Checks.Where(p => p.CheckEnabled ) ) {
                if( !checkConfirm && c.CheckType == BizCheckType.etConfirm) continue;
                if( !checkNotConfirm && c.CheckType != BizCheckType.etConfirm ) continue;

                var p2v = Params.Find(p => p.ParamName ==  c.ParamToValidate);
                if (p2v == null) throw new Exception(string.Format("Biz \"{0}\" 的检查 \"{1}\" 参数是 \"{2}\"，但并未定义.", BizID, c.CheckSummary, c.ParamToValidate));
                if (p2v.ParamRepeated != c.CheckRepeated) throw new Exception(string.Format("Biz \"{0}\" 的检查 \"{1}\" 的重复设置跟待检查的参数不一致.", BizID, c.CheckSummary));
                var par = Params.Find(p => p.ParamName == (string.IsNullOrEmpty( c.CheckUpdateFlag) ? c.ParamToValidate : c.CheckUpdateFlag));
                //if ((par == null ) || (!par.ParamRepeated)){
                if(!c.CheckRepeated){
                    if (!c.Validate(pv, this)) yield return new BizValidationResult(c.CheckSummary, new[] { c.ParamToValidate }, c.CheckType == BizCheckType.etWarning);
                }else {
                    if (par == null) throw new Exception(string.Format("Biz \"{0}\" 的检查 \"{1}\" 未能确定更新标志参数，可能未定义UpdateFlag参数.", BizID, c.CheckSummary));
                    var paramsName = (from Match m in new Regex(":([a-zA-z_]+)").Matches(c.CheckSQL ?? "") select m.Value.Substring(1))
                        .Union(new[] { par.ParamName, c.ParamToValidate });
                    if (!string.IsNullOrEmpty(c.ParamToCompare)) paramsName = paramsName.Union(new []{c.ParamToCompare});
                    int i = 0;
                    List<Dictionary<string, DBParam>> pl = null;
                    try {pl = SmartParams.GetDBParamsSet(Params, paramsName, SmartParams.GetParamCount(par.ParamName));
                        }
                    catch(Exception e){
                        throw new Exception( string.Format("Biz \"{0}\" 业务检查 \"{1}\"执行中, ", BizID, c.CheckSummary) + e.Message);
                    }
                    foreach(var p in pl){
                            if (!c.Validate(p, this))
                                yield return new BizValidationResult(c.CheckSummary, new[] { c.ParamToValidate + "." + i.ToString(),  }, c.CheckType == BizCheckType.etWarning);
                        i++;                            
                    };
                }
            }
        }

        public string ParamNamePrefixHandle(string sql) {
            return SmartParams.ParamNamePrefixHandle(Params, sql);
        }
    }

    public enum BizCheckType { etError, etWarning, etConfirm };
    public class BizCheck {
        public int CheckIdx { get; set; }
        public bool CheckRepeated { get; set; }
        public BizCheckType CheckType { get; set; }
        [Required]
        public string CheckSummary { get; set; }
        public string ParamToValidate { get; set; }
        public string Type { get; set; }
        public string CompareType { get; set; }
        public string CheckSQL { get; set; }
        public string ParamToCompare { get; set; }
        public bool CheckEnabled { get; set; }
        public string CheckUpdateFlag { get; set; }
        public string CheckExecuteFlag { get; set; }

        public bool Validate(Dictionary<string, DBParam> paramValues, MetaBiz biz) {
            var type = Type.ToUpper().Trim();
            if (type.Equals("REQUIRED"))   {
                return !string.IsNullOrEmpty(Convert.ToString(paramValues[ParamToValidate].ParamValue));
            }
            else if (type.Equals("QUERY"))
                using (var conn = Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn(string.IsNullOrEmpty(biz.ConnAlias) ? null : biz.ConnAlias))
                {
                    return DBHelper.ExecuteQuery(conn, biz.ParamNamePrefixHandle(CheckSQL), paramValues).Rows.Count > 0;
                }
            else if (type.Equals("SQL"))
                using (var conn = Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn(string.IsNullOrEmpty(biz.ConnAlias) ? null : biz.ConnAlias))
                {
                    var dt = DBHelper.ExecuteQuery(conn, biz.ParamNamePrefixHandle(CheckSQL), paramValues);
                    if (dt.Rows.Count == 0 || !dt.Columns.Contains("ResultCode"))
                        throw new Exception(string.Format("SQL 类型的检查({0})必须返回一个ResultCode 字段.", CheckSummary));
                    return dt.Rows[0]["ResultCode"].ToString() == "0";
                }
            else if (type.Equals("COMPARETO"))
            {
                var v1 = paramValues[ParamToValidate].ParamValue;
                var v2 = paramValues[ParamToCompare].ParamValue;
                if (paramValues[ParamToValidate].DbType == DbType.Int32) {
                    if (CompareType == "=") return Convert.ToInt32(v1) == Convert.ToInt32(v2);
                    else if (CompareType == ">") return Convert.ToInt32(v1) > Convert.ToInt32(v2);
                    else if (CompareType == "<") return Convert.ToInt32(v1) < Convert.ToInt32(v2);
                    else if (CompareType == ">=") return Convert.ToInt32(v1) >= Convert.ToInt32(v2);
                    else if (CompareType == "<>") return Convert.ToInt32(v1) != Convert.ToInt32(v2);
                    else return true;
                }
                else if (paramValues[ParamToValidate].DbType == DbType.Decimal)
                {
                    if (CompareType == "=") return Convert.ToDouble(v1) == Convert.ToDouble(v2);
                    else if (CompareType == ">") return Convert.ToDouble(v1) > Convert.ToDouble(v2);
                    else if (CompareType == "<") return Convert.ToDouble(v1) < Convert.ToDouble(v2);
                    else if (CompareType == ">=") return Convert.ToDouble(v1) >= Convert.ToDouble(v2);
                    else if (CompareType == "<>") return Convert.ToDouble(v1) != Convert.ToDouble(v2);
                    else return true;
                }
                else if (paramValues[ParamToValidate].DbType == DbType.DateTime)
                {
                    if (CompareType == "=") return Convert.ToDateTime(v1) == Convert.ToDateTime(v2);
                    else if (CompareType == ">") return Convert.ToDateTime(v1) > Convert.ToDateTime(v2);
                    else if (CompareType == "<") return Convert.ToDateTime(v1) < Convert.ToDateTime(v2);
                    else if (CompareType == ">=") return Convert.ToDateTime(v1) >= Convert.ToDateTime(v2);
                    else if (CompareType == "<>") return Convert.ToDateTime(v1) != Convert.ToDateTime(v2);
                    else return true;
                }
                else if (paramValues[ParamToValidate].DbType == DbType.String)
                {
                    if (CompareType == "=") return Convert.ToString(v1) == Convert.ToString(v2);
                    else if (CompareType == "<>") return Convert.ToString(v1) != Convert.ToString(v2);
                    else throw new Exception(string.Format("Biz \"{0}\" 检查 \"{1}\" 的比较字符串只能比较等于和不等于", biz.BizID, CheckSummary));
                }
                else {
                    throw new Exception(string.Format("Biz \"{0}\" 检查 \"{1}\" 的无法比较此类型参数", biz.BizID, CheckSummary));
                    //return false;
                }
            }        //paramValues[ParamToValidate].ParamValue.ToString() > paramValues[ ParamToValidate].ParamValue.ToString();
            else return false;
        }
    }

    public class BizScript {
        public int ProcIdx { get; set; }
        public string ProcSQL { get; set; }
        public bool InterActive { get; set; }
        public bool ProcRepeated { get; set; }
        [Required]
        public string ProcSummary { get; set; }
        [Required]
        public int ExpectedRows { get; set; }
        public bool ProcEnabled { get; set; }
        public string ProcUpdateFlag { get; set; }
        public string ProcExecuteFlag {get;set;}
    }

    public class BizParam  : IParam {
        //这个要补上去
        //public ColumnType ParamType { get; set; }
        public string ParamName { get; set; }
        public bool ParamRepeated { get; set; }
        public bool Output { get; set; }
        public ColumnType ParamType { get; set; }
    }

}
