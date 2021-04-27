using Core.Models;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Cam4G_core.App_Data;
using Cam4G_core.Models;
using Newtonsoft.Json.Linq;

namespace Cam4G_core.Controllers
{
    /// <summary>
    /// 推播相關服務
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PushNotificationController: ControllerBase
    {
        private Logger _logger;
        private readonly IPushNotificationService _pushService;
        private readonly IGCMService _gCMService;
        private readonly IXmlService _xmlService;
        private readonly IWebHostEnvironment _env;

        public static List<PushServerKeyID> _AppnameList;
        public delegate void DeleteDeviceTokenDelegate(JObject obj);

        public PushNotificationController(IGCMService gCMService, 
                                          IPushNotificationService pushNotificationService,
                                          IXmlService xmlService,
                                          IWebHostEnvironment env)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _gCMService = gCMService;
            _xmlService = xmlService;
            _pushService = pushNotificationService;
            _env = env;
           
        }

        /// <summary>
        /// 從資料庫取得需推播之IEMI後，做google及apple推播服務
        /// </summary>
        /// <param name="IEMICODE"></param>
        /// <returns></returns>
        [Route("Push")]
        public async Task<bool> Push([FromForm] string IEMICODE)
        {
            bool result = true;
           
            //從資料庫取得推播數量
            List<NotificationObj> sendNotificationObjs = _gCMService.GetNotificationObjs(IEMICODE);
            _logger.Info(IEMICODE + "需要發送推播的數量為:{0}", sendNotificationObjs.Count);

            //初始化裝置推播config
            if (_AppnameList == null)
                InitAppnameList();

            int PushIndex = 0;

            //輪詢每台裝置
            for (int i = 0; i < _AppnameList.Count; i++)
            {
                List<PushNotificationPayload> CameraPuahParaList = new List<PushNotificationPayload>();
                try
                {
                    PushIndex = PushIndex + 1;
                    foreach (var obj in sendNotificationObjs)
                    {
                        PushNotificationPayload PushPayload = new PushNotificationPayload();
                        PushPayload.IMEICODE = obj.IMEICODE.Trim();
                        PushPayload.deviceToken = obj.Token.Trim();
                        PushPayload.NickName = obj.CAMEREA_NAME.Trim();
                        PushPayload.Push_No = PushIndex.ToString();
                        //PushPayload.GCM_TYPE = GCM_TYPE;
                        PushPayload.pushTime = DateTime.UtcNow.AddHours(8).ToString("yyyy/MM/dd HH:mm:ss");
                        PushPayload.push_server = obj.PushServer.Trim();
                        PushPayload.PushPayloadStr = PushPayload.PushPayload();
                        CameraPuahParaList.Add(PushPayload);
                    }

                    //發送推播
                    foreach (var pushPayload in CameraPuahParaList)
                    {
                        if (pushPayload.push_server.Equals("F")) 
                        {
                            _logger.Info("開始發送Goole推播{0}", IEMICODE);
                             _pushService.Google_SendPushNotification(pushPayload.deviceToken,
                                                                     pushPayload.PushPayloadStr,
                                                                     _AppnameList[i].FCM_SenderID,
                                                                     _AppnameList[i].FCM_DeviceId, 
                                                                     new Action(() => _gCMService.DeleteDeviceToken(pushPayload.deviceToken)));
                            _logger.Info("發送Goole推播結束{0}", IEMICODE);
                    
                            _gCMService.Update_CAMBEAT_Table("androidGCMFlag", IEMICODE);
                            _logger.Info("更新CAMBEA androidGCMFlag {0}", IEMICODE);
                        }
                        else if (pushPayload.push_server.Equals("A")) 
                        {
                            _logger.Info("開始發送Apple推播 {0}", IEMICODE);
                            _pushService.IOS_SendPushNotification(pushPayload.deviceToken,
                                                                pushPayload.PushPayloadStr,
                                                                _AppnameList[i].p12_Address,
                                                                _AppnameList[i].p12_PW,
                                                                new Action(() => _gCMService.DeleteDeviceToken(pushPayload.deviceToken)));
                            _logger.Info("發送Apple推播結束 {0}", IEMICODE);

                            _gCMService.Update_CAMBEAT_Table("iosGCMFlag", IEMICODE);
                            _logger.Info("更新CAMBEA iosGCMFlag {0}", IEMICODE);
                        }

                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    _logger.Info("PushNotification錯誤:{0}", ex.Message);
                    result = false;
                    continue;
                }
            }

            return result;
        }

        private void InitAppnameList()
        {
            string webRootPath = _env.WebRootPath;
            string fileName = Path.GetFullPath(Path.Combine(webRootPath, "AppNameBindKey.xml"));
            string[] appName = Config.GetSetting("PushNotification:xmlAppID").Split(',');
            (bool isFinished, List<PushServerKeyID> appnameList) = _xmlService.ReadAppXML(fileName, appName);

            List<PushServerKeyID> appnameList1 = appnameList.Select(o => new PushServerKeyID
            {
                Appname = o.Appname,
                p12_Address = webRootPath + o.p12_Address,
                p12_PW = o.p12_PW,
                FCM_DeviceId = o.FCM_DeviceId,
                FCM_SenderID = o.FCM_SenderID,
                Sandbox = o.Sandbox
            }).ToList();

            _AppnameList = appnameList1;
        }
    }
}
