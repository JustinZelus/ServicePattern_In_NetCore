using System;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace Cam4G_core.App_Data
{
    public class FTPClient
    {

        #region Construc
        /// <summary>
        /// Construc
        /// </summary>
        public FTPClient()
        {
            strRemoteHost = "";
            strRemotePath = "";
            strRemoteUser = "";
            strRemotePass = "";
            strRemotePort = 21;
            bConnected = false;
        }
        /// <summary>
        /// Construc
        /// </summary>
        public FTPClient(string remoteHost, string remotePath, string remoteUser, string remotePass, int remotePort)
        {
            strRemoteHost = remoteHost;
            strRemotePath = remotePath;
            strRemoteUser = remoteUser;
            strRemotePass = remotePass;
            strRemotePort = remotePort;
            //Connect();
        }
        #endregion
        #region Loging Property
        /// <summary>
        /// FTP Server IP Address
        /// </summary>
        private string strRemoteHost;
        public string RemoteHost
        {
            get
            {
                return strRemoteHost;
            }
            set
            {
                strRemoteHost = value;
            }
        }
        /// <summary>
        /// FTP Server Port
        /// </summary>
        private int strRemotePort;
        public int RemotePort
        {
            get
            {
                return strRemotePort;
            }
            set
            {
                strRemotePort = value;
            }
        }
        /// <summary>
        /// Server Folder
        /// </summary>
        private string strRemotePath;
        public string RemotePath
        {
            get
            {
                return strRemotePath;
            }
            set
            {
                strRemotePath = value;
            }
        }
        /// <summary>
        /// User帳號
        /// </summary> 
        private string strRemoteUser;
        public string RemoteUser
        {
            set
            {
                strRemoteUser = value;
            }
        }
        /// <summary>
        /// Password
        /// </summary>
        private string strRemotePass;
        public string RemotePass
        {
            set
            {
                strRemotePass = value;
            }
        }
        /// <summary>
        /// 是否已登入
        /// </summary>
        private Boolean bConnected;
        public bool Connected
        {
            get
            {
                return bConnected;
            }
        }
        #endregion
        #region Connect
        /// <summary>
        /// 建立連線
        /// </summary>
        public Boolean Connect()
        {
            IPAddress ip;
            ip = Dns.GetHostEntry(RemoteHost).AddressList[0];
            socketControl = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(ip, strRemotePort);
            // 連線
            try
            {
                socketControl.Connect(ep);
            }
            catch (Exception)
            {
                return false;
                //throw new IOException("Couldn't connect to remote server");
            }
            // 取得回應
            ReadReply();
            if (iReplyCode != 220)
            {
                DisConnect();
                return false;
                //throw new IOException(strReply.Substring(4));
            }
            // 登入
            SendCommand("USER " + strRemoteUser);
            if (!(iReplyCode == 331 || iReplyCode == 230))
            {
                CloseSocketConnect();//如果有錯誤就關閉連線
                return false;
                //throw new IOException(strReply.Substring(4));
            }
            if (iReplyCode != 230)
            {
                SendCommand("PASS " + strRemotePass);
                if (!(iReplyCode == 230 || iReplyCode == 202))
                {
                    CloseSocketConnect();
                    return false;
                    //throw new IOException(strReply.Substring(4));
                }
            }
            bConnected = true;
            // 切換到所選的目錄
            //ChDir(strRemotePath);
            return true;
        }

        /// <summary>
        /// 關閉連線
        /// </summary>
        public void DisConnect()
        {
            if (socketControl != null)
            {
                SendCommand("QUIT");
            }
            CloseSocketConnect();
        }
        #endregion
        #region 傳輸
        /// <summary>
        /// 傳輸模式
        /// </summary>
        public enum TransferType { Binary, ASCII };
        /// <summary>
        /// 設定傳輸模式
        /// </summary>
        /// <param name="ttType">傳輸模式</param>
        public void SetTransferType(TransferType ttType)
        {
            if (ttType == TransferType.Binary)
            {
                SendCommand("TYPE I");//binary
            }
            else
            {
                SendCommand("TYPE A");//ASCII
            }
            if (iReplyCode != 200)
            {
                throw new IOException(strReply.Substring(4));
            }
            else
            {
                trType = ttType;
            }
        }
        /// <summary>
        /// 取得傳輸模式
        /// </summary>
        /// <returns>傳輸模式</returns>
        public TransferType GetTransferType()
        {
            return trType;
        }
        #endregion
        #region Data
        /// <summary>
        /// 取得文件列表
        /// </summary>
        /// <param name="strMask">文件名稱~可用*</param>
        public string[] Dir(string strMask)
        {
            // 先確定是否已連線
            if (!bConnected)
            {
                Connect();
            }
            //建立並行數據連接的socket
            Socket socketData = CreateDataSocket();
            //傳送指令
            SendCommand("NLST " + strMask);
            //分析回應碼
            if (!(iReplyCode == 150 || iReplyCode == 125 || iReplyCode == 226))
            {
                throw new IOException(strReply.Substring(4));
            }
            //取得結果
            strMsg = "";
            while (true)
            {
                int iBytes = socketData.Receive(buffer, buffer.Length, 0);
                strMsg += ASCII.GetString(buffer, 0, iBytes);
                if (iBytes < buffer.Length)
                {
                    break;
                }
            }
            char[] seperator = { '\n' };
            string[] strsFileList = strMsg.Split(seperator);
            socketData.Close();//數據socket關閉時也會有回應馬
            if (iReplyCode != 226)
            {
                ReadReply();
                if (iReplyCode != 226)
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
            return strsFileList;
        }
        /// <summary>
        /// 取得文件大小
        /// </summary>
        /// <param name="strFileName">文件名</param>
        /// <returns>文件大小</returns>
        private long GetFileSize(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("SIZE " + Path.GetFileName(strFileName));
            long lSize = 0;
            if (iReplyCode == 213)
            {
                lSize = Int64.Parse(strReply.Substring(4));
            }
            else
            {
                throw new IOException(strReply.Substring(4));
            }
            return lSize;
        }

        /// <summary>
        /// 刪除
        /// </summary>
        /// <param name="strFileName">要刪除的文件名稱</param>
        public void Delete(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("DELE " + strFileName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
        }

        /// <summary>
        /// Rename(如果要改的名稱已存在將會覆蓋)
        /// </summary>
        /// <param name="strOldFileName">舊文件名</param>
        /// <param name="strNewFileName">新文件名</param>
        public void Rename(string strOldFileName, string strNewFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("RNFR " + strOldFileName);
            if (iReplyCode != 350)
            {
                throw new IOException(strReply.Substring(4));
            }

            SendCommand("RNTO " + strNewFileName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
        }
        #endregion
        #region 上傳和下載
        /// <summary>
        /// 下載一堆文件
        /// </summary>
        /// <param name="strFileNameMask">文件名稱匹配符合字串</param>
        /// <param name="strFolder">本機的目錄(不得以\结束)</param>
        public void Get(string strFileNameMask, string strFolder)
        {
            if (!bConnected)
            {
                Connect();
            }
            string[] strFiles = Dir(strFileNameMask);
            foreach (string strFile in strFiles)
            {
                if (!strFile.Equals(""))//一般來說最後一個陣列可能是空白字串
                {
                    Get(strFile, strFolder, strFile);
                }
            }
        }

        /// <summary>
        /// 下載一個文件
        /// </summary>
        /// <param name="strRemoteFileName">要下載的文件名稱</param>
        /// <param name="strFolder">本機的目錄(不得以\结束)</param>
        /// <param name="strLocalFileName">本機的檔名</param>
        public void Get(string strRemoteFileName, string strFolder, string strLocalFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SetTransferType(TransferType.Binary);
            if (strLocalFileName.Equals(""))
            {
                strLocalFileName = strRemoteFileName;
            }
            if (!File.Exists(strLocalFileName))
            {
                Stream st = File.Create(strLocalFileName);
                st.Close();
            }
            FileStream output = new
            FileStream(strFolder + "\\" + strLocalFileName, FileMode.Create);
            Socket socketData = CreateDataSocket();
            SendCommand("RETR " + strRemoteFileName);
            if (!(iReplyCode == 150 || iReplyCode == 125
            || iReplyCode == 226 || iReplyCode == 250))
            {
                throw new IOException(strReply.Substring(4));
            }
            while (true)
            {
                int iBytes = socketData.Receive(buffer, buffer.Length, 0);
                output.Write(buffer, 0, iBytes);
                if (iBytes <= 0)
                {
                    break;
                }
            }
            output.Close();
            if (socketData.Connected)
            {
                socketData.Close();
            }
            if (!(iReplyCode == 226 || iReplyCode == 250))
            {
                ReadReply();
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
        }

        /// <summary>
        /// 上傳一堆文件
        /// </summary>
        /// <param name="strFolder">本地目录(不得以\结束)</param>
        /// <param name="strFileNameMask">文件名匹配字符(可以包含*和?)</param>
        public void Put(string strFolder, string strFileNameMask)
        {
            string[] strFiles = Directory.GetFiles(strFolder, strFileNameMask);
            foreach (string strFile in strFiles)
            {
                //strFile是完整的文件名(包含路径)
                Put(strFile);
            }
        }
        /// <summary>
        /// 上傳一個文件
        /// </summary>
        /// <param name="strFileName">本機端的檔案名稱</param>
        public void Put(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            Socket socketData = CreateDataSocket();
            SendCommand("STOR " + Path.GetFileName(strFileName));
            if (!(iReplyCode == 125 || iReplyCode == 150))
            {
                throw new IOException(strReply.Substring(4));
            }
            FileStream input = new FileStream(strFileName, FileMode.Open);
            int iBytes = 0;
            while ((iBytes = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                socketData.Send(buffer, iBytes, 0);
            }
            input.Close();
            if (socketData.Connected)
            {
                socketData.Close();
            }
            if (!(iReplyCode == 226 || iReplyCode == 250))
            {
                ReadReply();
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
        }
        #endregion

        public void Put2(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            Socket socketData = CreateDataSocket();
            SendCommand("STOR " + Path.GetFileName(strFileName));
            if (!(iReplyCode == 125 || iReplyCode == 150))
            {
                throw new IOException(strReply.Substring(4));
            }
            //FileStream input = new FileStream(strFileName, FileMode.Open);
            //int iBytes = 0;
            //while ((iBytes = input.Read(buffer, 0, buffer.Length)) > 0)
            //{
            //    socketData.Send(buffer, iBytes, 0);
            //}
            //input.Close();
            socketData.SendFile(strFileName);
            if (socketData.Connected)
            {
                socketData.Close();
            }
            if (!(iReplyCode == 226 || iReplyCode == 250))
            {
                ReadReply();
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
        }
        #region 目錄操作

        /// <summary>
        /// 建立目錄
        /// </summary>
        /// <param name="strDirName">目錄名稱</param>
        public void MkDir(string strDirName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("MKD " + strDirName);
            if (iReplyCode != 257)
            {
                throw new IOException(strReply.Substring(4));
            }
        }
        /// <summary>
        /// 刪除目錄
        /// </summary>
        /// <param name="strDirName">目錄名稱</param>
        public void RmDir(string strDirName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("RMD " + strDirName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
        }
        /// <summary>
        /// 改變目錄
        /// </summary>
        /// <param name="strDirName">新的目錄名稱</param>
        public void ChDir(string strDirName)
        {
            if (strDirName.Equals(".") || strDirName.Equals(""))
            {
                return;
            }
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("CWD " + strDirName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
            this.strRemotePath = strDirName;
        }
        #endregion
        #region Field
        /// <summary>
        /// 伺服器回應的訊息
        /// </summary>
        private string strMsg;
        /// <summary>
        /// 伺服器回應的答應碼
        /// </summary>
        private string strReply;
        /// <summary>
        /// 伺服器回應的答應碼
        /// </summary>
        private int iReplyCode;
        /// <summary>
        /// socket物件
        /// </summary> 
        private Socket socketControl;
        /// <summary>
        /// 傳輸模式
        /// </summary>
        private TransferType trType;
        /// <summary>
        /// 接收和傳送數據的緩衝區
        /// </summary> 
        private static int BLOCK_SIZE = 256000; //512
        Byte[] buffer = new Byte[BLOCK_SIZE];
        /// 
        /// 編碼方式
        /// 如果你是台灣人就用big5
        /// 
        Encoding ASCII = Encoding.GetEncoding("big5");
        #endregion
        #region 內部函數
        /// <summary>
        /// 將回應的字串紀錄在strReplay&strMsg
        /// 回應碼記錄在iReplyCode
        /// </summary>
        private void ReadReply()
        {
            strMsg = "";
            strReply = ReadLine();
            iReplyCode = Int32.Parse(strReply.Substring(0, 3));
        }
        /// <summary>
        /// 建立並行數據連接的Socket
        /// </summary>
        /// <returns>Socket</returns>
        public Socket CreateDataSocket()
        {
            SendCommand("PASV");
            if (iReplyCode != 227)
            {
                throw new IOException(strReply.Substring(4));
            }
            int index1 = strReply.IndexOf('(');
            int index2 = strReply.IndexOf(')');
            string ipData =
             strReply.Substring(index1 + 1, index2 - index1 - 1);
            int[] parts = new int[6];
            int len = ipData.Length;
            int partCount = 0;
            string buf = "";
            for (int i = 0; i < len && partCount <= 6; i++)
            {
                char ch = Char.Parse(ipData.Substring(i, 1));
                if (Char.IsDigit(ch))
                    buf += ch;
                else if (ch != ',')
                {
                    throw new IOException("Malformed PASV strReply: " +
                     strReply);
                }
                if (ch == ',' || i + 1 == len)
                {
                    try
                    {
                        parts[partCount++] = Int32.Parse(buf);
                        buf = "";
                    }
                    catch (Exception)
                    {
                        throw new IOException("Malformed PASV strReply: " +
                         strReply);
                    }
                }
            }
            string ipAddress = parts[0] + "." + parts[1] + "." +
             parts[2] + "." + parts[3];
            int port = (parts[4] << 8) + parts[5];
            Socket s = new
             Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new
             IPEndPoint(IPAddress.Parse(ipAddress), port);
            try
            {
                s.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("Can't connect to remote server");
            }
            return s;
        }

        /// <summary>
        /// 關閉Socket的連線(用在登入以前)
        /// </summary>
        private void CloseSocketConnect()
        {
            if (socketControl != null)
            {
                socketControl.Close();
                socketControl = null;
            }
            bConnected = false;
        }

        /// <summary>
        /// 讀取Socket回應的所有字串
        /// </summary>
        /// <returns>回應碼</returns>
        private string ReadLine()
        {
            while (true)
            {
                Thread.Sleep(1000);
                int iBytes = socketControl.Receive(buffer, buffer.Length, 0);
                strMsg += ASCII.GetString(buffer, 0, iBytes);
                if (iBytes < buffer.Length)
                {
                    break;
                }
            }
            char[] seperator = { '\n' };
            string[] mess = strMsg.Split(seperator);
            if (strMsg.Length > 2)
            {
                strMsg = mess[mess.Length - 2];
            }
            else
            {
                strMsg = mess[0];
            }
            if (!strMsg.Substring(3, 1).Equals(" "))
            {
                return ReadLine();
            }
            return strMsg;
        }
        /// <summary>
        /// 傳送命令並取得回應碼和最後一行的回應字串
        /// </summary>
        /// <param name="strCommand">命令</param>
        private void SendCommand(String strCommand)
        {
            Encoding e = Encoding.GetEncoding("big5");
            Byte[] cmdBytes = e.GetBytes((strCommand + "\r\n").ToCharArray());
            socketControl.Send(cmdBytes, cmdBytes.Length, 0);
            ReadReply();
        }
        #endregion
    }
}
