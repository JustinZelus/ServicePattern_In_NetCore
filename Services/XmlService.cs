using Core.Models;
using Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Services
{
    public class XmlService : IXmlService
    {
        public Tuple<bool,List<PushServerKeyID>> ReadAppXML(string fileName,string[] AppName)
        {
            List<PushServerKeyID> AppnameList = new List<PushServerKeyID>();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);//載入xml檔
                XmlNode root = xmlDoc.SelectSingleNode("apps");
                XmlNodeList nodeList = root.ChildNodes;
                foreach (XmlNode xn in nodeList)
                {
                    PushServerKeyID PushServerKeyID = new PushServerKeyID();
                    try
                    {
                        XmlElement xe = (XmlElement)xn;

                        var result = AppName.Where(c => c.Contains(xe.GetAttribute("id")));
                        if (result.Any())   //判斷存在在AppName字串裡
                        {
                            PushServerKeyID.Appname = xe.GetAttribute("id");

                            XmlNodeList subList = xe.ChildNodes;
                            foreach (XmlNode xmlNode in subList)
                            {
                                switch (xmlNode.Name)
                                {
                                    case "p12_Address":
                                        PushServerKeyID.p12_Address = xmlNode.InnerText;
                                        break;
                                    case "p12_PW":
                                        PushServerKeyID.p12_PW = xmlNode.InnerText;
                                        break;
                                    case "FCM_DeviceId":
                                        PushServerKeyID.FCM_DeviceId = xmlNode.InnerText;
                                        break;
                                    case "FCM_SenderID":
                                        PushServerKeyID.FCM_SenderID = xmlNode.InnerText;
                                        break;
                                    case "Sandbox":
                                        PushServerKeyID.Sandbox = Convert.ToBoolean(xmlNode.InnerText);
                                        break;
                                    default:
                                        break;
                                }

                            }
                            AppnameList.Add(PushServerKeyID);
                        }
                    }
                    finally
                    {
                        PushServerKeyID = null;
                    }
                }

                return Tuple.Create(true, AppnameList);
            }
            catch (Exception ex)
            {
                //logger.Info("ReadAppXML:發生錯誤=>" + ex.ToString());
                return Tuple.Create(false, AppnameList);
            }

        }
    }
}
