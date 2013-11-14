using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace l.core
{
    public struct SqlHelperSetting {
        public bool FieldPrefix { get; set; }
    }

    public class SqlHelper
    {
        static public SqlHelperSetting Setting;

        static public string SelectSql(string tableName, List<string> where, List<string> fields = null, bool like = false, string filterColumn = null){
            var sql = new StringBuilder();
            var selFields = string.Empty;
            if (fields != null) {
                foreach (string s in fields)
                    selFields += (Setting.FieldPrefix ? (s + " as " + s.Substring(1)) : s) + ",";
            }
            else selFields = "*,";
            selFields = selFields.Substring(0, selFields.Length - 1);

            sql.AppendLine(string.Format("select {2} from {0} {1}", tableName, ((where==null)||(where.Count == 0)) ? "" : " where ", selFields));

            if (where != null) {
                if (filterColumn != null) where.Add(filterColumn);
                for (var i = 0; i < where.Count; i++)
                    sql.Append(where[i] + (like ? " like " : "=") + " @" + (Setting.FieldPrefix ? where[i].Substring(1) : where[i]) + (i == where.Count - 1 ? "" : " and "));
            }
            return sql.ToString();
        }

        static public string UpdateSql(string tableName, List<string> updateFields, List<string> keyFields)
        {
            var sql = new StringBuilder();
            sql.AppendLine(string.Format("update {0} set ", tableName));
            for (var i = 0; i < updateFields.Count; i++)
                sql.AppendLine(string.Format("     {0} = @{1}{2}", updateFields[i], updateFields[i], (i == updateFields.Count - 1 ? "" : ", ")));
            sql.AppendLine("where");

            for (var i = 0; i < keyFields.Count; i++)
                sql.AppendLine(string.Format("     {0} = @{1} {2}", keyFields[i], keyFields[i], (i == keyFields.Count - 1 ? "" : " and ")));
            return sql.ToString();
        }
        
        static public string InsertSql(string tableName, List<string> Fields)
        {
            var sql = new StringBuilder();
            sql.AppendLine(string.Format("insert {0} ( ", tableName));
            for (var i = 0; i < Fields.Count; i++)
                sql.AppendLine(string.Format("  {0}{1}", Fields[i], (i == Fields.Count - 1 ? ")" : ", ")));
            sql.AppendLine(" values (");

            for (var i = 0; i < Fields.Count; i++)
                sql.AppendLine(string.Format("  @{0}{1}", Fields[i], (i == Fields.Count - 1 ? ")" : ", ")));
            return sql.ToString();
        }
    }
}
