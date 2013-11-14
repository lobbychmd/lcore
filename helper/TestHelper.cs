using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace l.core
{
    public class FixtureHelper {

        public void Insert(l.core.MetaTable table, List<Dictionary<string, object>> Data) { 
        
        }

        //插入几条记录
        public void Insert(l.core.MetaTable table, int rowCount)
        {

        }
    
    }
    public class Fixture
    {
        public string FixtureName { get; set; }
        public string TableName { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }

        public void Insert() {
            
        }

        public void Copy() {
            using (var conn = DBHelper.GetConnection(1)) {
                var sh = SQLHelper.From(TableName);
                DBHelper.ExecuteSql(conn, sh.DeleteAll(), null);
                if (Data.Count >0){
                    sh.Fields(Data[0].Keys.ToArray());
                    Data.ForEach(p=>
                        DBHelper.ExecuteSql(conn, sh.Insert(), p.Keys.ToDictionary(
                            p1=>p1, q=> new DBParam{ ParamValue = p[q]}
                        ))
                    );
                }
            }
        }
    }
}
