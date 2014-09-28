using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using l.core;

namespace l.core
{
    public class ExternalSrv : MetaExternalSrv   {
        public string HashCode { get; set; }
        public string Version { get; set; }

        private void checkHashCode()  {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm()  {
            return OrmHelper.From("metaExternalSrv").F("SrvCode", "Name",   "SrvType", "SrvParams", "URI", "Version", "HashCode").PK("SrvCode").Obj(this).End;
        }
 
        public ExternalSrv Load()  {
            getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<ExternalSrv>(this, "MetaExternalSrv", new[] { "SrvCode" }, true)) getOrm().Setup();
            return this;
        }
        public void Remove() { getOrm().Dels(); }
        public void Save() { getOrm().Save(); }
    }
 
    public class MetaExternalSrv  {
        public string Name { get; set; }
        public string SrvParams { get; set; }
        public string SrvCode { get; set; }
        public string SrvType { get; set; }
        public string URI { get; set; }
    }

}