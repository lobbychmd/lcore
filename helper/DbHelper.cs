using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.Odbc;
using FirebirdSql.Data.FirebirdClient;
using System.Data.Common;
using l.core;
using System.Text.RegularExpressions;

namespace l.core
{
    public class DBParam
    {
        private object _paramValue;
        public string ParamName { get; set; }
        public Int32 Length { get; set; }
        virtual public object ParamValue
        {
            get { return _paramValue; }
            set
            {
                _paramValue = value ?? DBNull.Value;
            }
        }
        public DbType DbType { get; set; }
        public DBParam()
        {
            DbType = DbType.Object;
        }

        static private void SetSqlCmdParam(DbCommand cmd, Dictionary<string, DBParam> dbParams, string paramName) {
            if (!dbParams.ContainsKey(paramName)) return;

            var v = dbParams[paramName].ParamValue == null ? DBNull.Value : dbParams[paramName].ParamValue;
            if (cmd is SqlCommand)
            {
                var p = (cmd as SqlCommand).Parameters.AddWithValue(paramName, v);
                if (dbParams[paramName].DbType != DbType.Object) p.DbType = dbParams[paramName].DbType;
                if (p.DbType == DbType.Binary)
                {
                    p.SqlDbType = SqlDbType.Binary;
                    p.Size = dbParams[paramName].Length;
                }
                //if (p.DbType == DbType.String) p.SqlDbType = SqlDbType.VarChar;
            }
            else if (cmd is OdbcCommand)
            {
                var p = (cmd as OdbcCommand).Parameters.AddWithValue(paramName, v);
                if (dbParams[paramName].DbType != DbType.Object) p.DbType = dbParams[paramName].DbType;
                //if (p.DbType == DbType.String) p.SqlDbType = SqlDbType.VarChar;
            }
            else (cmd as FbCommand).Parameters.AddWithValue(paramName, v).DbType = dbParams[paramName].DbType;
        }
        
        static public void SetSqlCmdParams(DbCommand cmd, Dictionary<string, DBParam> dbParams)
        {
            if (dbParams == null) return;
            //if ((cmd is OdbcCommand) && ((cmd.Connection as OdbcConnection)).Driver== "Adaptive Server Enterprise") {
            if (true){
                foreach (Match m in new Regex("@([_a-zA-Z][_a-zA-Z0-9]*)").Matches(cmd.CommandText))
                    if((from System.Data.Common.DbParameter p in cmd.Parameters where p.ParameterName == m.Value.Substring(1) select p).Count() == 0) 
                        SetSqlCmdParam(cmd, dbParams, m.Value.Substring(1));
            }
            else foreach (var i in dbParams) {
                SetSqlCmdParam(cmd, dbParams, i.Key);
            }
        }
    }

    public class DBHelper
    {
        static public ConnectionStringSettingsCollection connConfig = System.Configuration.ConfigurationManager.ConnectionStrings;
        static public string DefaultConnectionStringPrefix = "SQLConnString";

        static public IDbConnection GetConnection(int index) {
            return GetConnection(DefaultConnectionStringPrefix + index.ToString());
        }
        static public IDbConnection GetConnection(string key) {
            var conn = connConfig[key];
            if (conn == null) throw new Exception("没有设置数据连接[" + key + "]");
            return GetConnection(conn.ProviderName, conn.ConnectionString);
        }

        static public IDbConnection GetConnection(string type, string connString) {
            return GetConnection(new ConnectionStringSettings { ProviderName = type, ConnectionString = connString });
        }

