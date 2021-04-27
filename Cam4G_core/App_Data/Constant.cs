using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cam4G_core.App_Data
{
    public class Constant
    {

        public static Dictionary<string, string> GetIEMISubDirectories(string siemicode)
        {
            Dictionary<string, string>  _IEMISubDirectories = new Dictionary<string, string>();
            string dirPath = Config.GetSetting("FilePath");
            _IEMISubDirectories.Add("picture", dirPath + "\\" + siemicode + "\\picture"); //圖片
            _IEMISubDirectories.Add("movie", dirPath + "\\" + siemicode + "\\movie"); //影像
            _IEMISubDirectories.Add("zip", dirPath + "\\" + siemicode + "\\zip");  //壓縮檔
            _IEMISubDirectories.Add("Temp", dirPath + "\\" + siemicode + "\\Temp");  //暫存轉檔
            return _IEMISubDirectories;
        }
    }
}
