using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using l.core;

namespace l.core
{
    public class BizSrv : MetaBizSrv
    {
        public string HashCode { get; set; }
        public string Version { get; set; }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaBizSrv").PK("SrvCode").Obj(this).End;
        }
        public void Remove() { getOrm().Dels(); }
        public BizSrv Load()  {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) 
                if (!VersionHelper.Helper.CheckNewAs<BizSrv>(this, "MetaBizSrv", new[] { "SrvCode" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("BizSrv \"{0}\" does not exist.", SrvCode));
            return this;
        }

        public void Save()  { getOrm().Save(); }
    }


    public class MetaBizSrv 
    {
        public string Name{get;set;}
        public string BizID {get;set;}
        public string SrvCode {get;set;}
        public string Summary {get;set;}
         
    }
}