using Core;
using Core.Models;
using Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services
{
    public class GCMService: IGCMService
    {
        private readonly IUnitOfWork _unitOfWork;
        public GCMService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public bool DeleteDeviceToken(string Token)
            => _unitOfWork.GCMRepo.DeleteDeviceToken(Token);

        public List<NotificationObj> GetNotificationObjs(string IEMICODE)
            => _unitOfWork.GCMRepo.GetNotificationObjs(IEMICODE);

        //public bool PictureTSizeToDB(string IMEICODE, double TotalSize, int Num, string Type = "ADD")
        //    => _unitOfWork.GCMRepo.PictureTSizeToDB(IMEICODE, TotalSize, Num, Type);

        //public string UpdateSendGCM_Cambea_dayilyStatistics(string IEMICODE, string GCMTYPE)
        //     => _unitOfWork.GCMRepo.UpdateSendGCM_Cambea_dayilyStatistics(IEMICODE, GCMTYPE);

        public bool Update_CAMBEAT_Table(string flagFieldName, string IEMICODE)
            => _unitOfWork.GCMRepo.Update_CAMBEAT_Table(flagFieldName, IEMICODE);
    }
}
