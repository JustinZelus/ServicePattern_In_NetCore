using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Repositories
{
    public interface IGCMRepository
    {
        List<NotificationObj> GetNotificationObjs(string IEMICODE);
        //string UpdateSendGCM_Cambea_dayilyStatistics(string IEMICODE, string GCMTYPE);
        //bool PictureTSizeToDB(string IMEICODE, double TotalSize, int Num, string Type = "ADD");
        bool DeleteDeviceToken(string token);
        bool Update_CAMBEAT_Table(string flagFieldName, string IEMICODE);
    }
}
