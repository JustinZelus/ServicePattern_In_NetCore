using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Models
{
    public class PushServiceModel
    {
    }

    public class PushServer_InitialValue
    {
        public string Phone_system { get; set; }
        public string SendGCM_Table_STATUE { get; set; }
        public string SendGCM_Table_GCM_NUM { get; set; }
        public string GCM_Messagecfg_Table_connectionDT { get; set; }
        public string GCM_Messagecfg_Table_connectionDT_UTC { get; set; }
        public string CAMBEA_Table_GCMFlag { get; set; }
        public string SendGCM_Table_STATUE_OTHER { get; set; }
        public string SendGCM_Table_GCM_NUM_OTHER { get; set; }
        public string CAMBEA_Table_GCMFlag_OTHER { get; set; }
        //public string AP_Type { get; set; } = ConfigurationManager.AppSettings["AP_Type"];
        public void Setting(string AP_Type)
        {
            if (AP_Type == "A")
            {
                Phone_system = "android";
                SendGCM_Table_STATUE = "STATUE";
                SendGCM_Table_GCM_NUM = "GCM_NUM";
                GCM_Messagecfg_Table_connectionDT = "connectionDT";
                GCM_Messagecfg_Table_connectionDT_UTC = "connectionDT_UTC";
                CAMBEA_Table_GCMFlag = "androidGCMFlag";

                SendGCM_Table_STATUE_OTHER = "STATUE_IOS";
                SendGCM_Table_GCM_NUM_OTHER = "GCM_NUM_IOS";
                CAMBEA_Table_GCMFlag_OTHER = "iosGCMFlag";
            }
            else
            {
                Phone_system = "ios";
                SendGCM_Table_STATUE = "STATUE_IOS";
                SendGCM_Table_GCM_NUM = "GCM_NUM_IOS";
                GCM_Messagecfg_Table_connectionDT = "connectionDT_ios";
                GCM_Messagecfg_Table_connectionDT_UTC = "connectionDT_ios_UTC";
                CAMBEA_Table_GCMFlag = "iosGCMFlag";

                SendGCM_Table_STATUE_OTHER = "STATUE";
                SendGCM_Table_GCM_NUM_OTHER = "GCM_NUM";
                CAMBEA_Table_GCMFlag_OTHER = "androidGCMFlag";
            }

        }


    }
    public class PushNotification
    {
        //  public Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public List<PushNotificationPayload> CameraPuahParaList { get; set; }
        public string p12_Address { get; set; }
        public string p12_PW { get; set; }
        public Boolean Sandbox { get; set; }
        public string FCM_SenderID { get; set; }
        public string FCM_DeviceId { get; set; }
        public string AP_Type { get; set; }
        public int index { get; set; }
    }
    public class PushNotificationPayload
    {
        public string sound = "default";
        public int badge = 1;
        public string pushTime { get; set; }
        public string deviceToken { get; set; }

        public int ContentAvailable = 1;
        public string Push_No { get; set; }
        public string NickName { get; set; }  //裝置暱稱
        public string IMEICODE { get; set; }
        public int GCM_TYPE { get; set; }
        public string push_server { get; set; }
        public string PushPayloadStr { get; set; }
        public string PushPayload()
        {
            if (push_server == "A" || push_server == "apn")   //APN
            {
                //發給APNs的Payload格式
                //http://min-yeh-ho-blog.logdown.com/posts/1250411-apns-push-json-format
                string[] myArray;
                myArray = new string[2];
                myArray[0] = IMEICODE;
                myArray[1] = NickName;
                return "{\"aps\":" +
                                 "{" +
                                     "\"content-available\":" + ContentAvailable + "," +
                                     " \"badge\":" + badge + "," +
                                     " \"sound\":\"" + sound + "\"," +
                                     " \"loc-args\":\"" + IMEICODE + "\"," +
                                     " \"alert\":" +
                                     "{" +
                                          "\"body\":\"" + IMEICODE + "\"," +
                                          "\"loc-key\":\"" + "POWER_REQUEST_FORMAT" + "\"," +
                                          "\"action-loc-key\":\"" + "0" + "\"," +
                                          "\"loc-args\":" + JsonConvert.SerializeObject(myArray) +
                                     "}" +
                                 "}" +
                       "}";
            }
            else   //安卓(GCM & FCM)
            {
                return IMEICODE + "," + NickName + ",0";   // IMEICODE,裝置名稱,照片數量(無用=0)
            }


        }

    }

    public class PushServerKeyID
    {
        public string Appname { get; set; }
        public string p12_Address { get; set; }
        public string p12_PW { get; set; }

        public bool Sandbox { get; set; }
        public string FCM_DeviceId { get; set; }
        public string FCM_SenderID { get; set; }
    }
}
