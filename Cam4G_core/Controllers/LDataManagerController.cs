
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Cam4G_core.App_Data;
using Core.Services;
using System.Collections.Generic;
using System.IO;
using NLog;
using Cam4G_core.Models;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using System.Net;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using FluentFTP;
using Cam4G_core.Services;

namespace Cam4G_core.Controllers
{
    /// <summary>
    /// 本地相關服務
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LDataManagerController : ControllerBase
    {
        private Logger _logger;
        private readonly IIOService _iOService;
        private readonly IGCMService _gCMService;
        private readonly IBackgroundTaskQueue _taskQueue;

        private Dictionary<string, string> IEMISubDirs;
        private double limitSize;

        public LDataManagerController(IIOService iOService,
                                IGCMService gCMService,
                                IBackgroundTaskQueue taskQueue)
        {
            // _logger = logger;
            _logger = LogManager.GetCurrentClassLogger();
            _iOService = iOService;
            _gCMService = gCMService;
            _taskQueue = taskQueue;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }

        [HttpPost("EmptyFolder")]
        public async Task<MoveFileResponseObj> EmptyFolder([FromForm] string IEMICODE)
        {
            MoveFileResponseObj resObj = Tools.GetSuccessResult();
            if (string.IsNullOrEmpty(IEMICODE))
                return Tools.GetErrorResult("IEMICODE 為空或null");
            try
            {
                var folderPaths = Constant.GetIEMISubDirectories(IEMICODE).Values.ToList();
                folderPaths.Add(Config.GetSetting("FilePath") + "\\" + IEMICODE);
                foreach (var path in folderPaths)
                {
                    if (_iOService != null)
                        await _iOService.EmptyFolder(path);
                }
            }
            catch (Exception ex)
            {
                _logger.Info("web api - EmptyFolder 錯誤:{0}", ex.Message);
            }
            return resObj;
        }

        /// <summary>
        /// 檢查IMEI資料夾及相關子資料夾有無建置
        /// </summary>
        /// <param name="IEMICODE"></param>
        /// <returns></returns>
        [HttpPost("MoveData")]
        public async Task<MoveFileResponseObj> MoveData([FromForm] string IEMICODE)
        {
            if (string.IsNullOrEmpty(IEMICODE))
                return Tools.GetErrorResult("IEMICODE is IsNullOrEmpty");

            #region 初始化
            limitSize = Config.GetSettingDVal("UoloadFolderLimitSize");         //TODO 這兩行如何改善
            IEMISubDirs = Constant.GetIEMISubDirectories(IEMICODE);

            var subDirs = IEMISubDirs.Values.ToArray();
            if (subDirs.Length == 0)
            {
                _logger.Info("初始化Dictionary: _IEMISubDirectories錯誤");
                return Tools.GetErrorResult("初始化Dictionary: _IEMISubDirectories錯誤");
            }
           
            if (!await _iOService.CreateDirIfNotExist(subDirs))
                return Tools.GetErrorResult("檢查資料夾是否存在，不存在就新增錯誤");
            _logger.Info("檢查資料夾{0}底下的子資料夾", IEMICODE);
            #endregion

            var files = _iOService.GetFilesInfo(Config.GetSetting("FilePath") + "\\" + IEMICODE,
                                                new string []{"JPG","MOV" });

            MoveFileResponseObj response;
            if (files.Count == 0)
            {
                response = Tools.GetSuccessResult();
            }
            else
            {
                try
                {
                    response = await MoveOnLocal(IEMICODE);
                }
                catch (Exception ex)
                {
                    _logger.Info("MoveFileJob錯誤:{0}", ex.Message);
                    response = Tools.GetErrorResult("MoveFileJob錯誤:{0}" + ex.Message);
                }
            }
            return response;
        }

        /// <summary>
        /// 將IMEI資料夾底下檔案搬移至子資料夾
        /// </summary>
        /// <param name="IEMICODE"></param>
        /// <returns></returns>
        private async Task<MoveFileResponseObj> MoveOnLocal(string IEMICODE)
        {
            var sourcePath = Config.GetSetting("FilePath") + "\\" + IEMICODE;
            var extsToPaths = new Dictionary<string, string>
                 {
                    { ".JPG" ,IEMISubDirs["picture"] },
                    { ".MOV" ,IEMISubDirs["movie"] }
                 };
            bool isCheckSpecialFileSize = true;
            bool isDelExceptSpecifiedExt = true;
            bool isUnzip = true;
            string UnzipPath = IEMISubDirs["Temp"];
            bool isDelZipWhenFinished = true;

            #region 搬移檔案
            _logger.Info("開始搬移裝置照片 \r\n");
            (bool isFinished, int movedFileCount, double movedFileTotalSize) = await _iOService.MoveFilesToPath(sourcePath, extsToPaths,
                                                                                     isCheckSpecialFileSize, isDelExceptSpecifiedExt, isUnzip, UnzipPath, isDelZipWhenFinished);
            _logger.Info("\r\n搬移裝置照片結束 ");
            //搬移裝置照片結束
            if (!isFinished)
                return Tools.GetErrorResult("搬移裝置照片未完成", movedFileCount, movedFileTotalSize);

            _logger.Info("已完成搬移{0}個檔案", movedFileCount);
            _logger.Info("所有搬移的檔案大小{0}", movedFileTotalSize);
            #endregion

            #region 刪除不相關之檔案
            var paths = new string[] {
                            IEMISubDirs["movie"],
                            IEMISubDirs["picture"],
                        };
            var extensionFilters = new string[] { ".JPG", ".MOV" };
            _logger.Info("開始刪除不相關之檔案");
            (bool isDeletedFinished, int delCount) = await _iOService.DeleteFileIfExistWithExtension(paths, extensionFilters);
            _logger.Info("刪除不相關之檔案結束");
            #endregion

            #region 檢查是否超出FTP限制容量
            int deleteNum = 0;
            double deleteSize = 0;
            //"檢查是否超出FTP限制容量(每一個裝置50MB)，並刪除舊照片"
            if (movedFileCount > 0)
            {
                _logger.Info("開始檢查是否超出FTP限制容量");
                double FolderTotalSize = await _iOService.CaculateTotalSize(paths);
                double UploadSize = 0;


                if (FolderTotalSize > limitSize)
                {
                    _logger.Info("已超出FTP限制容量");
                    List<FileInfo> ficList = _iOService.GetFilesInfo(paths);

                    ficList.Sort((a, b) => a.CreationTime.CompareTo(b.CreationTime));

                    _logger.Info("開始刪除超出容量");
                    _logger.Info("刪除前容量: " + FolderTotalSize);
                    foreach (var fic in ficList)
                    {
                        deleteNum = deleteNum + 1;
                        deleteSize = deleteSize + fic.Length;
                        FolderTotalSize = FolderTotalSize - fic.Length;
                        await _iOService.DeleteFile(fic.FullName);
                        _logger.Info("(超SIZE)刪除檔案" + fic.FullName + "  " + fic.Length + "bytes");
                        if (FolderTotalSize <= limitSize)
                        {
                            break;
                        }
                    }
                    _logger.Info("刪除後容量: " + FolderTotalSize);
                    _logger.Info("刪除超出容量結束");
                }
                else
                {
                    UploadSize = limitSize - FolderTotalSize;
                    _logger.Info("剩餘上傳空間{0}", UploadSize);
                }
                _logger.Info("檢查是否超出FTP限制容量結束");
            }
            #endregion

            _logger.Info(IEMICODE + "搬移檔案數量{0}", movedFileCount);
            return Tools.GetSuccessResult(movedFileCount, movedFileTotalSize, deleteNum, deleteSize);
        }

        //--------------------------------------------------------------------------
        //-------------------------------------方案B---------------------------------
        //--------------------------------------------------------------------------

        /// <summary>
        /// 將sandbox(client)檔案"上傳"至遠端FTP(server)
        /// </summary>
        /// <param name="IEMICODE"></param>
        /// <returns></returns>
        [HttpPost("MoveDataFtp")]
        public async Task<IActionResult> MoveDataFtp([FromForm] string IEMICODE)
        {
            if (string.IsNullOrEmpty(IEMICODE))
                return Ok("1");

            #region 初始化
            limitSize = Config.GetSettingDVal("UoloadFolderLimitSize");        
            IEMISubDirs = Constant.GetIEMISubDirectories(IEMICODE);

            var subDirs = IEMISubDirs.Values.ToArray();
            if (subDirs.Length == 0)
            {
                _logger.Info($"TaskID:{Task.CurrentId} 初始化Dictionary: _IEMISubDirectories錯誤");
                return Ok("1");
            }
           
            if (!await _iOService.CreateDirIfNotExist(subDirs))
                return Ok("1");
            _logger.Info($"TaskID:{Task.CurrentId} 檢查{IEMICODE} 資料夾底下子資料夾有無創建");
            #endregion

            var files = _iOService.GetFilesInfo(Config.GetSetting("FilePath") + "\\" + IEMICODE,
                                             new string[] { "JPG", "MOV" });
            if (files.Count == 0)
            {
                _logger.Info($"TaskID:{Task.CurrentId} , {IEMICODE} 檔案數量0");
                return Ok("0");
            }
            else
            {
                try
                {
                 
                    //  Task.Run(() => MoveDataToFtp(IEMICODE));
                    _taskQueue.QueueBackgroundWorkItem(async token =>
                    {
                        _logger.Info($"TaskID:{Task.CurrentId}  開始移動檔案至ftp");
                        await MoveDataToFtp(IEMICODE);
                        _logger.Info($"TaskID:{Task.CurrentId}  移動檔案至ftp結束");
                    });

                }
                catch (Exception ex)
                {
                    _logger.Info($"TaskID:{Task.CurrentId}  MoveFileJob錯誤:{ex.Message}");
                }
            }
            return Ok("0");
        }

        public async Task<MoveFileResponseObj> MoveDataToFtp(string IEMICODE)
        {
            string url = Config.GetSetting("FTP:IP");
            string user = Config.GetSetting("FTP:Account");
            string pwd = Config.GetSetting("FTP:Pwd");
            string Ftp_dir = Config.GetSetting("FTP:Ftp_dir");
            string gcm_url = Config.GetSetting("GCM_Url");

            _logger.Info($"TaskID:{Task.CurrentId} 開始FTP流程");
            (
                bool isFinished,
                 string message,
                 double deletedSize,
                 double movedFileTotalSize,
                 int deletedNum,
                 int MovedFileCount
             ) = await FTPStart(IEMICODE, url, user, pwd, Ftp_dir, "Y");
            _logger.Info($"TaskID:{Task.CurrentId} FTP流程結束");

            var responseObj = new MoveFileResponseObj
            {
                IsSuccess = isFinished,
                DeletedNum = deletedNum,
                DeletedSize = deletedSize,
                Message = message,
                MovedFileCount = MovedFileCount,
                MovedFileTotalSize = movedFileTotalSize,
            };

            if (isFinished)
            {
                _logger.Info($"TaskID:{Task.CurrentId} 開始post: {gcm_url}");
                (string result, int satausCode) = Tools.Post(gcm_url, new { IEMICODE = IEMICODE, GCMTYPE = 1 });
                _logger.Info($"TaskID:{Task.CurrentId} post結束， result:{result}");
                if (!result.Equals("0"))
                {
                    responseObj.Message = result;
                    _logger.Info($"TaskID:{Task.CurrentId} push {gcm_url}失敗 , 原因:{result}");
                }
            }

            return responseObj;
        }

        private async Task<Tuple<bool, string, double, double, int, int>> FTPStart(string IEMICODE, string ip, string user, string password, string Ftp_dir, string Upload_Data = "N")
        {
            bool isFinished = false;
            string message = "";
            double deletedSize = 0;
            double movedFileTotalSize = 0;
            int deletedCount = 0;
            int movedFileCount = 0;

            try
            {
                FtpClient client = new FtpClient(ip);
                client.Credentials = new NetworkCredential(user, password);
                client.Connect();

                if (!client.IsConnected)
                    _logger.Info($"TaskID:{Task.CurrentId} ftp連線失敗");
 
                var sourcePath = Config.GetSetting("FilePath") + "\\" + IEMICODE;
                
                //await _iOService.CreateDirIfNotExist(sourcePath);
                //_logger.Info("檢查資料夾{0}存在完畢", sourcePath);

                #region 初始化上傳檔案路徑
                string[] filters = new string[] { "JPG", "MOV" };
                List<FileInfo> fileInfos = _iOService.GetFilesInfo(sourcePath, filters);
                List<string> sourceFiles = fileInfos.Select(o => o.FullName).ToList();
                var sourceFileNames = fileInfos.Select(o => o.Name).ToArray();
                _logger.Info($"TaskID:{Task.CurrentId} 已取得需上傳檔案路徑");

                List<string> remoteFiles = new List<string>();
                foreach (var fileInfo in fileInfos)
                {
                    string remoteFileName = Ftp_dir + "\\" + IEMICODE + "\\" + fileInfo.Name;
                    remoteFiles.Add(remoteFileName);
                }
                #endregion

                #region 上傳
                _logger.Info($"TaskID:{Task.CurrentId} 開始上傳");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int i = 0; i < sourceFiles.Count; i++)
                {
                    client.UploadFile(sourceFiles[i], remoteFiles[i]);
                }
                stopwatch.Stop();
                _logger.Info($"TaskID:{Task.CurrentId} 上傳結束");
                _logger.Info($"TaskID:{Task.CurrentId} 花費{TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds}秒");
                #endregion

                #region 過濾出要刪除的檔案
                List<string> listRemoteFiles = new List<string>();
                foreach (FtpListItem item in client.GetListing(Ftp_dir + "\\" + IEMICODE))
                {
                    if (item.Type == FtpFileSystemObjectType.File)
                        listRemoteFiles.Add(item.Name);
                }
                _logger.Info($"TaskID:{Task.CurrentId} 取得ftp 資料夾{Ftp_dir + "\\" + IEMICODE}檔案");

                List<string> uploadedfileNames = new List<string>();
                foreach (var fileName in listRemoteFiles)
                {
                    var split1 = fileName.Split("\\");
                    if (split1.Length == 1)
                        break;
                    else
                    {
                        var split2 = split1[2].Split("/");                //ex: \\Picure\\123451234512355/00000001.JPG
                        if (split2.Length == 2)
                        {
                            var Fname = split2[1];
                            uploadedfileNames.Add(Fname);
                        }

                    }
                }
                if (uploadedfileNames.Count > 0)
                {
                    var exceptFileNames = sourceFileNames.Except(uploadedfileNames).ToList();
                    foreach (var fileName in exceptFileNames)
                    {
                        int index = sourceFiles.FindIndex(o => o.Contains(fileName));
                        if (index >= 0)
                            sourceFiles.RemoveAt(index);
                    }
                }
                #endregion

                #region 刪除檔案
                _logger.Info($"TaskID:{Task.CurrentId} 要刪除的檔案數量:{sourceFiles.Count}");
                _logger.Info($"TaskID:{Task.CurrentId} 開始刪除");
                foreach (var fn in sourceFiles)
                {
                    int index = fileInfos.FindIndex(o => o.FullName.Contains(fn));
                    if (index >= 0)
                    {
                        FileInfo fileInfo = fileInfos[index];
                        deletedSize += fileInfo.Length;
                        movedFileTotalSize += fileInfo.Length;
                        deletedCount++;
                        movedFileCount++;
                        lock(fn)
                        {
                            if (System.IO.File.Exists(fn))
                            {
                                System.IO.File.Delete(fn);
                                _logger.Info($"已刪除檔案{fn}");
                            }
                        }

                    }
                }
                _logger.Info($"TaskID:{Task.CurrentId} 刪除結束");
                #endregion
                client.Disconnect();
                isFinished = true;
            }
            catch (Exception ex)
            {
                isFinished = false;
                _logger.Info($"TaskID:{Task.CurrentId} FTP上傳過程錯誤: " + ex.Message);
            }

            return Tuple.Create(isFinished, message, deletedSize, movedFileTotalSize, deletedCount, movedFileCount);
        }

        [HttpPost]
        [Route("TestPost2")]
        public async Task<IActionResult> TestPost2([FromForm] string IEMICODE)
        {
            _taskQueue.QueueBackgroundWorkItem(async token =>
            {
                _logger.Info($"{IEMICODE} 開始上傳檔案");
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                _logger.Info($"{IEMICODE} 上傳檔案結束");
            });

            return Ok("0");
        }

    }


 
}
