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
    public enum CheckType { etError, etWarning, etConfirm };
    public class Check
    {
        public int CheckIdx { get; set; }
        
        public CheckType CheckType { get; set; }
        [Required]
        public string CheckSummary { get; set; }
        public string ParamToValidate { get; set; }
        public string Type { get; set; }
        public string CompareType { get; set; }
        public string CheckSQL { get; set; }
        public string ParamToCompare { get; set; }
        public bool CheckEnabled { get; set; }

        public void ValidateSelf(string owner, List<IParam> _params)
        {
            if (CheckEnabled)
            {
                if (Type.ToUpper().Equals("COMPARETO"))
                {
                    if (string.IsNullOrEmpty(CompareType))
                        throw new Exception(string.Format("\"{0}\" 的检查 \"{1}\" 类型是比较，但比较类型未设置.", owner, CheckSummary));
                    else if (string.IsNullOrEmpty(ParamToCompare))
                        throw new Exception(string.Format("\"{0}\" 的检查 \"{1}\" 类型是比较，但比较参数未设置.", owner, CheckSummary));
                    else if (_params.Find(q => q.ParamName == ParamToCompare) == null)
                        throw new Exception(string.Format("\"{0}\" 的检查 \"{1}\" 类型是比较，但比较参数未定义.", owner, CheckSummary));
                }
            }
        }


        public bool Validate(Dictionary<string, DBParam> paramValues, dynamic owner, string ownerName, out object errorMessageEx) {
            errorMessageEx = null;
            var type = Type.ToUpper().Trim();
            if (type.Equals("REQUIRED"))
            {
                var v = Convert.ToString(paramValues[ParamToValidate].ParamValue);
                return !string.IsNullOrEmpty(v) && v != "%" && v != "%%";
            }
            else if (type.Equals("QUERY"))
                using (var conn = Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn((owner.ConnAlias??"").Trim() == "" ? null : owner.ConnAlias))
                {
                    return DBHelper.ExecuteQuery(conn, owner.ParamNamePrefixHandle(CheckSQL), paramValues).Rows.Count > 0;
                }
            else if (type.Equals("SQL"))
                using (var conn = Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn((owner.ConnAlias ?? "").Trim() == "" ? null : owner.ConnAlias))
                {
                    var dt = DBHelper.ExecuteQuery(conn, owner.ParamNamePrefixHandle(CheckSQL), paramValues);
                    if (dt.Rows.Count == 0 || !dt.Columns.Contains("ResultCode"))
                        throw new CheckException(string.Format("SQL 类型的检查({0})必须返回一个ResultCode 字段.", CheckSummary));
                    return dt.Rows[0]["ResultCode"].ToString() == "0";
                }
            else if (type.Equals("QUERYGRID"))
                using (var conn = Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn((owner.ConnAlias ?? "").Trim() == "" ? null : owner.ConnAlias))
                {
                    System.Data.DataTable dtx = DBHelper.ExecuteQuery(conn, owner.ParamNamePrefixHandle(CheckSQL), paramValues);
                    var succ = dtx.Rows.Count == 0;
                    if (!succ) {
                        new l.core.FieldMetaHelper().Ready(dtx);
                        var fields = from System.Data.DataColumn dc in dtx.Columns select dc;
                        var rows = from System.Data.DataRow dr in dtx.Rows select fields.Select(p => dr[p]);
                        errorMessageEx = new
                        {
                            fields = fields.Select(p=>p.Caption),
                            rows = rows
                        };
                    };
                    return succ;
                }
            else if (type.Equals("COMPARETO"))
            {
                var v1 = paramValues[ParamToValidate].ParamValue;
                var v2 = paramValues[ParamToCompare].ParamValue;
                if (v1 == DBNull.Value || v2 == DBNull.Value) return false;
                else if (paramValues[ParamToValidate].DbType == DbType.Int32) {
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
                    else if (CompareType == "<=") return Convert.ToDateTime(v1) <= Convert.ToDateTime(v2);
                    else if (CompareType == "<>") return Convert.ToDateTime(v1) != Convert.ToDateTime(v2);
                    else return true;
                }
                else if (paramValues[ParamToValidate].DbType == DbType.String)
                {
                    if (CompareType == "=") return Convert.ToString(v1) == Convert.ToString(v2);
                    else if (CompareType == "<>") return Convert.ToString(v1) != Convert.ToString(v2);
                    else throw new CheckException(string.Format("Biz \"{0}\" 检查 \"{1}\" 的比较字符串只能比较等于和不等于", ownerName, CheckSummary));
                }
                else {
                    throw new Exception(string.Format("Biz \"{0}\" 检查 \"{1}\" 的无法比较此类型参数", ownerName, CheckSummary));
                    //return false;
                }
            }        //paramValues[ParamToValidate].ParamValue.ToString() > paramValues[ ParamToValidate].ParamValue.ToString();
            else return false;
        }
    }

     
       
}
