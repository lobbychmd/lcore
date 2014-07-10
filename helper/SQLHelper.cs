using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    public class SQLHelper
    {
        
        private string[] pk;
        private string tableName;
        private string[] fields;
        public int PrefixLength { get; set; }
        public SQLHelper(string tableName) {
            this.tableName = tableName;
        }

        public SQLHelper PK(params string[] field) {
            this.pk = field;
            return this;
        }

        public SQLHelper Fields(string[] field)  {
            fields = field;
            return this;
        }

        public string Select() {
            return string.Format("select {0} from {1} {2} {3}", 
                fields!=null && fields.Count() >0 ?
                    string.Join(", ", fields.Select(p => string.Format("{0}=[{1}]", p, p.Substring(PrefixLength))))
                    :"*",
                tableName,
                pk == null ? "" : "where",
                pk == null ? "" : string.Join(" and ", pk.Select(p=> string.Format("[{0}]=:{1}", p, p))));
        }

        public string Insert() {
            return string.Format("insert {0}({1}) values ({2})", tableName,
                string.Join(" ,", fields.Select (p=>"[" + p + "]")), string.Join(" ,", fields.Select(p => ":" + p.Substring(PrefixLength))));
        }

        public string Update() {
            return string.Format("update {0} set {1} where {2} ", tableName,
                string.Join(", ", fields.Select(p => "[" + p + "] = :" + p.Substring(PrefixLength))),
                string.Join(" and ", pk.Select(p => "[" + p + "] = :" + p.Substring(PrefixLength))));
        }

        public string DeleteAll() {
            return string.Format("delete  from {0}", tableName);
        }

        public string Delete() {
            return string.Format("delete  from {0} {1} {2}", tableName, pk == null ? "" : "where", pk == null ? "" : string.Join(" and ", pk.Select(p => "[" + p + "] = :" + p.Substring(PrefixLength))));
        }

        static public SQLHelper From(string tableName) {
            return new SQLHelper(tableName);
        }


    }
}
