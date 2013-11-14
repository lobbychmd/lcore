using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace l.core
{
    public static  class DsHelper
    {
        static public DataColumn FindColumn(this  DataTable dt , string columnName){
            if (!dt.Columns.Contains(columnName)) throw new Exception(string.Format("table {0} not contains column:{1}", dt.TableName, columnName));
            else return dt.Columns[columnName];
        }
    }

    
}
