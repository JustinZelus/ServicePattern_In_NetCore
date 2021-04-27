using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories.Sql
{
    public class Repository
    {
        protected SQLDB _context;

        public Repository(SQLDB context)
        {
            _context = context;
        }

        public SqlConnection OpenDB(int conntype)
            => _context.OpenDB(conntype);

        public async Task<SqlConnection> OpenDBAsync(int conntype)
            => await _context.OpenDBAsync(conntype);

        public SqlDataAdapter OpenAD(string sqlstr)
            => _context.OpenAD(sqlstr);

        public SqlDataAdapter OpenSAD(string SQL, SqlConnection DB)
            => _context.OpenSAD(SQL, DB);

        public SqlDataReader OpenRD(String SQL)
           => _context.OpenRD(SQL);

        public SqlDataReader OpenSRD(String SQL, SqlConnection DB)
            => _context.OpenSRD(SQL, DB);
        public async Task<SqlDataReader> OpenSRDAsync(String SQL, SqlConnection DB)
            => await _context.OpenSRDAsync(SQL, DB);

        public object OpenCM(String SQL)
            =>  _context.OpenCM(SQL);

        public async Task<object> OpenCMAsync(string SQL)
            => await _context.OpenCMAsync(SQL);

        public object OpenSCM(String SQL, SqlConnection DB)
            => _context.OpenSCM(SQL, DB);
        public int GetCountSQL(string sqlstr, SqlConnection DB)
            => _context.GetCountSQL(sqlstr, DB);
        public void Dispose()
            => _context.Dispose();
        public void SetSQLDBNull()
        {
            _context = null;
        }

        public void ClearPool(SqlConnection db)
        {
            SqlConnection.ClearPool(db);
        }

        public void Execute(SqlConnection Conn,string sqlstr)
        {
            Conn.Execute(sqlstr);
        }
        
    }
}
