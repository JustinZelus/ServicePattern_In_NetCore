using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Data.Repositories.Sql
{
     
    public class SQLDB
    {
        private string strConnStr1;
        private SqlConnection PublicDB;

        public SQLDB(string ConnectionStrings)
        {
            this.strConnStr1 = ConnectionStrings;
        }

        public SqlConnection OpenDB(int conntype)
        {
            String connstr = "";
            SqlConnection db = null;

            if (conntype == 1)
                connstr = strConnStr1;

            db = new SqlConnection(connstr);

            db.Open();
            return db;

        }

        public async Task<SqlConnection> OpenDBAsync(int conntype)
        {
            string connstr = "";
            SqlConnection db = null;

            if (conntype == 1)
                connstr = strConnStr1;
            try
            {
                db = new SqlConnection(connstr);
                await db.OpenAsync();
            }
            catch (Exception)
            {
                //Log
                return db;
            }

            return db;
        }

        public SqlDataAdapter OpenAD(String sqlstr)
        {
            SqlDataAdapter AD;

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

                AD = new SqlDataAdapter(sqlstr, PublicDB);
                return AD;
            }
            finally
            {
                PublicDB.Close();
                PublicDB.Dispose();
            }
        }

        public SqlDataAdapter OpenSAD(String SQL, SqlConnection DB)
        {
            SqlDataAdapter AD;
            AD = new SqlDataAdapter(SQL, DB);
            return AD;
        }

        public SqlDataReader OpenRD(String SQL)
        {
            SqlCommand Comm = null;
            SqlDataReader RD;

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
                Comm = new SqlCommand(SQL, PublicDB);
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
                SqlConnection.ClearPool(PublicDB);
                PublicDB.Dispose();

            }
        }

        public SqlDataReader OpenSRD(String SQL, SqlConnection DB)
        {
            SqlCommand Comm;
            SqlDataReader RD;
            try
            {
                if (DB.State == ConnectionState.Closed)
                    DB = OpenDB(1);
                Comm = new SqlCommand(SQL, DB);
                RD = Comm.ExecuteReader();
                return RD;
            }
            finally
            {

            }

        }

        public async Task<SqlDataReader> OpenSRDAsync(String SQL, SqlConnection DB)
        {
            SqlCommand Comm;
            SqlDataReader RD = null;
            try
            {
                if (DB.State == ConnectionState.Closed)
                    DB = await OpenDBAsync(1);
                Comm = new SqlCommand(SQL, DB);
                RD = await Comm.ExecuteReaderAsync();

            }
            catch (Exception ex)
            {
                //log
            }
            return RD;
        }

        public object OpenCM(String SQL)
        {
            SqlCommand Comm = null;
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
                Comm = new SqlCommand(SQL, PublicDB);
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
                SqlConnection.ClearPool(PublicDB);
                PublicDB.Dispose();
            }
        }

        public async Task<object> OpenCMAsync(string SQL)
        {
            SqlCommand Comm = null;
            object Objectstr = null;

            try
            {
                if (PublicDB == null)
                {
                    PublicDB = await OpenDBAsync(1);
                }

                if (PublicDB != null)
                {
                    PublicDB.Close();

                    if (PublicDB.State == ConnectionState.Closed)
                    {
                        await PublicDB.OpenAsync();
                    }
                    Comm = new SqlCommand(SQL, PublicDB);
                    Objectstr = await Comm.ExecuteScalarAsync();
                }

            }
            finally
            {
                if (Comm != null)
                {
                    Comm = null;
                }
                PublicDB.Close();
                SqlConnection.ClearPool(PublicDB);
                PublicDB.Dispose();
            }

            return Objectstr;
        }

        public object OpenSCM(String SQL, SqlConnection DB)
        {
            SqlCommand Comm;
            Object Objectstr;

            try
            {
                Comm = new SqlCommand(SQL, DB);
                Objectstr = Comm.ExecuteScalar();
                return Objectstr;
            }
            finally
            {

            }
        }

        public int GetCountSQL(string sqlstr, SqlConnection DB)
        {
            SqlCommand Comm = null;
            SqlDataReader RD = null;


            try
            {
                Comm = new SqlCommand(sqlstr, DB);
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

        public void Dispose()
        {
            if (PublicDB != null)
            {
                PublicDB.Close();

                PublicDB.Dispose();

            }
        }
    }
}