        static public IDbConnection GetConnection(ConnectionStringSettings conn) {
            System.Data.IDbConnection connection = null;
            if (conn.ProviderName.IndexOf("FirebirdSql") == 0)
                connection = new FirebirdSql.Data.FirebirdClient.FbConnection(conn.ConnectionString);
            else if (conn.ProviderName.IndexOf("System.Data.Odbc") == 0)
                connection = new OdbcConnection(conn.ConnectionString);
            else connection = new SqlConnection(conn.ConnectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();

            return connection;
        }

        static private string handleParamPrefix(string SQL){
            return new System.Text.RegularExpressions.Regex(":([_a-zA-Z][_a-zA-Z0-9]*)").Replace(SQL, @"@$1");
        }

        static private void Log (string title, IDbConnection connection, string sql, Dictionary<string, DBParam> dbParams, int rowCount){
            l.core.LogHelper.Log(title + ": " + sql.Summary(), new { DB = connection.Database, SQL = sql, Params = dbParams == null ? null : dbParams.ToList(), RowsCount = rowCount }, who: "system"); 
        }

        private static DbCommand createCmd (IDbConnection conn, string sql){
            DbCommand cmd =  conn is SqlConnection? new SqlCommand(sql, conn as SqlConnection):(
                conn is FbConnection? new FbCommand(sql, conn as FbConnection) as DbCommand: (conn is OdbcConnection? new OdbcCommand(sql, conn as OdbcConnection): null));
            cmd.CommandTimeout = conn.ConnectionTimeout;
            return cmd;
        }

        private static DbDataAdapter createAdapter(IDbConnection conn, DbCommand cmd) {
            return conn is SqlConnection ? new SqlDataAdapter(cmd as SqlCommand) : (
                conn is FbConnection ? new FbDataAdapter(cmd as FbCommand) as DbDataAdapter : (conn is OdbcConnection ? new OdbcDataAdapter(cmd as OdbcCommand) : null));
        }

        static public DataTable ExecuteQuery(IDbConnection connection, string sql, Dictionary<string, DBParam> dbParams,
            int startRecord = 0, int maxRecords = 0) {
                sql = handleParamPrefix(sql);
            DbCommand cmd = createCmd(connection, sql);
            DbDataAdapter da = createAdapter(connection, cmd);
            
            DBParam.SetSqlCmdParams(cmd, dbParams);
            cmd.Connection = (DbConnection) connection;
            cmd.Transaction = (DbTransaction) TranscationMgr.GetTransObject(connection);

            DataTable dt = new DataTable();
            if ((startRecord == 0)&&( maxRecords == 0))
                da.Fill(dt);
            else da.Fill(startRecord, maxRecords, dt);

            Log("prepare ExecuteQuery", connection, sql, dbParams, dt.Rows.Count); 
            return dt;
        }

        static public int ExecuteSql(IDbConnection connection, string sql, Dictionary<string, DBParam> dbParams) {
            sql = handleParamPrefix(sql);
            DbCommand cmd = createCmd(connection, sql);
            DBParam.SetSqlCmdParams(cmd, dbParams);
            //cmd.Connection = (SqlConnection)connection;
            TranscationMgr.SetCmdTrans(TranscationMgr.GetTransObject(connection), cmd);

            int c = cmd.ExecuteNonQuery();
            Log("prepare ExecuteSQL", connection, sql, dbParams, c); 
            return c;
        }

        static public IDbTransaction GetTranscation(IDbConnection connection) {
            var t = ((IDbConnection)connection).BeginTransaction();
            TranscationMgr.SetTransObject(connection, t);
            return t;
        }

        static public void CommitTranscation(IDbTransaction tranction) {
            ((IDbTransaction)tranction).Commit();
        }

        static public void RollbackTranscation(IDbTransaction tranction) {
            ((IDbTransaction)tranction).Rollback();
        }
    }
    public class TranscationMgr
    {
        private static Dictionary<IDbConnection, IDbTransaction> trans = new Dictionary<IDbConnection, IDbTransaction>();
        static public void SetTransObject(IDbConnection conn, IDbTransaction tran) {
            trans[conn] = tran;
        }

        static public IDbTransaction GetTransObject(IDbConnection conn) {
            return trans.ContainsKey(conn) ? trans[conn] : null;
        }

        static public void SetCmdTrans(IDbTransaction tran, IDbCommand cmd) {
            if (cmd is SqlCommand) cmd.Transaction = (SqlTransaction)tran;
            else if (cmd is OdbcCommand) cmd.Transaction = (OdbcTransaction)tran;
            else if (cmd is FbCommand) cmd.Transaction = (FbTransaction)tran;
        }
    }
}
