using Core.Models;
using Core.Repositories;
using Dapper;
using Data.Repositories.MySql;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class GCMRepository : MySQLRepository, IGCMRepository
    {
        private Logger _logger { get; set; }
        public GCMRepository(MySQLDB context, Logger logger) : base(context)
        {
            _logger = logger;
        }

        public List<NotificationObj> GetNotificationObjs(string IEMICODE)
        {
            string sql = "select B.IMEICODE, B.Token, C.CAMEREA_NAME, B.PushServer, C.MEMBER_NO ";
                   sql += "from gcm_imei B, cambea C ";
                   sql += "where B.IMEICODE = C.IEMICODE ";
                   sql += "and B.IMEICODE = " + "'" + IEMICODE + "'";
                   sql += " and B.Token <> ''";
                            
            List<NotificationObj> notificationObj = new List<NotificationObj>();
            try
            {
                using (var conn = OpenDB(1))
                {
                    notificationObj = conn.Query<NotificationObj>(sql).AsList();
                }
            }
            catch (Exception ex)
            {
                _logger.Info("取得推播token錯誤{0}",ex.Message);
            }
            return notificationObj;
        }

        //public bool PictureTSizeToDB(string IMEICODE, double TotalSize, int Num, string Type = "ADD")
        //{
        //    bool send = false;
        //    string sqlstr = "";
        //    var db = _context.OpenDB(1);
        //    try
        //    {
        //        if (Type == "DEL")
        //        {
        //            sqlstr = "INSERT INTO delpicturesizelog(IMEICODE, Datetime,Size,Num)         " +
        //                     "VALUES ('" + IMEICODE + "',UTC_TIMESTAMP(),'" + TotalSize + "','" + Num + "')";
        //        }
        //        else
        //        {
        //            sqlstr = "INSERT INTO uppicturesizelog(IMEICODE, Datetime,Size,Num)         " +
        //                        "VALUES ('" + IMEICODE + "',UTC_TIMESTAMP(),'" + TotalSize + "','" + Num + "')";
        //        }


        //        _context.OpenSCM(sqlstr, db);
        //    }
        //    finally
        //    {

        //        db.Close();
        //        ClearPool(db);
        //        // SqlConnection.ClearPool(db);
        //        db.Dispose();
        //       //SetSQLDBNull();
        //        //DBMan = null;
        //    }


        //    return send;
        //}

        //public string UpdateSendGCM_Cambea_dayilyStatistics(string IEMICODE, string GCMTYPE)
        //{
        //    // Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //    var result = "";
        //    try
        //    {
        //        string sqlstr = "Declare @icount int " +
        //            " select @icount=isnull(count(*),0) From  SendGCM " +
        //            " where IMEICODE='" + IEMICODE + "'" +
        //            "   and GCM_TYPE=" + GCMTYPE +
        //            " if (@icount=0) " +
        //            " Insert into SendGCM(IMEICODE,CDATE,GCM_TYPE,STATUE,STATUE_IOS) Values('" +
        //                     IEMICODE + "',getutcdate(),'" + GCMTYPE + "',0,0)" +
        //            " else         " +
        //            " Update SendGCM set STATUE = 0,STATUE_IOS=0 " +
        //            " Where IMEICODE='" + IEMICODE + "'" +
        //            " and GCM_TYPE=" + GCMTYPE;
        //        using (var Conn = _context.OpenDB(1))
        //        {
        //            Execute(Conn, sqlstr);
        //            //  logger.Info(IEMICODE + "設定資料庫PUSH旗標成功!!");


        //            //更新APP取得檔案數量GetFileNum的旗標0&Last_ConnectionTime&統計每天上傳次
        //            sqlstr = "Update  Cambea  Set  ISUpdateing=0," +
        //                     "                     Last_ConnectionTime=getutcdate()" +
        //                     " Where IEMICODE='" + IEMICODE + "'" +
        //                     " Update dayilyStatistics Set Device_upload=Device_upload+1,  " +
        //                     "                            Device_GCM=Device_GCM+1         " +
        //                     " Where ReportDate=convert(char(8),getutcdate(),112)   ";


        //            //Conn.Execute(sqlstr);
        //            Execute(Conn, sqlstr);
        //            // logger.Info(IEMICODE + "設定統計PUSH每天上傳次數成功");
        //        }
        //        result = "0";
        //    }
        //    catch (Exception ex)
        //    {
        //        //  logger.Info("END(AA):" + IEMICODE + ":攔截例外---" + ex);
        //        result = "AA";
        //    }
        //    return result;
        //}

        public bool DeleteDeviceToken(string token)
        {
            if (string.IsNullOrEmpty(token.Trim()))
                return false;

            try
            {
                using (var conn = OpenDB(1))
                {
                    string sql = "delete gcm_imei ";
                           sql += " where TOKEN='" + token.Trim() + "'";
                           sql += " delete gcm_email ";
                           sql += " where TOKEN='" + token.Trim() + "'";
                    conn.Execute(sql);
                }
            }
            catch (Exception ex)
            {
                _logger.Info("刪除推播token錯誤:{0}", ex.Message);
            }
            return true;
        }

        public bool Update_CAMBEAT_Table(string flagFieldName, string IEMICODE)
        {
            if (string.IsNullOrEmpty(flagFieldName) || string.IsNullOrEmpty(IEMICODE))
                return false;

            try
            {
                using (var conn = OpenDB(1))
                {
                    string sql = "update cambea Set gcmtime=UTC_DATE()," + flagFieldName + "=1";
                            sql += " where IEMICODE='" + IEMICODE + "'";

                    _logger.Info("sql: " + sql);
                    conn.Execute(sql);
                }
            }
            catch (Exception ex)
            {

                _logger.Info("更新cambea錯誤:{0}", ex.Message);
            }
            return true;
        }
    }
}
