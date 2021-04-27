using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Services
{
    public interface IGCMService
    {
        //string UpdateSendGCM_Cambea_dayilyStatistics(string IEMICODE, string GCMTYPE);
        //bool PictureTSizeToDB(string IMEICODE, double TotalSize, int Num, string Type = "ADD");
        List<NotificationObj> GetNotificationObjs(string IEMICODE);
        bool DeleteDeviceToken(string Token);
        bool Update_CAMBEAT_Table(string flagFieldName, string IEMICODE);
    }
}
