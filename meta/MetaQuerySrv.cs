using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using l.core;

namespace l.core
{
    public class QuerySrv : MetaQuerySrv
    {
        public string HashCode { get; set; }
        public string Version { get; set; }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaSrv").F("SrvCode", "Name", "QueryName", "Version", "IdField", "HashCode", "Summary").PK("SrvCode").Obj(this).End;
        }

        public QuerySrv Load() {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0)
                if (!VersionHelper.Helper.CheckNewAs<QuerySrv>(this, "MetaDataPublishSrv", new[] { "SrvCode" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("QuerySrv \"{0}\" does not exist.", SrvCode));
            return this;
        }
        public void Remove() { getOrm().Dels(); }
        public void Save()  { getOrm().Save(); }

        public DataSet Execute(string id, Dictionary<string, object> _params, int limit, int start =0) {
            var q = new l.core.Query(QueryName).Load();
            foreach (var i in _params)
                if (q.Params.Find(p=>p.ParamName == i.Key) !=null ) q.SmartParams.SetParamValue(i.Key, i.Value);
            if ( (IdField ??"").Trim() != "" && id != null) q.SmartParams.SetParamValue(IdField, id);
            return q.ExecuteQuery(null, start, limit);
        }
    }

    public class MetaQuerySrv {
        public string SrvCode { get; set; }
        public string Name { get; set; }
        public string Summary { get; set; }
        public string IdField { get; set; }
        public string QueryName { get; set; }
    }
}