using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Data.Repositories.MySql
{
    public class MySQLDB
    {
        private string strConnStr1;
        private MySqlConnection PublicDB;
        public MySQLDB(string ConnectionStrings)
        {
            this.strConnStr1 = ConnectionStrings;
        }

        public MySqlConnection OpenDB(int conntype)
        {
            String connstr = "";
            MySqlConnection db = null;

            if (conntype == 1)
                connstr = strConnStr1;

            db = new MySqlConnection(connstr);

            db.Open();
            return db;

        }

        public MySqlDataAdapter OpenAD(String sqlstr)
        {
            MySqlDataAdapter AD;

            try
            {
                if (PublicDB == null)
                {
                    PublicDB = OpenDB(1);
                }

                PublicDB.Close();

                if (PublicDB.State == ConnectionState.Closed)
                {
                    PublicDB.Open();
                }

                AD = new MySqlDataAdapter(sqlstr, PublicDB);
                return AD;
            }
            finally
            {
                PublicDB.Close();
                PublicDB.Dispose();
            }
        }

        public MySqlDataAdapter OpenSAD(String SQL, MySqlConnection DB)
        {
            MySqlDataAdapter AD;
            AD = new MySqlDataAdapter(SQL, DB);
            return AD;
        }

        public MySqlDataReader OpenRD(String SQL)
        {
            MySqlCommand Comm = null;
            MySqlDataReader RD;

            try
            {
                if (PublicDB == null)
                {
                    PublicDB = OpenDB(1);
                }

                PublicDB.Close();

                if (PublicDB.State == ConnectionState.Closed)
                {
                    PublicDB.Open();
                }
                Comm = new MySqlCommand(SQL, PublicDB);
                RD = Comm.ExecuteReader();
                return RD;
            }
            finally
            {
                if (Comm != null)
                {
                    Comm = null;
                }
                PublicDB.Close();
                MySqlConnection.ClearPool(PublicDB);
                PublicDB.Dispose();

            }
        }

        public MySqlDataReader OpenSRD(String SQL, MySqlConnection DB)
        {
            MySqlCommand Comm;
            MySqlDataReader RD;
            try
            {
                if (DB.State == ConnectionState.Closed)
                    DB = OpenDB(1);
                Comm = new MySqlCommand(SQL, DB);
                RD = Comm.ExecuteReader();
                return RD;
            }
            finally
            {

            }

        }

        public object OpenCM(String SQL)
        {
            MySqlCommand Comm = null;
            Object Objectstr;

            try
            {
                if (PublicDB == null)
                {
                    PublicDB = OpenDB(1);
                }

                PublicDB.Close();

                if (PublicDB.State == ConnectionState.Closed)
                {
                    PublicDB.Open();
                }
                Comm = new MySqlCommand(SQL, PublicDB);
                Objectstr = Comm.ExecuteScalar();
                return Objectstr;


            }
            finally
            {
                if (Comm != null)
                {
                    Comm = null;
                }
                PublicDB.Close();
                MySqlConnection.ClearPool(PublicDB);
                PublicDB.Dispose();
            }


        }

        public object OpenSCM(String SQL, MySqlConnection DB)
        {
            MySqlCommand Comm;
            Object Objectstr;

            try
            {
                Comm = new MySqlCommand(SQL, DB);
                Objectstr = Comm.ExecuteScalar();
                return Objectstr;
            }
            finally
            {

            }
        }

        public int GetCountSQL(string sqlstr, MySqlConnection DB)
        {
            MySqlCommand Comm = null;
            MySqlDataReader RD = null;


            try
            {
                Comm = new MySqlCommand(sqlstr, DB);
                RD = Comm.ExecuteReader();
                if (RD == null)
                {
                    return -1;
                }
                if (RD.Read())
                {
                    return RD.GetInt32(0);
                }
                else
                {
                    return 0;
                }
            }
            finally
            {
                if (RD != null)
                {
                    RD.Close();
                    RD = null;
                }
                if (Comm != null)
                {
                    Comm = null;
                }

            }
        }
    }
}
