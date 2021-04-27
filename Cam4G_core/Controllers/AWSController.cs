using Cam4G_core.App_Data;
using Cam4G_core.Models;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cam4G_core.Controllers
{
    /// <summary>
    /// AWS相關服務
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AWSController: ControllerBase
    {
        private readonly IS3Service _s3Service;
        private readonly IIOService _iOService;
        private Logger _logger;

        private Dictionary<string, string> IEMISubDirs;
        private double limitSize;

        public AWSController(IIOService iOService,IS3Service s3Service)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _iOService = iOService;
            _s3Service = s3Service;
        }

        /// <summary>
        /// 檢查IMEI資料夾及相關子資料夾有無建置
        /// </summary>
        /// <param name="IEMICODE"></param>
        /// <returns></returns>
        [HttpPost("S3/MoveData")]
        public async Task<MoveFileResponseObj> MoveData([FromForm] string IEMICODE,[FromForm] string encryptStr = "")
        {
            if (string.IsNullOrEmpty(IEMICODE))
                return Tools.GetErrorResult("IEMICODE is IsNullOrEmpty");

            #region 初始化
            limitSize = Config.GetSettingDVal("UoloadFolderLimitSize");      
            IEMISubDirs = Constant.GetIEMISubDirectories(IEMICODE);

            var subDirs = IEMISubDirs.Values.ToArray();
            if (subDirs.Length == 0)
            {
                _logger.Info("初始化Dictionary: _IEMISubDirectories錯誤");
                return Tools.GetErrorResult("初始化Dictionary: _IEMISubDirectories錯誤");
            }
            _logger.Info("檢查此siemicode資料夾底下的子資料夾");
            if (!await _iOService.CreateDirIfNotExist(subDirs)) //檢查資料夾是否存在，不存在就新增
                return Tools.GetErrorResult("檢查此siemicode資料夾底下的子資料夾");
            _logger.Info("檢查此siemicode資料夾底下的子資料夾結束");
            #endregion

            MoveFileResponseObj response;

            try
            {
                response = await MoveOnS3(IEMICODE, encryptStr);
            }
            catch (Exception ex)
            {
                _logger.Info("MoveFileJob錯誤:{0}", ex.Message);
                response = Tools.GetErrorResult("MoveFileJob錯誤:{0}" + ex.Message);
            }
            return response;
        }

        /// <summary>
        /// 將IMEI資料夾底下檔案搬移至S3 bucket上
        /// </summary>
        /// <param name="IEMICODE"></param>
        /// <returns></returns>
        private async Task<MoveFileResponseObj> MoveOnS3(string IEMICODE,string encryptStr = "")
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

            //壓縮
            //var img_fList = Directory.GetFiles(IEMISubDirs["picture"]);
            //var mov_fList = Directory.GetFiles(IEMISubDirs["movie"]);
            //var destPath = IEMISubDirs["Temp"];
            //var zipName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".zip";
            //var fList = img_fList.Concat(mov_fList).ToArray();
            //if (fList.Length == 0)
            //    return "error";

            //if (!_iIOService.ZipFiles(fList, destPath, zipName))
            //    return "error";

            //await _iIOService.DeleteFiles(fList); ResponseObject responseObj_3;
        

            //上傳S3 zip流程
            //var uploadFile = Path.GetFullPath(Path.Combine(destPath, zipName));
            //if (!_iIOService.CheckFileExist(uploadFile))
            //    return "error";

            //var res = await _s3Service.UploadFileAsync(uploadFile, IEMICODE, Config.GetSetting("S3:BucketName"));
            //#region 上傳成功後刪除
            //if (res.IsSuccess)
            //    await _iIOService.DeleteFileIfExist(uploadFile);
            //else
            //    result += res.Message + "\r\n";
          

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

            #region 上傳檔案至S3的兩個Buckets
            var picFiles = Directory.GetFiles(IEMISubDirs["picture"]);
            var movieFiles = Directory.GetFiles(IEMISubDirs["movie"]);
            var externalStr = encryptStr;

            if (picFiles.Length > 0)
            {
                bool[] isUploadeds = await MultipleAsyncUploadTask(IEMISubDirs["picture"], 
                                                                    Path.Combine(IEMICODE, "picture"),
                                                                    Path.Combine(IEMICODE, externalStr, "picture"));
                if (isUploadeds[0] && isUploadeds[1])
                {
                    _logger.Info("上傳結束，開始刪除所有照片檔案");
                    await _iOService.DeleteFiles(picFiles);
                    _logger.Info("刪除所有照片檔案結束");
                }
            }
          
            if (movieFiles.Length > 0)
            {
                bool[] isUploadeds = await MultipleAsyncUploadTask(IEMISubDirs["movie"], 
                                                                    Path.Combine(IEMICODE, "movie"),
                                                                    Path.Combine(IEMICODE, externalStr, "movie"));
                if (isUploadeds[0] && isUploadeds[1])
                {
                    _logger.Info("上傳結束，開始刪除所有影片檔案");
                    await _iOService.DeleteFiles(movieFiles);
                    _logger.Info("刪除所有影片檔案結束");
                }
            }
            #endregion

            #region 檢查在S3上的IEMICODE資料夾是否超出限制容量
           // var bucketsName = new string[] { Config.GetSetting("S3:BucketName"), Config.GetSetting("S3:BucketName2") };
            

            Task<Tuple<int, double>> t1 = CheckIfReachUploadLimit(Config.GetSetting("S3:BucketName"), IEMICODE,limitSize);
            Task<Tuple<int, double>> t2 = CheckIfReachUploadLimit(Config.GetSetting("S3:BucketName2"), IEMICODE, limitSize);

            _logger.Info("開始檢查Bucket:{0},在S3上的IEMICODE資料夾是否超出限制容量", Config.GetSetting("S3:BucketName"));
            _logger.Info("開始檢查Bucket:{0},在S3上的IEMICODE資料夾是否超出限制容量", Config.GetSetting("S3:BucketName2"));
            Tuple<int, double>[] deleteResult = await Task.WhenAll(t1, t2);
            _logger.Info("檢查Bucket:{0},在S3上的IEMICODE資料夾是否超出限制容量結束", Config.GetSetting("S3:BucketName"));
            _logger.Info("檢查Bucket:{0},在S3上的IEMICODE資料夾是否超出限制容量結束", Config.GetSetting("S3:BucketName2"));

            
            int deleteNum = 0;
            double deleteSize = 0.0;
            //檢查兩個bucket是否刪除同筆數資料
            if (deleteResult[0].Item1 == deleteResult[1].Item1)
                deleteNum = deleteResult[0].Item1;
            if(deleteResult[0].Item2 == deleteResult[1].Item2)
                deleteSize = deleteResult[0].Item2;

            //_logger.Info("檢查在S3上的IEMICODE資料夾是否超出限制容量結束");
            #endregion


            _logger.Info(IEMICODE + "上傳至Bucket1:{0} ,數量{1}", Config.GetSetting("S3:BucketName"), movedFileCount);
            _logger.Info(IEMICODE + "上傳至Bucket2:{0} ,數量{1}", Config.GetSetting("S3:BucketName2"), movedFileCount);
            return Tools.GetSuccessResult(movedFileCount,movedFileTotalSize, deleteNum, deleteSize);

            #region inner function
            async Task<Tuple<int,double>> CheckIfReachUploadLimit(string bucketName, string folderName, double limitSize)
            {
                int deleteNum = 0;
                double deleteSize = 0.0;
                try
                {

                    double UploadSize = 0;  
                    var s3Objects = await _s3Service.GetObjects(bucketName, folderName);
                    double FolderTotalSize = s3Objects.Select(o => o.Size).Sum();
                    if (FolderTotalSize > limitSize)
                    {
                        _logger.Info("已超出限制容量");
                        s3Objects.Sort((a, b) => a.LastModified.CompareTo(b.LastModified));
                        // _logger.Info("開始計算須刪除的objects");
                        var delObjectsKey = new List<string>();
                        foreach (var s3O in s3Objects)
                        {
                            deleteSize = deleteSize + s3O.Size;
                            FolderTotalSize = FolderTotalSize - s3O.Size;
                            delObjectsKey.Add(s3O.Key);
                            if (FolderTotalSize < limitSize)
                                break;
                        }
                        // _logger.Info("計算須刪除的objects結束");

                        _logger.Info("開始刪除超出容量");
                        //_logger.Info("刪除前容量: " + FolderTotalSize);
                        deleteNum = await _s3Service.DeleteObjectsAsync(bucketName, delObjectsKey);
                        // _logger.Info("刪除後容量: " + FolderTotalSize);
                        _logger.Info("刪除超出容量結束");
                        // _logger.Info("共刪除: {0} 個檔案", deleteNum);   
                    }
                    else
                    {
                        UploadSize = limitSize - FolderTotalSize;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Info("檢查在S3上的IEMICODE資料夾是否超出限制容量錯誤: " + ex.Message);
                }
                return Tuple.Create(deleteNum,deleteSize);
            }
            async Task<bool> UploadS3(string directoryPath, string keyPrefix, string bucketName)
            {
                //上傳失敗最多重複3次
                bool result = true;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var res = await _s3Service.UploadFilesAsync(directoryPath, keyPrefix, bucketName);
                        if (res.IsSuccess)
                        {
                            result = true;
                            break;
                        }
                        else
                        {
                            result = false;
                            _logger.Info("S3 多檔上傳api失敗:{0} \r\n 嘗試重傳({1}次)", res.Message, i + 1);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.Info("s3多檔上傳錯誤:{0}", ex.Message);
                        result = false;
                    }
                }

                return result;
            }
            async Task<bool[]> MultipleAsyncUploadTask(string directoryPath, string prefix, string prefix2 = "")
            {
                Task<bool> t1 = UploadS3(directoryPath, prefix2, Config.GetSetting("S3:BucketName"));//only cam bucket 需要加subfolder name
                Task<bool> t2 = UploadS3(directoryPath, prefix, Config.GetSetting("S3:BucketName2"));
           
                _logger.Info("開始上傳至Bucket:{0}", Config.GetSetting("S3:BucketName"));
                _logger.Info("開始上傳至Bucket2:{0}", Config.GetSetting("S3:BucketName2"));
                var results = await Task.WhenAll(t1, t2);
                _logger.Info("上傳至Bucket:{0}結束", Config.GetSetting("S3:BucketName"));
                _logger.Info("上傳至Bucket2:{0}結束", Config.GetSetting("S3:BucketName2"));
                return results;
            }
            #endregion
        }


    }
}
