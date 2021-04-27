using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.MySql
{
    public class MySQLRepository
    {
        protected MySQLDB _context;

        public MySQLRepository(MySQLDB context)
        {
            _context = context;
        }
        public MySqlConnection OpenDB(int conntype)
            => _context.OpenDB(conntype);
        public MySqlDataAdapter OpenAD(String sqlstr)
            => _context.OpenAD(sqlstr);
        public MySqlDataAdapter OpenSAD(String SQL, MySqlConnection DB)
            => _context.OpenSAD(SQL, DB);
        public MySqlDataReader OpenRD(String SQL)
            => _context.OpenRD(SQL);
        public MySqlDataReader OpenSRD(String SQL, MySqlConnection DB)
            => _context.OpenSRD(SQL, DB);
        public object OpenCM(String SQL)
            => _context.OpenCM(SQL);
        public object OpenSCM(String SQL, MySqlConnection DB)
            => _context.OpenSCM(SQL, DB);
        public int GetCountSQL(string sqlstr, MySqlConnection DB)
            => _context.GetCountSQL(sqlstr, DB);
    }
}
