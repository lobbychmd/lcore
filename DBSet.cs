using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;

//这个类用来封装 一个兼容 Sql 和 NoSql 的数据模型
namespace System.Data
{

    public interface ITables : System.Collections.IEnumerable {
        ITable this[int index] { get; }
        int Count();
        
    }

    public interface ITable : System.Collections.IEnumerable
    {
        IDictionary<string, object> this[int index] { get; }
        System.Collections.Generic.IEnumerable<string> Keys { get; }
        int Count();
        string TableName { get; }
        /*IDictionary<string, object>*/ void NewRow();
    }

    public class NewDataSet : ITables {
        public System.Data.DataSet Doc { get; set; }

        public IEnumerator GetEnumerator()
        {
            foreach (DataTable dt in Doc.Tables)
            {
                yield return dt.Table() ;
            }
        }

        public ITable this[int index]
        {
            get {
            return Doc.Tables[index].Table();
        } }

        public int Count() {
            return Doc.Tables.Count;
        }
    }

    public class NewDataTable : ITable
    {
        public System.Data.DataTable Table { get; set; }
        public IDictionary<string, object> this[int index] {
            get { return (from System.Data.DataColumn dc in Table.Columns select dc.ColumnName).ToDictionary(p=>p, q=> Table.Rows[index][q]); }
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Table.Rows.Count; i++ )
            {
                yield return this[i];
            }
        }

        public System.Collections.Generic.IEnumerable<string> Keys { get {
            return from System.Data.DataColumn dc in Table.Columns select dc.ColumnName;
        } }

        public int Count() { return Table.Rows.Count; }

        public void NewRow() {
            var newrow = Table.NewRow();
            Table.Rows.Add(newrow);
            //return this[Table.Rows.Count - 1];
        }

        public string TableName { get { return Table.TableName; }  }
    }

    static public class NewDataSetHelper
    {
        static public ITables Tables(this System.Data.DataSet ds)
        {
            return new NewDataSet { Doc = ds };
        }

        static public ITable Table(this System.Data.DataTable table)
        {
            return new NewDataTable { Table = table };
        }
    }

    /// <summary>
    /// ///////////////////////////////////////////mongodb
    /// </summary>

    public class NewMongoDoc : ITables
    {
        public IEnumerable<MongoDB.Bson.BsonDocument> Doc { get; set; }
        public string MainType { get; set; }
        public Dictionary<string, string> SubTypes { get; set; }
        public Dictionary<string, ITable> SubTables { get; set; }
        //private List<MongoDB.Bson.BsonDocument> list = null;

        public IEnumerator GetEnumerator()
        {
            yield return Doc.Table(MainType);
            if (SubTypes!=null)
                for (int i = 0; i < SubTypes.Count; i++ ) {
                    yield return this[i + 1];
                }
        }

        public NewMongoDoc() {
            SubTables = new Dictionary<string, ITable>();
        }

        public ITable this[int index]
        {
            get
            {
                if (index == 0) return  Doc.Table(MainType) ;
                else {
                    var keys = SubTypes.Keys.ToArray();
                    var key = keys[index - 1];
                    if (!SubTables.ContainsKey(key)){
                        var value = Doc.First()[key];
                        if (value is BsonNull) {
                            value = new BsonArray();
                            Doc.First()[key] = value;
                        }
                        SubTables[key] = (value as BsonArray).Table(SubTypes[key], "Table" + index.ToString());
                    }
                    return SubTables[key];
                }
            }
        }

        public int Count() {
            return (SubTypes == null? 0:SubTypes.Count()) + 1;
        }
    }

    public class NewMongoElement : ITable
    {
        //public MongoDB.Driver.MongoCursor<MongoDB.Bson.BsonDocument> Table { get; set; }
        public IEnumerable<MongoDB.Bson.BsonDocument> Table { get; set; }
        private List<MongoDB.Bson.BsonDocument> list = null;
        public string Type { get; set; }

        private object getValue(BsonValue value) {
            return value.IsValidDateTime ? value.AsDateTime : (value.IsObjectId? value.ToString(): value.RawValue);
        }
        public IEnumerator GetEnumerator()
        {
            if (list == null)
                foreach (var row in Table){
                    yield return row.ToDictionary(p => p.Name, q => getValue(q.Value));
                }
            else foreach (var row in list) {
                yield return row.ToDictionary(p => p.Name, q => getValue(q.Value));
                }
        }

        public IDictionary<string, object> this[int index] {
            get {
                if (list == null) list = Table.ToList();
                return list[index].ToDictionary(p => p.Name, q => getValue(q.Value));
            }
        }

        public System.Collections.Generic.IEnumerable<string> Keys {
            get {
                if (this.Count() > 0)
                    return Table.First().Elements.Select(p => p.Name);
                else return Type.Split(';');
                    //Type.GetProperties().Where(p=>!p.PropertyType.IsArray)./*Where(p=> p.Name != "_id").*/Select(p => p.Name);
            }
        }

        public int Count() { if (list == null) list = Table.ToList(); return list.Count(); }

        public void NewRow() {
            if (list == null) list = Table.ToList();
            var newRow = new BsonDocument ();
            foreach (string key in Keys) newRow[key] = BsonNull.Value; 
            list.Add(newRow);
        }

        public string TableName { get; set; }
    }



    static public class NewMongoDocHelper1
    {
        static public ITables Tables(this IEnumerable<MongoDB.Bson.BsonDocument> mainTable, string MainType,
            Dictionary<string, string> SubTypes = null )
        {
            return new NewMongoDoc { Doc = mainTable, MainType = MainType, SubTypes = SubTypes };
        }

        static public ITable Table(this IEnumerable<MongoDB.Bson.BsonDocument> table, string type)
        {
            return new NewMongoElement { Table = table, Type = type };
        }

        

        static public ITable Table(this MongoDB.Bson.BsonArray table, string type, string tableName)
        {
            return new NewMongoElement { Table = table.Select(p => p.AsBsonDocument), Type = type, TableName = tableName };
        }
    } 

}
    