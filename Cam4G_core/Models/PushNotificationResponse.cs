using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cam4G_core.Models
{
    public class PushNotificationResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public bool IsDeleteDeviceToken { get; set; }
    }
}
