using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace l.core
{
    //通用模型加上ORM 设定
    public class Module : MetaModule {
        public string HashCode { get; set; }

        public Module(string moduleID) {
            this.ModuleID = moduleID;
            
            //不能加这句，否则反序列号的时候也会去load db,更新的时候就死循环
            //if (!string.IsNullOrEmpty( ModuleID )) Load();
        }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaModule").F("ModuleID", "Caption", "ParentID", "Path", "HashCode", "Version").MF("Caption", "ModuleName").PK("ModuleID").Obj(this).End
                .SubFrom("metaModuleFunc").Obj(Functions).End
                .SubFrom("metaModulePage").F("PageParams", "PageID", "PageType","PageLookup", "Params", "UI", "PageFlow", "Queries").Obj(ModulePages).End;
        }

        public Module Load() {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null) if (!VersionHelper.Helper.CheckNewAs<Module>(this, "MetaModule", new[] { "ModuleID" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Module \"{0}\" does not exist.", ModuleID));
            //checkHashCode();
            return this;
        }

        public string UpdateSQL() {
            return getOrm().UpdateSQL();
        }

        public void Save() {
            getOrm().Save();
        }
    }

    public class Function : MetaFunction
    {
        public string HashCode { get; set; }
        public Function(string funcID) {
            this.FuncID = funcID;
        }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaFunction").PK("FuncID").Obj(this).End;
        }

        public Function Load() {
            getOrm().Setup();
            if (VersionHelper.Helper != null) if (!VersionHelper.Helper.CheckNewAs<Function>(this,"MetaFunction", new []{"FuncID"}, true)) getOrm().Setup();
            //checkHashCode();
            return this;
        }

        public void Save()
        {
            getOrm().Save();
        }
        public string UpdateSQL()
        {
            return getOrm().UpdateSQL();
        }
    }

    //下面是通用模型
    public class MetaModule {
        [Required]
        public string ModuleID { get; set; }
        public string Caption { get; set; }
        public string ParentID { get; set; }
        public string Path { get; set; }
        public string Version { get; set; }
        public List<ModuleFunc> Functions { get; set; }
        public List<ModulePage> ModulePages { get; set; }

        public MetaModule() {
            Functions = new List<ModuleFunc>();
            ModulePages = new List<ModulePage>();
        }

    }

    public class ModuleFunc
    {
        public string FuncID { get; set; }
    }
    
    
    public class MetaFunction
    {
        [Required]
        public string FuncID { get; set; }
        [Required]
        public string FuncName { get; set; }
        public string Version { get; set; }

        public MetaFunction()
        {
        }

    }

    public enum ModulePageType {ptQuery, ptDetail, ptPrint, ptLookup }
    public class ModulePage {
        private string pageflow, pagelookup, pageparams, pagequeries;
        private PageFlowItem[] flows;
        private SmartLookup[] lookups;
        private Dictionary<string, string> _params;
        private string[] queries;

        public string PageID { get; set; }
        public ModulePageType PageType { get; set; }
        public string UI { get; set; }

        public ModulePage()  {
        }

        public string PageParams { get { return pageparams; } set { if (pageparams != value) { pageparams = value; _params = null; } } }
        public string PageFlow { get { return pageflow; } set { if (pageflow != value) { pageflow = value; flows = null; } } }
        public string PageLookup { get { return pagelookup; } set { if (pagelookup != value) { pagelookup = value; lookups = null; } } }
        
        public string Queries { get { return pagequeries; } set { pagequeries = value;} }

        public SmartLookup[] Lookups { get { 
                if (lookups == null ){
                    if (pagelookup != null && !string.IsNullOrEmpty(pagelookup.Trim()))
                        try { lookups = Newtonsoft.Json.JsonConvert.DeserializeObject<SmartLookup[]>(pagelookup); }
                        catch (Exception e) { lookups = null; throw new Exception(string.Format("页面 \"{0}\" 获取关联设置格式出错.\n", PageID) + e.Message); }
                    else lookups = new SmartLookup[]{};
                }
                return lookups; 
            }
            set { lookups = value; }
        }  

        public PageFlowItem[] Flows { get {
                if (flows == null) {
                    if (pageflow != null && !string.IsNullOrEmpty(pageflow.Trim())){
                        try { flows = Newtonsoft.Json.JsonConvert.DeserializeObject<PageFlowItem[]>(pageflow); }
                        catch (Exception e) { flows = null; throw new Exception(string.Format("页面 \"{0}\" 获取流程设置格式出错.\n", PageID) + e.Message); }

                        if (flows != null)
                            foreach (var f in flows) {
                                if (!new System.Text.RegularExpressions.Regex("(^New$)|(^Normal$)|(^Freezed$)|(^Deleted$)|(^Checked$)|(^Checked_\\d*$)").IsMatch(f.ID))
                                    throw new Exception(string.Format("页面 \"{0}\" 流程设置项\"{1}\" 的命名规则不正确（New|Normal|Checked）.\n", PageID, f.ID));
                            }
                    }else flows = null;// new PageFlowItem[] { }; 
                }
                return flows;
            }
        }

        public Dictionary<string, string> PageParam { get {
                if (_params == null) {
                    if (pageparams != null && !string.IsNullOrEmpty(pageparams.Trim()))
                        try { _params = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(pageparams); }
                        catch (Exception e) { _params = null; throw new Exception(string.Format("页面 \"{0}\" 获取页面参数设置格式出错.\n", PageID) + e.Message); }
                    else _params = new Dictionary<string, string>();
                }  
                return _params;
            }
        }

        public string[] QueryList { get {
            if (queries == null)
                queries = pagequeries == null ? null : pagequeries.Trim().Split(';').Where(p=>p.Trim() != string.Empty).ToArray();
            return queries; }  
        }
        public string MainQuery { get { return QueryList == null || QueryList.Count() == 0 ? null : QueryList[0]; } }

        /**--------------待优化-----------------*/
        public string[] Params { get; set; }
        public string QueryString(Dictionary<string, object> paramValues) {
            return Params == null? "":string.Join("&", Params.Select(p=>p + "=" + (paramValues==null?"":(paramValues.ContainsKey(p)?paramValues[p]:""))));
        }
        /**--------------待优化 end ------------*/
    }

    public enum FlowState{fsNew, fsNormal, fsAuditing, fsAudited}
    public class PageFlowAction {
        public string Summary { get; set; }
        public string Biz { get; set; }
        public string Description { get; set; }
    }

    public enum DataEditType { etInsert, etUpdate, etDelete}
    public class PageFlowItem {
        public string ID { get; set; } //New;Normal;Checked_1;Checked_2;Checked
        //public bool Actived { get; set; } //这里是配置，不应该存放状态。
        public string Summary { get; set; }
        public string Description { get; set; }
        public FlowState State { get; set; }
        public string SaveBiz { get; set; } //存放新增和修改的biz。（不属于流程）==null 代表不能保存（修改）
        public List<PageFlowAction> Action { get; set; }
        
        //下面两个属性表示了每个字段是否可以修改以及从表在此状态下是否可以增加删除
        public string[] BlackList { get; set; }
        public string[] WhiteList { get; set; }
        //public string[] ReadOnlyFields { get; set; }
        //public Dictionary<DisabledActionType, int> DisabledAction { get; set; } //代表了从表是否可以增加或者删除
        //end
    }

    public class PageFlow {
        //public string Query { get; set; }
        public List<PageFlowItem> Items { get; set; }

        public PageFlow() {
            Items = new List<PageFlowItem>() ;
        }

        static public string GetCurrentStateID(DataTable table){
            if (!table.Columns.Contains("Checked")) return "Normal";
            else if (Convert.ToBoolean(table.Rows[0]["Checked"])) return "Checked";
            else {
                int i = 1;
                while (i > 0) {
                    if (table.Columns.Contains("Checked_" + i.ToString()))
                    {
                        if (!Convert.ToBoolean(table.Rows[0]["Checked_" + i.ToString()])) return "Checked_" + (i - 1).ToString();
                        else i++;
                    }
                    else return i==1? "Normal": ("Checked_" + (i-1)  .ToString());
                }
            }
            return "Undefined";
        }
    }
}
