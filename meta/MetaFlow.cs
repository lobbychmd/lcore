using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    public class Role : MetaRole {
        private OrmHelper getOrm() {
            return OrmHelper.From("metaRoles").F("RoleID", "RoleName").PK("RoleID").Obj(this).End; 
        }
        
        public Role(string roleID) {
            this.RoleID = roleID;
        }

        public Role Load() {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) if (!VersionHelper.Helper.CheckNewAs<Role>(this, "MetaRole", new[] { "RoleID" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Role \"{0}\" does not exist.", RoleID)); 
            return this;
        }

        public string UpdateSQL() {
            return getOrm().UpdateSQL();
        }
        public void Remove() { getOrm().Dels(); }
        public void Save(){
            getOrm().Save();
        }
    }

    public class MetaRole {
        public string RoleID { get; set; }
        public string RoleName { get; set; }
        public string Version { get; set; }
        public string HashCode { get; set; }
        public MetaRole()
        {
        }

    }

    public class Flow : MetaFlow
    {
        
        private OrmHelper getOrm() {
            return OrmHelper.From("metaFlow").F("FlowID", "FlowSummary", "FlowSetting").PK("FlowID").Obj(this).End;
        }

        public Flow(string flowID)
        {
            this.FlowID = flowID;
        }
        public Flow Load() {
            var loaded = getOrm().Setup();
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0)
                if (!VersionHelper.Helper.CheckNewAs<Flow>(this, "MetaFlow", new[] { "FlowID" }, true)) loaded = getOrm().Setup();
            if (!loaded) throw new Exception(string.Format("Flow \"{0}\" does not exist.", FlowID));
            return this;
        }

        public string UpdateSQL()  {
            return getOrm().UpdateSQL();
        }
        public void Remove() { getOrm().Dels(); }
        public void Save() {
            getOrm().Save();
        }
    }

    public class MetaFlowSettingUser
    {

        public string RoleID { get; set; }
    }
    public class MetaFlowSettingAction {
        public MetaFlowSettingUser[] User { get; set; }
        public string Biz { get; set; }
        public string ReceiptNOField { get; set; }
    }
    public class MetaFlowSetting
    {
        public Dictionary<string, MetaFlowSettingAction> Actions { get; set; }
    }

    public class MetaFlow
    {
        public string FlowID { get; set; }
        public string FlowSummary { get; set; }
        public string FlowSetting {get; set; }
        public string Version { get; set; }
        public string HashCode { get; set; }
        public ParamsHelper Params { get; set; }
        private string _fs {get;set;}
        private MetaFlowSetting fc;

        private System.Collections.Specialized.NameValueCollection _params;
        private Biz biz;
        

        public void SetAction(string action){
            this.biz = new Biz(FlowConfig.Actions[action].Biz).Load();
        }

        public string[] ParamNames { get { return biz.Params.Select(p=>p.ParamName).ToArray(); } }
        public ParamsHelper SmartParams { get { return biz.SmartParams; } }
        public MetaFlowSetting FlowConfig {get{
            if (_fs != FlowSetting) {
                _fs = FlowSetting;
                fc = Newtonsoft.Json.JsonConvert.DeserializeObject<MetaFlowSetting>( _fs );
            }
            return fc;
        }}

        private bool CheckCondition(string action, System.Data.IDbConnection conn, string receiptNO, string user) {
            return FlowConfig.Actions[action].User.Where(p => {
                var dt = l.core.DBHelper.ExecuteQuery(conn, "select 1 from tFlowActionLog where FlowID = :FlowID and Action = :Action and UserNO = :UserNO and ReceiptNO = :ReceiptNO ",
                    new Dictionary<string, DBParam>{
                            {"FlowID", new DBParam{ParamValue = FlowID}},
                            {"Action", new DBParam{ParamValue = action}},
                            {"ReceiptNO", new DBParam{ParamValue = receiptNO}},
                            {"UserNO", new DBParam{ParamValue = user}}
                        });
                return dt.Rows.Count > 0;
            }).Count() == FlowConfig.Actions[action].User.Count();
        }

        public BizResult Execute(string action, string user, string where, System.Collections.Specialized.NameValueCollection _params)
        {
            var flowaction = this.FlowConfig.Actions[action];
            this._params = _params;
            BizResult result = new BizResult { Errors = new List<l.core.BizValidationResult>() };
            using(var conn = l.core.Project.Current.GetConn()){
                var role = l.core.DBHelper.ExecuteQuery(conn, "select RoleID from tAccountRoles where UserNO = :UserNO and PlaceNO = :Where",
                    new Dictionary<string, DBParam>{
                        {"UserNO", new DBParam{ParamValue = user}},
                        {"Where", new DBParam{ParamValue = where}}
                    });
                if ((from System.Data.DataRow dr in role.Rows where dr[0].ToString() == flowaction.User[0].RoleID select 1).Count() == 0)
                    result.Errors.Add(new BizValidationResult ( "当前用户不允许进行此流程" ));
                else {
                    var receiptNO = _params[flowaction.ReceiptNOField];
                    l.core.DBHelper.ExecuteSql(conn,  "insert into tFlowActionLog(FlowID, Action, ReceiptNO, UserNO, At) values (:FlowID, :Action, :ReceiptNO, :UserNO, :At)",
                        new Dictionary<string, DBParam>{
                            {"FlowID", new DBParam{ParamValue = FlowID}},
                            {"Action", new DBParam{ParamValue = action}},
                            {"ReceiptNO", new DBParam{ParamValue = receiptNO}},
                            {"UserNO", new DBParam{ParamValue = user}},
                            {"At", new DBParam{ParamValue = DateTime.Now}}
                        });
                    if (CheckCondition(action, conn, receiptNO, user)) {
                        return biz.Execute(conn);
                    }
                }
            }
            return result;
        }
    }
}
