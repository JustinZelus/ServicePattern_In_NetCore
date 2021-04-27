using Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using PushSharp.Apple;
using PushSharp.Google;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private Logger _logger { get; set; }
        public PushNotificationService(Logger logger)
        {
            this._logger = logger;
        }
        public void Google_SendPushNotification(string deviceToken, 
                                                                        string PushPayloadStr,
                                                                        string FCM_SenderID,
                                                                        string FCM_DeviceId,
                                                                        Action failueCallback = null)
        {
            //string errorMessage = "";
            //bool isSuccess = true; 
            try
            {
                string RegistrationID = deviceToken;   //TOKEN
                string url = "https://fcm.googleapis.com/fcm/send";
                string API_Key = FCM_DeviceId.Trim();
                string applicationID = FCM_SenderID.Trim();
                string message = PushPayloadStr;
                Object postData = new
                {
                    to = RegistrationID,
                    priority = "high",
                    data = new
                    {
                        message = message    //message這個tag要讓前端開發人員知道
                    }
                };
          
                //準備對GCM/FCM Server發出Http post 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json;charset=utf-8;";
                request.Headers.Add(string.Format("Authorization: key={0}", API_Key));
                request.Headers.Add(string.Format("Sender: id={0}", applicationID));

                string p = JsonConvert.SerializeObject(postData);//將Linq to json轉為字串 
                byte[] byteArray = Encoding.UTF8.GetBytes(p);//要發送的字串轉為byte[] 
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                //發出Request 
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string responseStr = reader.ReadToEnd();
                reader.Close();
                responseStream.Close();
                response.Close();
                JObject obj = (JObject)JsonConvert.DeserializeObject(responseStr);

                if (Convert.ToInt32(obj["failure"].ToString()) > 0)
                {
                   // isSuccess = false;
                    obj = (JObject)obj["results"][0];
                    var errorMessage = obj["error"].ToString();
                    _logger.Info("google推播失敗:{0}", errorMessage);
                    if (errorMessage == "InvalidRegistration" || errorMessage == "NotRegistered")
                        failueCallback?.Invoke();
                }
                else
                    _logger.Info("google推播成功");
            }
            catch (Exception ex)
            {
                _logger.Info("google推播錯誤:{0}", ex.Message);
            }
          //  return Tuple.Create(isSuccess,errorMessage);
        }

        public void IOS_SendPushNotification(string deviceToken,
                                                                        string PushPayloadStr,
                                                                        string p12_Address,
                                                                        string p12_PW, 
                                                                        Action expiredCallback = null)
        {
            //string errorMessage = "";
            //bool isSuccess = true;

            var appleCert = File.ReadAllBytes(p12_Address);
            var config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Production, appleCert, p12_PW);
            var fbs = new FeedbackService(config);
            fbs.FeedbackReceived += (string devicToken, DateTime timestamp) =>
            {
                expiredCallback?.Invoke();
            };
            fbs.Check();//檢查金鑰是否過期

            var apnsBroker = new ApnsServiceBroker(config);
            apnsBroker.OnNotificationFailed += (notification, aggregateEx) =>
            {
               // isSuccess = false;
               
                aggregateEx.Handle(ex => {
                    var errorMessage = "";
                    // See what kind of exception it was to further diagnose
                    if (ex is ApnsNotificationException)
                    {
                        var notificationException = (ApnsNotificationException)ex;

                        // Deal with the failed notification
                        var apnsNotification = notificationException.Notification;
                        var statusCode = notificationException.ErrorStatusCode;

                        errorMessage = $"Apple Notification Failed: ID={apnsNotification.Identifier}, Code={statusCode}";

                    }
                    else
                    {
                        // Inner exception might hold more useful information like an ApnsConnectionException			
                        errorMessage = $"Apple Notification Failed for some unknown reason : {ex.InnerException}";
                    }
                    _logger.Info("Apple推播失敗:{0}", errorMessage);
                    // Mark it as handled
                    return true;
                });
            };

            apnsBroker.OnNotificationSucceeded += (notification) =>
            {
                //  isSuccess = true;
                _logger.Info("Apple推播成功");
            };

            // Start the broker
            apnsBroker.Start();
            apnsBroker.QueueNotification(new ApnsNotification
            {
                DeviceToken = deviceToken,
                Payload = JObject.Parse(PushPayloadStr)
            });
            apnsBroker.Stop();
          //  yield return Tuple.Create(isSuccess, errorMessage);
        }

      
    }
}
