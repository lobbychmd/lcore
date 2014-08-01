using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Diagnostics;
using System.Data;
using l.core;

namespace l.core{
    public class DataSubscribeSrv : MetaDataSubscribeSrv  {
        public string HashCode { get; set; }
        public string Version { get; set; }
        
        public DataSubscribeSrv() {
            Items = new List<MetaDataSubscribeSrvItem>();
        }
        private void checkHashCode()  {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaDataSubscribeSrv")
                .F("Name", "SrvCode", "HashCode", "Version","Interval",  "IntervalUnit").PK("SrvCode").Obj(this).End
                .SubFrom("metaDataSubscribeSrvItems").Obj(Items).End;
        }

        public DataSubscribeSrv Load()  {
            getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<DataSubscribeSrv>(this, "MetaDataSubscribeSrv", new[] { "SrvCode" }, true)) 
                getOrm().Setup();
            return this;
        }

        public void Save() {  
            getOrm().Save();  
        }
    }

    public class MetaDataSubscribeSrv {
         

        public string SrvCode { get; set; }
        public string Name {get;set;}
        public decimal Interval {get;set;}
        public string IntervalUnit {get;set;}
        public List<MetaDataSubscribeSrvItem> Items { get; set; }

        virtual public void Init(){
        
        }
    }

    public class MetaDataSubscribeSrvItem {
        //public string SrvCode { get; set; }
        public int QueueIdx { get; set; }
        public string QueueName { get; set; }
        public string Direction { get; set; }
        public string QueueSummary { get; set; }
        public int BatchSize { get; set; }
        public bool Enabled { get; set; }
        public string SourceSrv {get;set;}
        public string DestinationSrv {get;set;}
        public string DestSrvParams { get; set; }
        //public RepDestinationResult LastInvoke { get; set; }

    }
}

 