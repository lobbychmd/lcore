using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Data;

namespace l.core
{
    public class Table : MetaTable {
        public string HashCode { get; set; }
        //private MetaTable clone;
        public Table(string tableName) {
            this.TableName = tableName;
        }

        private void checkHashCode() {
            if (!string.IsNullOrEmpty(HashCode))
                System.Diagnostics.Debug.Assert(
                    l.core.ScriptHelper.GetHashCode(this) == HashCode, "校验码错误.");
        }

        private OrmHelper getOrm() {
            return OrmHelper.From("metaTable").F("TableName", "TableSummary", "HashCode", "Version").PK("TblName").MF("TableName", "TblName")
                .MF("TableSummary", "TblSummary").Obj(this).End
                .SubFrom("metaTableColumn").MF("TableName", "TblName").MF("Selection", null).Obj( Columns).End
                .SubFrom("metaTableIndex").MF("TableName", "TblName").MF("Cols", null).Obj(Indexes).End;
        }

        public Table Load() {
            var loaded = getOrm().Setup();
            //clone = Newtonsoft.Json.JsonConvert.DeserializeObject<MetaTable>(Newtonsoft.Json.JsonConvert.SerializeObject(this));
            
            if (VersionHelper.Helper != null && VersionHelper.Helper.Action.IndexOf("update") >= 0) {
                var newtb = VersionHelper.Helper.GetAs<Table>("MetaTable", new Dictionary<string, string> { { "TableName", TableName } }) as Table;
                if (Sync(null, newtb)) {
                    if (!VersionHelper.Helper.CheckNewAs<Table>(this, "MetaTable", new[] { "TableName" }, true)) 
                        loaded = getOrm().Setup();
                }
                //Sync(null);
            }
            if (!loaded) throw new Exception(string.Format("Table \"{0}\" does not exist.", TableName));
            else if (Columns.Count == 0) throw new Exception(string.Format("Table \"{0}\" does not include any columns.", TableName));
            //checkHashCode();
            return this;
        }
        public void Remove() { getOrm().Dels(); }
        public void Save( ) {
            //SaveAndSync(null);
            getOrm().Save( );
        }

        public void SaveAndSync( IDbConnection conn)  {
            getOrm().Saves(p => {
                Sync(conn, null); return true;
            });
                 
        }
        public bool Sync( IDbConnection conn, Table newTable)  {
            var connection = conn == null ? Project.Current == null ? DBHelper.GetConnection(1) : Project.Current.GetConn(  null ) : null;
            try{
                string sql = Columns.Count == 0 ? newTable. Create()
                    : string.Join("\n", newTable.Columns.Where(p1 => {
                                var pold = Columns.Find(p2 => p2.ColumnName == p1.ColumnName);
                                return (pold != null) && (pold.AllowNull != p1.AllowNull || pold.IsIdentity != p1.IsIdentity || pold.Precision != p1.Precision || pold.Scale != p1.Scale || pold.Size != p1.Size || pold.Type != p1.Type);
                                }).Select(c => string.Format("alter table {0} alter column {1} {2} {3} {4}  ",
                            TableName, c.ColumnName, GetDBType(c), GetDBTypeEx(c),c.AllowNull ? "null" : "not null"
                            ))) +
                    string.Join("\n", newTable.Columns.Where(p1 => Columns.Find(p2 => p2.ColumnName == p1.ColumnName) == null).Select(c => string.Format("alter table {0} add {1} {2} {3} {4}  ",
                            TableName, c.ColumnName, GetDBType(c), GetDBTypeEx(c), c.AllowNull ? "null" : "not null"
                            ))) +
                        string.Join("\n", Columns.Where(p1 => newTable.Columns.Find(p2 => p2.ColumnName == p1.ColumnName) == null).Select(c => string.Format("alter table {0} drop column {1}   ",
                            TableName, c.ColumnName
                            ))) + 
                        ((Indexes.Find(p=>p.PrimaryKey ) == null)?
                          "\n" +string.Join("\n", newTable.Indexes.Where(p1 => p1.PrimaryKey ).Select(c => string.Format("   ALTER TABLE {0} ADD PRIMARY KEY CLUSTERED ({1})", TableName, String.Join(",", c.Cols()))))
                        :"") + "\n" + string.Join("\n", newTable.Indexes.Where(p1 => !p1.PrimaryKey && Indexes.Find(p2 => !p2.PrimaryKey && p2.IndexName == p1.IndexName) == null).Select(c =>
                            string.Format("   CREATE {3} NONCLUSTERED INDEX {0} ON {1} ({2})",
                                c.IndexName, TableName, String.Join(",", c.Cols()), c.IsUnique ? "UNIQUE" : "")))
                        ;
                if (!string.IsNullOrEmpty(sql))
                    try { 
                        DBHelper.ExecuteSql(conn ?? connection, sql, null);
                        return true;
                    }
                    catch (Exception e) {
                        throw new Exception(e.Message + "\n" + sql);
                    }
                else return true;
            }
            finally {
                if (conn == null) connection.Dispose();
            }
                 
        }
        static public void UpdateAll(IDbConnection conn) { 
            if (l.core.VersionHelper.Helper != null)   {
                l.core.MetaTable[] tables = l.core.VersionHelper.Helper.GetAs<l.core.MetaTable[]>("MetaTable.category", null) as l.core.MetaTable[];
                foreach( var i in tables)
                    new Table(i.TableName).Load().SaveAndSync(conn);
            }
        }
    }

