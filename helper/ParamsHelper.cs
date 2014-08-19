using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    public interface IParam 
    {
        string ParamName {get;set;}
        ColumnType ParamType { get; set; }
    }

    public class ParamsHelper{
        private Dictionary<string, object> ParamsValue;
        //public Dictionary<string, object> ParamValues { get { return ParamsValue; } }

        public override string ToString() {
            return Newtonsoft.Json.JsonConvert.SerializeObject(ParamsValue);
        }

        private IEnumerable<IParam> _params;
        public bool CheckParam { get; set; }

        public ParamsHelper() {
            ParamsValue = new Dictionary<string, object>();
            CheckParam = false;
        }

        public void SetParams(IEnumerable<IParam> _params) {
            CheckParam = true;
            this._params = _params;
        }

        private void CheckParams() {
            if (_params == null) throw new Exception("无法确定调用类的参数信息."); 
        }

        public IParam CheckParamExists(string paramName, bool silent = false) {
            if (CheckParam) {
                CheckParams();
                var pp = (from i in _params where i.ParamName == paramName select i);
                if (pp.Count() == 0)
                    if (silent) return null;
                    else throw new Exception(string.Format("参数[{0}]没有定义.", paramName));
                else return pp.First();
            }
            else return null;
        }

        public ParamsHelper SetParamValue(string paramName, object paramValue) {
            CheckParamExists( paramName);
            ParamsValue[paramName] = paramValue;
            return this;
        }

        public ParamsHelper AddParamValue(string paramName, object paramValue) {
            var p = CheckParamExists(paramName);
            if (p != null && !(p as BizParam).ParamRepeated) throw new Exception(
                    string.Format("参数 \"{0}\" 并非重复参数，不能调用 AddParamValue.", p.ParamName)
                );
            var l = ParamsValue.ContainsKey(paramName) ? ParamsValue[paramName] as List<object> : null;
            if (l == null) l = new List<Object>();
            
            l.Add(paramValue);
            ParamsValue[paramName]  = l;
            /*if (paramName.IndexOf("UpdateFlag") == 0 ) {
                var pp = (from i in _params where i.ParamName == paramName + ".count" select i);
                if (pp.Count() > 0)  SetParamValue(paramName + ".count", l.Count);
            }
            */
            return this;
        }

        //如果非重复参数，不管index 等于多少都返回
        public object GetParamValue(string paramName, int index = 0){
            CheckParamExists(paramName);
            if (!ParamsValue.ContainsKey(paramName)) return null;
            var p = ParamsValue[paramName];
            if (p == null) return null;
            if (p is List<object>) {
                var pl = (p as List<object>);
                if (pl.Count - 1 < index) throw new Exception(string.Format("重复参数 \"{0}\" 取第\"{1}\" (从0开始) 个值出错，数量不足", paramName, index));
                else return pl[index];
            }
            else return p;
        }

        public int GetParamCount(string paramName){
            CheckParamExists(paramName);
            if (!ParamsValue.ContainsKey(paramName)) return 0;
            else{
                var l = ParamsValue[paramName] as List<object>;
                return l == null ? 0 : l.Count;
            }
        }

        

        public List<Dictionary<string, DBParam>> GetDBParamsSet(IEnumerable<IParam> _params, IEnumerable<string> paramsName, int paramCount) {
            var r = new List<Dictionary<string, DBParam>>();
            for (int i = 0; i < paramCount; i++) {
                r.Add(paramsName.ToDictionary(
                            p1 => p1 , q => {
                                var pars = (from j in _params where j.ParamName == q select j);
                                return typeMatch(
                                    new DBParam { ParamValue = GetParamValue(q, i),}, 
                                    pars.Count() > 0 ? pars.First().ParamType : ColumnType.ctStr, null);
                            }
                        ));
            }
            return r;
        }

        public Dictionary<string, DBParam> GetDBParams(IEnumerable<IParam> _params)   {
            //this._params = _params;
            return (_params as IEnumerable<IParam>).ToDictionary(
                            p1 => p1.ParamName as string, q => typeMatch(new DBParam {
                                ParamValue = GetParamValue(q.ParamName)},  q.ParamType, null)
                        );
        
        }

        public string ParamNamePrefixHandle(IEnumerable<IParam> _params, string script) {
            foreach (IParam p in _params as IEnumerable<IParam>) {
                script = new System.Text.RegularExpressions.Regex("@"+ string.Join("", (from c in p.ParamName as string select string.Format("[{0}{1}]", c.ToString().ToUpper(), c.ToString().ToLower())))).Replace(script, "@par_" + p.ParamName);
            }
            return script;
        }

        //SmartParams 处理
        //1) 类型匹配（输入端全是 string，输出要匹配 ColumnType）
        //2) 日期处理
        //3) 字符串的 empty 和 null 处理
        //4) 模糊查询
        //5) 默认值
        public System.Data.DbType GetDBType(ColumnType type)
        {
            return type == ColumnType.ctDateTime ? System.Data.DbType.DateTime : (
                type == ColumnType.ctNumber ? System.Data.DbType.Decimal : (
                type == ColumnType.ctBool ? System.Data.DbType.Boolean : (
                type == ColumnType.ctBinary ? System.Data.DbType.Binary : System.Data.DbType.String)));
        }
        public ColumnType GetParamType(System.Type type)
        {
            return type == typeof(DateTime) ? ColumnType.ctDateTime : (
                type == typeof(int) || type == typeof(decimal) || type == typeof( Int16)|| type == typeof(double) ? ColumnType.ctNumber : (
                type == typeof(bool) ? ColumnType.ctBool : (
                type == typeof(byte) ? ColumnType.ctBinary : ColumnType.ctStr)));
        }

        private DBParam typeMatch(DBParam param, ColumnType type, object isnull) {
            var pv = param.ParamValue;
            if (type == ColumnType.ctStr){
                //if (pv == string.Empty) pv = null;
                //else 
                    pv =  Convert.ToString(pv).Trim();
            }
            else if (type== ColumnType.ctDateTime){
                if ((pv == null) || (pv.ToString().Trim() == string.Empty)) pv = DBNull.Value;
                else try { pv = Convert.ToDateTime(pv); } catch{ }
                if (isnull != null) try { isnull = DateTime.Now.AddDays(Convert.ToInt32(isnull)); }
                    catch { try { isnull = Convert.ToDateTime(isnull); } catch { isnull = DBNull.Value; } }
            }
            else if (type == ColumnType.ctBool) {
                pv = (Convert.ToString(pv) == "true")||(Convert.ToString(pv) == "true,false")?true : Convert.ToString(pv) == ""?DBNull.Value as object : false;
            }
            else if (type == ColumnType.ctNumber) {
                //if ((pv is string) && string.IsNullOrEmpty(pv.ToString())) pv = DBNull.Value;
                if (Convert.ToString(pv) == "") pv = DBNull.Value;
            }
            if ((isnull != null) && (isnull != "") && ((pv == null) || pv.ToString().Equals(string.Empty) || (pv == DBNull.Value))) pv = isnull;
            param.ParamValue = pv;
            param.DbType = GetDBType(type);
            return param;
        }

        private void likeHandle(DBParam param, ColumnType type,  bool likeLeft, bool likeRight)
        {
            var pv = param.ParamValue;
            
            if (likeLeft) pv = "%" + Convert.ToString(pv);
            if (likeRight) pv = Convert.ToString(pv) + "%";
            param.ParamValue = pv;
        }

        public Dictionary<string, DBParam> GetQueryDBParams(IEnumerable<IParam> _params) {
           // this._params = _params;
            return (_params as IEnumerable<dynamic>).ToDictionary(
                            p1 => p1.ParamName as string, q =>
                            {
                                object pv = GetParamValue(q.ParamName);
                                var param = new DBParam{ ParamValue = pv};
                                typeMatch(param, q.ParamType, q.IsNull);
                                likeHandle(param, q.ParamType, q.LikeLeft, q.LikeRight);
                                return param;
                            }
                        );

        }
    }
}
