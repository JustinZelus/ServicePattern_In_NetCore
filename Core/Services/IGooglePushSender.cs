using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IGooglePushSender
    {
        void Google_SendPushNotification(string deviceToken,
                                                                        string PushPayloadStr,
                                                                        string FCM_SenderID,
                                                                        string FCM_DeviceId,
                                                                        Action failueCallback = null);
    }
}