    public class MetaTable {
        public MetaTable() {
            Columns = new List<MetaColumn>();
            Indexes = new List<MetaIndex>();
        }
        
        [Required]
        public string TableName { get; set; }
        public string At { get; set; }
        public string TableSummary { get; set; }
        //public bool SysTable { get; set; }
        public List<MetaColumn> Columns {get;set;}
        public List<MetaIndex> Indexes {get;set;}
        public string Version { get; set; }

        [Required, DisplayName("主键")]
        public MetaIndex PrimaryKey { get {
            MetaIndex pkey = Indexes.Find(p => p.PrimaryKey);
            //if (pkey == null) throw new Exception(string.Format("表结构 {0} 没有设置主键，无法自动获取update sql.", TableName));
            return pkey;
        } }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var i in Indexes){
                if (string.IsNullOrEmpty(i.Columns))
                    yield return new ValidationResult("索引必须包含字段", new[] { "Indexies" });
            }

        }

        public string GetDBType(MetaColumn i){
            return i.Type == ColumnType.ctStr ? (i.Size > 8000 ? "Text" : "Varchar") :
                        (i.Type == ColumnType.ctBinary ? "Image" :
                            (i.Type == ColumnType.ctBool ? "bit" :
                                (i.Type == ColumnType.ctDateTime ? "DateTime" :
                                    (i.Type == ColumnType.ctNumber ? (
                                        i.Scale == 0 ? "int" : "Decimal") : "")
                                )
                            )
                            );
        }

        public string GetDBTypeEx(MetaColumn i)
        {
            return i.Type == ColumnType.ctNumber ? (
                                i.Scale == 0 ? "" : string.Format("({0}, {1})", i.Precision, i.Scale)) : (
                                    (i.Size > 0) && (i.Size <= 8000) ? string.Format("({0})", i.Size) : "");
        }
        //生成创建表的 sql
        public string Create() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Create Table {0} (", TableName));
            foreach (var i in Columns)  {
                sb.AppendLine(string.Format("   {0} {1} {2} {3} {4},", i.ColumnName,
                    GetDBType(i), GetDBTypeEx(i) ,
                            i.AllowNull ? "Null" : "Not Null", i.IsIdentity ? "identity" : ""
                ));
            }
            //foreach (var i in Indexes.Where(p=> p.PrimaryKey && p.Cols().Count() > 0 )) {
            //    sb.AppendLine(string.Format("   Primary key({0})", String.Join(",", Indexes[0].Cols())));
           // }
            sb.AppendLine(")");

            foreach (var i in Indexes.Where(p=> p.PrimaryKey && p.Cols().Count() > 0 )) {
                sb.AppendLine(string.Format("   ALTER TABLE {0} ADD PRIMARY KEY CLUSTERED ({1})", TableName, String.Join(",", i.Cols())));
            }
            foreach (var i in Indexes.Where(p => !p.PrimaryKey && p.Cols().Count() > 0)) {
                sb.AppendLine(string.Format("   CREATE {3} NONCLUSTERED INDEX {0} ON {1} ({2})",
                    i.IndexName, TableName, String.Join(",", i.Cols()), i.IsUnique ? "UNIQUE" : ""));
            }
            //if (exec)
            //    using (var conn = DbHelper.GetConnection(SysTable ? 0 : 1))
            //    {
            //        DbHelper.ExecuteSql(conn, sb.ToString(), null);
            //    }
            return sb.ToString();
        }
        
    }

    public class MetaColumn
    {
        [Required]
        public string ColumnName { get; set; }
        public string Caption { get; set; }
        public string Summary { get; set; }
        [Required]
        public l.core.ColumnType Type { get; set; }
        public int? Size { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool AllowNull { get; set; }
        public bool IsIdentity { get; set; }
        public string Selection { get; set; }
        
    }

    public class MetaIndex
    {
        [Required]
        public string IndexName { get; set; }
        public bool PrimaryKey { get; set; }
        public bool IsUnique { get; set; }

        public string[] Cols()
        {
            return Columns.Split(';');
        }
        public string Columns { get; set; }
    }
}