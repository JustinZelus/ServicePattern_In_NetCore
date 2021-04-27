using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IApplePushSender
    {
        void IOS_SendPushNotification(string deviceToken,
                                                                         string PushPayloadStr,
                                                                         string p12_Address,
                                                                         string p12_PW,
                                                                         Action expiredCallback = null);
    }
}
