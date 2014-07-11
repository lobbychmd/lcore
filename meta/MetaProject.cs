using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace l.core
{
    public class MenuItem : l.core.MetaModule{
        public MenuItem Parent { get; set; }
        public List<MenuItem> Children { get; set; }
        public MenuItem() {
            Children = new List<MenuItem>();
        }

        public MenuItem Find(string moduleID) { 
            foreach(MenuItem i in Children){
                if (i.ModuleID == moduleID) return i;
            }
            foreach (MenuItem i in Children)
            {
                var m = i.Find(moduleID);
                if (m != null) return m;
            }
            return null;
        }

        private void children(MenuItem mi, DataTable dtModule, DataTable dtModulePower , DataTable dtMenu, List<MetaModule> modules) {
            var v = new DataView(dtMenu, "ParentID='" + (mi.ModuleID ?? "") + "'", "ModuleID", DataViewRowState.CurrentRows);
            v.Sort = "Idx";
            foreach (System.Data.DataRowView dr in v)
            {
                var mid =dr.Row["ModuleID"].ToString();
                var m = dtModule.DefaultView.Find(mid);
                if (m >=0) {
                    var mii = new MenuItem { ModuleID = mid, Caption = dtModule.DefaultView[m].Row["ModuleName"].ToString(), Parent = mi, ParentID = dr["ParentID"].ToString(), Path = dtModule.DefaultView[m].Row["Path"].ToString() };
                    //构造菜单时，顺便填充一个所有模块列表清单（例如流程图要用）
                    modules.Add(new MetaModule { ModuleID = mid, Caption = mii.Caption}); 

                    children(mii, dtModule, dtModulePower, dtMenu, modules);
                    var p = dtModulePower.DefaultView.Find(mid);

                    if ((p >= 0) || (mii.Children.Count>0)) mi.Children.Add(mii);
                }
            }
        }

        public void SyncModuleFromMeta()
        {
            //var modules = 
        }

        public void Load(DataTable dtModule, DataTable dtModulePower , DataTable dtMenu, List<MetaModule> modules) {
            dtModulePower.DefaultView.Sort = "FuncID";
            dtModule.DefaultView.Sort = "ModuleID";
            //dtMenu.DefaultView.Sort = "Idx";
            children(this, dtModule, dtModulePower, dtMenu, modules);
        }
    }

    //全局的。每个用户都共享
    public class Project : MetaProject {
        public Dictionary<string, object> Params { get; set; }

        public Project(string frmConnKey = null, Dictionary<string, string> connMaps = null): base(frmConnKey, connMaps){
            Params = new Dictionary<string, object>();
        }

        private OrmHelper getCloudOrm()  {  //云，用模拟器号做主键
            return OrmHelper.From("metaProject").F("ProjectCode","ProjectName",  "SimulateCode", "StaConnectionString", "FrmConnectionString", "SyncPassword").PK("SimulateCode").Obj(this).End;
        }
        private OrmHelper getOrm()  //非云，用ProjectCode做主键
        {
            return OrmHelper.From("metaProject").F("ProjectCode", "ProjectName", "SimulateCode" ).PK("ProjectCode").Obj(this).End;
        }

        public string CloudUrl(string url) {  //用模拟器号 是否为空 代表是否云
            return SimulateCode == null ? url : "/" + SimulateCode.ToString() + url;
        }

        public Project Load()  {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<Project>(this, "MetaProject", new string[] { }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Project \"{0}\" does not exist.", ProjectCode));
            //checkHashCode();
            SimulateCode = null;//用模拟器号 空 代表非云
            return this;
        }

        public Project CloudLoad() {
            getCloudOrm().Cloud(false).Setup();
            if (VersionHelper.Helper != null) {
                var prj = VersionHelper.Helper.GetAs<Project>("MetaProject", new Dictionary<string, string> { { "SimulateCode", SimulateCode } }) as Project;
                if (prj != null && (ProjectName != prj.ProjectName || SyncPassword != prj.SyncPassword || ProjectCode != prj.ProjectCode))
                {
                    ProjectName = prj.ProjectName;
                    SyncPassword = prj.SyncPassword;
                    ProjectCode = prj.ProjectCode;
                    using (var conn = l.core.DBHelper.GetConnection(0)) {
                        DBHelper.ExecuteSql(conn,
                            @"update metaProject set ProjectName = :ProjectName, SyncPassword = :SyncPassword, ProjectCode = :ProjectCode where SimulateCode = :SimulateCode
                              if @@rowcount = 0 insert into metaProject(SimulateCode, ProjectName, ProjectCode, SyncPassword) values(:SimulateCode, :ProjectName, :ProjectCode, :SyncPassword)",
                            new Dictionary<string , DBParam>{
                                {"SimulateCode", new DBParam{ParamValue = SimulateCode}},
                                {"SyncPassword", new DBParam{ParamValue = SyncPassword}},
                                {"ProjectCode", new DBParam{ParamValue = ProjectCode}},
                                {"ProjectName", new DBParam{ParamValue = ProjectName}}
                            });

                    }
                }

            }
            //checkHashCode();
            if (string.IsNullOrEmpty(FrmConnectionString)) {
                var cloudDBSetting = System.Configuration.ConfigurationManager.AppSettings["CloudDBSetting"];
                using (var conn = l.core.DBHelper.GetConnection("System.Data.SqlClient",  cloudDBSetting))  {
                    FrmConnectionString = cloudDBSetting.Replace("=master;", "=idcclouddb" + SimulateCode + ";");
                   // StaConnectionString = cloudDBSetting.Replace("=master;", "=sta" + SimulateCode + ";");
                    using (var conn1 = l.core.DBHelper.GetConnection(0))
                    {
                        DBHelper.ExecuteSql(conn1,
                            @"update metaProject set FrmConnectionString = :FrmConnectionString, StaConnectionString = :StaConnectionString where SimulateCode = :SimulateCode",
                            new Dictionary<string, DBParam>{
                                {"SimulateCode", new DBParam{ParamValue = SimulateCode}},
                                {"FrmConnectionString", new DBParam{ParamValue = FrmConnectionString}},
                                {"StaConnectionString", new DBParam{ParamValue = FrmConnectionString}}
                            });

                    }
                    DBHelper.ExecuteSql(conn, string.Format("create database {0}  COLLATE  Chinese_PRC_CI_AS", "idcclouddb" + SimulateCode), null);
                    //DBHelper.ExecuteSql(conn, "create database sta" + SimulateCode, null);
                }
            }
            return this;
        }

        public void Save()
        {

            getOrm().Save();
        }
        static public Project Current;
        static public Project Root;
    }

    //下面是通用模型
    public class ConnInfo {
        public string Where; public string ConnAlias; public int ConnIndex; 
    }
    
    //存放链接
    //存放子系统类型信息
    //提供 GetConn，可以处理多链接，但不能处理位置
    //处理多位置的 l.core.web.Account.Current().GetConn
    public class MetaProject
    {
        [Required]
        private string frmConnKey;
        private Dictionary<string, string> connMapInfo  { get; set; }

        public string SimulateCode { get; set; }
        public string ProjectName { get; set; }
        public string Version { get; set; }
        public string FrmConnectionString { get; set; }
        public string StaConnectionString { get; set; }
        public string SyncPassword { get; set; }
        public string ProjectCode { get; set; }
        public Dictionary<string, object> Params { get; set; }

        public MetaProject(string frmConnKey , Dictionary<string, string> connMaps) {
            var dcsf = DBHelper.DefaultConnectionStringPrefix;
            if (frmConnKey == null) frmConnKey= dcsf + "0";
            connMapInfo = new Dictionary<string, string>();
            var cs = DBHelper.connConfig;
           
            this.frmConnKey = frmConnKey;
            if (connMaps != null)
                foreach (var i in connMaps) {
                    new Connection(i.Key).Load(); //检查idc.net 有没有定义
                    if (cs[i.Value] == null) throw new Exception(string.Format("Web.config 没有连接\"{0}\"的定义.", i.Value));

                    connMapInfo[i.Key] = i.Value;
                }
            if (connMaps == null || connMaps.Count == 0)
            {
                if (cs[dcsf + "1"] == null) throw new Exception("Web.config 无法确定业务库.");
                else connMapInfo["main"] = dcsf + "1";
            }
        }

        public System.Data.IDbConnection GetFrmConn() {
            return !string.IsNullOrEmpty(FrmConnectionString) ? DBHelper.GetConnection("System.Data.SqlClient", FrmConnectionString) : DBHelper.GetConnection(frmConnKey);
        }

        public System.Data.IDbConnection GetConn(string aliasName = null)
        {
            if (!string.IsNullOrEmpty(StaConnectionString))
                return DBHelper.GetConnection("System.Data.SqlClient", StaConnectionString);

            var n = aliasName == null? connMapInfo.First().Value : connMapInfo.ContainsKey(aliasName)?connMapInfo[aliasName]:null;
            if (n == null) throw new Exception(string.Format("业务库别名\"{0}\" 未在系统定义.", aliasName));
            return l.core.DBHelper.GetConnection(n);
        }
    }

    public class Connection : MetaConnection  {
        private OrmHelper getOrm() {
             return OrmHelper.From("metaConnection").F("Summary", "Alias").PK("Alias").Obj(this).End; ; 
        }
        
        public Connection(string alias) {
            this.Alias = alias;
        }

        public Connection Load() {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<Connection>(this, "MetaConnection", new[] { "Alias" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Connection \"{0}\" does not exist.", Alias)); 
            return this;
        }

        public string UpdateSQL() {
            return getOrm().UpdateSQL();
        }

        public void Save(){
            getOrm().Save();
        }
    }

    //就是链接类型表（只和组织类型相关，跟实际组织结构多少无关）
    public class MetaConnection{
        //ConnectionString 不下载到本地。 只为服务端使用（本地用 web.config 设置）
        //public string ConnectionString {get;set;}
        public string Summary { get; set; }
        public string Alias { get; set; }
        public string Version { get; set; }
    }

    //就是组织结构类型表
    public class MetaSubsystem {
        public string Code { get; set; }
        public string Name { get; set; }
        public Dictionary<string, int> ConnAlias { get; set; }
        
        public MetaSubsystem (){
        }

    }

    public class Theme : MetaTheme {
        private OrmHelper getOrm() {
            return OrmHelper.From("metaTheme").F("Theme", "LayoutUI", "LayoutQueries","Version", "HashCode").PK("Theme").Obj(this).End
                .SubFrom("metaThemeStyle").F("StyleSection", "StyleContent" ).Obj(StyleSheet).End; 
        }
        public Theme(string theme) {
            this.Theme = theme;
        }

        public Theme Load() {
            getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<Theme>(this, "MetaTheme", new[] { "Theme" }, true)) getOrm().Setup();
            return this;
        }

        public string UpdateSQL() {
            return getOrm().UpdateSQL();
        }

        public void Save(){
            getOrm().Save();
        }
    }

    public class MetaTheme {
        private string layoutqueries;
        public string Theme { get; set; }
        public string LayoutUI { get; set; }
        public string LayoutQueries { get { return layoutqueries; } set { layoutqueries = value; } }
        public string Version { get; set; }
        public string HashCode {get; set; }
        private string[] queries;

        public List<MetaThemeStyle> StyleSheet { get; set; }
        public MetaTheme() {
            StyleSheet = new List<MetaThemeStyle>();
        }
        public string[] QueryList { get {
            if (queries == null)
                queries = layoutqueries == null ? null : layoutqueries.Trim().Split(';').Where(p => p.Trim() != string.Empty).ToArray();
            return queries; }  
        }
    }

    public class MetaThemeStyle {
        public string StyleSection { get; set; }
        public string StyleContent { get; set; }
    }

}
