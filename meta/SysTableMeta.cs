using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using l.core;

namespace l.core
{
    public class SysTableMeta
    {
        static public List<MetaTable> SysTables = new List<MetaTable> { 
            new MetaTable{ TableSummary = "metaTable", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "TblName", Caption = "表名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "TblSummary", Caption = "表说明", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                }, Indexes = new List<MetaIndex> {
                    new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "TblName"}
                }
            },
            new MetaTable{ TableName = "metaTableColumn", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "TblName", Caption = "表名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ColumnName", Caption = "字段名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "Caption", Caption = "简称", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "Type", Caption = "类型", Type = ColumnType.ctNumber, Precision = 1, Scale=0, AllowNull = false},
                new MetaColumn{ ColumnName = "Size", Caption = "长度", Type = ColumnType.ctNumber, Precision = 8, Scale = 0, AllowNull = false},
                new MetaColumn{ ColumnName = "Precision", Caption = "精度", Type = ColumnType.ctNumber, Precision = 8, Scale = 0, AllowNull = false},
                new MetaColumn{ ColumnName = "Scale", Caption = "小数位", Type = ColumnType.ctNumber, Precision = 8, Scale = 0, AllowNull = false},
                new MetaColumn{ ColumnName = "AllowNull", Caption = "允许为空", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "IsIdentity", Caption = "自增", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "Summary", Caption = "说明", Type = ColumnType.ctStr, Size = 1024, AllowNull = true},

            }, Indexes = new List<MetaIndex>{
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "TblName;ColumnName"}
            }},
            new MetaTable{ TableName = "metaTableIndex", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "TblName", Caption = "表名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "IndexName", Caption = "索引名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "PrimaryKey", Caption = "主键", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "IsUnique", Caption = "唯一", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "Columns", Caption = "字段", Type = ColumnType.ctStr, AllowNull = false},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "TblName;IndexName"}
            }},
            new MetaTable{ TableName = "metaBiz",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "BizID", Caption = "业务逻辑名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ForMeta", Caption = "系统", Type = ColumnType.ctBool, AllowNull = false},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "BizID"}
            }},
            new MetaTable{ TableName = "metaBizItems",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "BizID", Caption = "业务逻辑名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ProcIdx", Caption = "序号", Type = ColumnType.ctNumber, Precision=3, Scale=0, AllowNull = false},
                new MetaColumn{ ColumnName = "ProcSQL", Caption = "SQL", Type = ColumnType.ctStr, Size= 8001, AllowNull = false},
                new MetaColumn{ ColumnName = "InterActive", Caption = "交互", Type = ColumnType.ctBool,   AllowNull = false},
                new MetaColumn{ ColumnName = "ProcRepeated", Caption = "重复", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "ProcEnabled", Caption = "启用", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "ProcSummary", Caption = "摘要 ", Type = ColumnType.ctStr, Size=255, AllowNull = false},
                new MetaColumn{ ColumnName = "ExpectedRows", Caption = "行数", Type = ColumnType.ctNumber, Precision=1, Scale=0, AllowNull = false},
                new MetaColumn{ ColumnName = "ProcUpdateFlag", Caption = "更新标志", Type = ColumnType.ctStr, Size =10,   AllowNull = true},
                new MetaColumn{ ColumnName = "ProcExecuteFlag", Caption = "执行标志", Type = ColumnType.ctStr, Size =10,    Scale=0, AllowNull = true},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "BizID;ProcIdx"}
            }},
            new MetaTable{ TableName = "metaBizChecks",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "BizID", Caption = "业务逻辑名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "CheckIdx", Caption = "序号", Type = ColumnType.ctNumber, Precision=3, Scale=0, AllowNull = false},
                new MetaColumn{ ColumnName = "CheckRepeated", Caption = "重复", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "CheckSummary", Caption = "摘要 ", Type = ColumnType.ctStr, Size=255, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamToValidate", Caption = "检查参数 ", Type = ColumnType.ctStr, Size=40, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamToCompare", Caption = "比较参数 ", Type = ColumnType.ctStr, Size=40, AllowNull = true},
                new MetaColumn{ ColumnName = "ValidateType", Caption = "检查类型 ", Type = ColumnType.ctStr, Size=40, AllowNull = false},
                new MetaColumn{ ColumnName = "CheckSQL", Caption = "SQL", Type = ColumnType.ctStr, Size= 8001, AllowNull = true},
                new MetaColumn{ ColumnName = "CheckEnabled", Caption = "启用", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "CheckUpdateFlag", Caption = "更新标志", Type = ColumnType.ctStr, Size =10,   AllowNull = true},
                new MetaColumn{ ColumnName = "CheckExecuteFlag", Caption = "执行标志", Type = ColumnType.ctStr, Size =10,    Scale=0, AllowNull = true},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "BizID;CheckIdx"}
            }},
            new MetaTable{ TableName = "metaBizParams",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "BizID", Caption = "业务逻辑名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamName", Caption = "参数名", Type = ColumnType.ctStr, Size=40, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamType", Caption = "数据类型", Type = ColumnType.ctNumber, Precision = 1, Scale=0, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamRepeated", Caption = "重复", Type = ColumnType.ctBool, AllowNull = false},
                new MetaColumn{ ColumnName = "Output", Caption = "输出", Type = ColumnType.ctBool, AllowNull = false},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "BizID;ParamName"}
            }},
            new MetaTable{ TableName = "metaQuery",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "QueryName", Caption = "查询名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "QueryType", Caption = "查询类型", Type = ColumnType.ctNumber, Precision = 1, Scale = 0, AllowNull = false},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "QueryName"}
            }},
            new MetaTable{ TableName = "metaQueryScript", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "QueryName", Caption = "查询名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ScriptIdx", Caption = "序号", Type = ColumnType.ctNumber, Precision = 2, Scale = 0, AllowNull = false},
                new MetaColumn{ ColumnName = "Script", Caption = "SQL", Type = ColumnType.ctStr, Size = 8001, AllowNull = false},
                new MetaColumn{ ColumnName = "Sum", Caption = "Sum", Type = ColumnType.ctStr, Size = 8001, AllowNull = true},
                new MetaColumn{ ColumnName = "MetaColumn", Caption = "元数据", Type = ColumnType.ctStr,  Size = 1024, AllowNull = true},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "QueryName;ScriptIdx"}
            }},
            new MetaTable{ TableName = "metaQueryParams",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "QueryName", Caption = "查询名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamIdx", Caption = "序号", Type = ColumnType.ctNumber, Precision = 3, Scale = 0, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamType", Caption = "数据类型", Type = ColumnType.ctNumber, Precision = 1, Scale=0, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamName", Caption = "参数名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ParamGroups", Caption = "分组", Type = ColumnType.ctStr, Size = 255, AllowNull = true},
                new MetaColumn{ ColumnName = "LikeLeft", Caption = "%-", Type = ColumnType.ctBool,   AllowNull = false},
                new MetaColumn{ ColumnName = "LikeRight", Caption = "-%", Type = ColumnType.ctBool,   AllowNull = false},
                new MetaColumn{ ColumnName = "IsNull", Caption = "当未赋值", Type = ColumnType.ctStr, Size=40,  AllowNull = true},

            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "QueryName;ParamIdx"}
            }},
            new MetaTable{ TableName = "metaModule",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "ModuleID", Caption = "模块ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "ModuleName", Caption = "模块名称", Type = ColumnType.ctStr, Size = 80, AllowNull = false},
                new MetaColumn{ ColumnName = "ParentID", Caption = "上级模块", Type = ColumnType.ctStr, Size = 40, AllowNull = true},
                new MetaColumn{ ColumnName = "Path", Caption = "模块路径", Type = ColumnType.ctStr, Size = 255, AllowNull = true}
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "ModuleID"}
            }},
            new MetaTable{ TableName = "metaModulePage",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "ModuleID", Caption = "模块ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "PageID", Caption = "页面ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "PageType", Caption = "页面类型", Type = ColumnType.ctStr, Size = 40, AllowNull = true},
                new MetaColumn{ ColumnName = "PageQuery", Caption = "数据源", Type = ColumnType.ctStr, Size = 1024, AllowNull = true},
                new MetaColumn{ ColumnName = "PageBiz", Caption = "业务逻辑", Type = ColumnType.ctStr, Size = 1024, AllowNull = true},
                new MetaColumn{ ColumnName = "PageUI", Caption = "页面UI", Type = ColumnType.ctStr, Size = 2048, AllowNull = true},
                new MetaColumn{ ColumnName = "PageFlow", Caption = "页面流程", Type = ColumnType.ctStr, Size = 2048, AllowNull = true},
                new MetaColumn{ ColumnName = "PageLookup", Caption = "关联", Type = ColumnType.ctStr, Size = 2048, AllowNull = true},
                new MetaColumn{ ColumnName = "PageParams", Caption = "页面参数", Type = ColumnType.ctStr, Size = 2048, AllowNull = true},
                new MetaColumn{ ColumnName = "TimeStamp", Caption = "时间戳", Type = ColumnType.ctDateTime, AllowNull = true },
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "ModuleID;PageID"}
            }},
            new MetaTable{ TableName = "metaColumn",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "ColumnName", Caption = "字段名", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "At", Caption = "情景", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "Caption", Caption = "标题", Type = ColumnType.ctStr, Size = 80, AllowNull = true},
                new MetaColumn{ ColumnName = "Format", Caption = "格式", Type = ColumnType.ctStr, Size = 255, AllowNull = true},
                new MetaColumn{ ColumnName = "RangeFrom", Caption = "开始范围", Type = ColumnType.ctStr, Size = 255, AllowNull = true},
                new MetaColumn{ ColumnName = "RangeTo", Caption = "结束范围", Type = ColumnType.ctStr, Size = 255, AllowNull = true},
                new MetaColumn{ ColumnName = "CharLength", Caption = "录入字符长度", Type = ColumnType.ctNumber, Precision =8, Scale = 0, AllowNull = true},
                new MetaColumn{ ColumnName = "Inherited", Caption = "父类", Type = ColumnType.ctStr, Size = 40, AllowNull = true},
                new MetaColumn{ ColumnName = "AllowInherite", Caption = "允许继承", Type = ColumnType.ctBool,   AllowNull = true},
                new MetaColumn{ ColumnName = "DicNO", Caption = "可选字典项", Type = ColumnType.ctStr, Size = 40,  AllowNull = true},
                new MetaColumn{ ColumnName = "Selection", Caption = "可选项", Type = ColumnType.ctStr, Size = 1024, AllowNull = true},
                new MetaColumn{ ColumnName = "TimeStamp", Caption = "时间戳", Type = ColumnType.ctDateTime, AllowNull = true },
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "ColumnName;At"}
            }},
            new MetaTable{ TableName = "metaFunction",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "FuncID", Caption = "功能ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "FuncName", Caption = "功能名称", Type = ColumnType.ctStr, Size = 80, AllowNull = false},
                new MetaColumn{ ColumnName = "TimeStamp", Caption = "时间戳", Type = ColumnType.ctDateTime, AllowNull = true},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "FuncID"}
            }},
            new MetaTable{ TableName = "metaModuleFunc",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "ModuleID", Caption = "模块ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "FuncID", Caption = "功能ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                //new MetaColumn{ ColumnName = "TimeStamp", Caption = "时间戳", Type = ColumnType.ctDateTime, AllowNull = true},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "ModuleID;FuncID"}
            }},
            new MetaTable{ TableName = "metaDataSubscribeSrv",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "Interval", Caption = "间隔时间", Type = ColumnType.ctNumber,  AllowNull = false},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "SrvCode"}
            }},
            ////////////////////////---------------------------------------------------


            new MetaTable{ TableName = "tRoles", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "RoleID", Caption = "角色ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "RoleName", Caption = "角色名", Type = ColumnType.ctStr, Size = 80, AllowNull = false},
                new MetaColumn{ ColumnName = "TimeStamp", Caption = "时间戳", Type = ColumnType.ctDateTime, AllowNull = true },
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "RoleID"}
            }},
            new MetaTable{ TableName = "tRoleFunc", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "RoleID", Caption = "角色ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "FuncID", Caption = "功能ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "TimeStamp", Caption = "时间戳", Type = ColumnType.ctDateTime, AllowNull = true},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "RoleID;FuncID"}
            }},
            new MetaTable{ TableName = "tMenu",  Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "ParentID", Caption = "上级模块", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "Idx", Caption = "顺序", Type = ColumnType.ctNumber, Precision =3, Scale = 0, AllowNull = false},
                new MetaColumn{ ColumnName = "ModuleID", Caption = "模块ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                //new MetaColumn{ ColumnName = "Caption", Caption = "模块名称", Type = ColumnType.ctStr, Size = 80, AllowNull = false},
                //new MetaColumn{ ColumnName = "Path", Caption = "模块路径", Type = ColumnType.ctStr, Size = 255, AllowNull = true}
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "ParentID;Idx"}
            }},
            new MetaTable{ TableName = "tAccount", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "UserNO", Caption = "用户NO", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "UserName", Caption = "用户名", Type = ColumnType.ctStr, Size = 80, AllowNull = false},
                new MetaColumn{ ColumnName = "Password", Caption = "密码", Type = ColumnType.ctStr, Size = 255, AllowNull = false},
                new MetaColumn{ ColumnName = "Tag", Caption = "Tag", Type = ColumnType.ctNumber, Precision=2, Scale=0, AllowNull = false},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "UserNO"}
            }},
            new MetaTable{ TableName = "tAccountRoles", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "UserNO", Caption = "用户NO", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "RoleID", Caption = "职务ID", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "PlaceNO", Caption = "系统位置", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "UserNO;RoleID;PlaceNO"}
            }},
            new MetaTable{ TableName = "tSysPlace", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "PlaceNO", Caption = "系统位置编号", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "Place", Caption = "系统位置", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "SystemType", Caption = "系统类型", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "PlaceNO"}
            }},
            new MetaTable{ TableName = "tAccountSetting", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "UserNO", Caption = "用户NO", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "SettingKey", Caption = "设定标识", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "SettingValue", Caption = "设定", Type = ColumnType.ctStr, Size = 2048, AllowNull = true},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "UserNO;SettingKey"}
            }},

            
            
            #region 字典
            new MetaTable{ TableName = "tDic", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "DicNO", Caption = "字典编号", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "DicDesc", Caption = "字典描述", Type = ColumnType.ctStr, Size = 80, AllowNull = false},
                new MetaColumn{ ColumnName = "DicLength", Caption = "最大长度", Type = ColumnType.ctNumber, Precision =1, Scale = 0, AllowNull = true},
                new MetaColumn{ ColumnName = "SysDic", Caption = "是否系统参数", Type = ColumnType.ctBool, AllowNull = false},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "DicNO"}
            }},
            new MetaTable{ TableName = "tDicDtl", Columns = new  List<MetaColumn>{
                new MetaColumn{ ColumnName = "DicNO", Caption = "字典编号", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "DicDtlNO", Caption = "标识", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
                new MetaColumn{ ColumnName = "DicDtlValue", Caption = "标识", Type = ColumnType.ctStr, Size = 40, AllowNull = false},
            }, Indexes = new List<MetaIndex>{ 
                new MetaIndex{ PrimaryKey = true, IsUnique = true, Columns = "DicNO;DicDtlNO"}
            }},
            #endregion 字典

        

        };

        static public void Create() {
            foreach (var i in SysTables)
                try { 
                    i.Create(); 
                }catch{
                    
                }
        }
    }
}
