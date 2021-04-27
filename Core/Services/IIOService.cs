using Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface IIOService
    {
        bool CheckFileExist(string fileName);
        Task<double> CaculateTotalSize(string path);
        Task<double> CaculateTotalSize(string[] path);
        Task<bool> CreateDirectory(string path);
        Task<bool> CreateDirIfNotExist(string[] paths);
        Task<bool> CreateDirIfNotExist(string path);
        Task<bool> DeleteDirectory(string dirPath);
        Task<bool> DeleteFile(string fileName);
        Task<bool> DeleteFiles(string[] filesName);
        Task<bool> DeleteFileIfExist(string fileName);
        Task<Tuple<bool, int>> DeleteFileIfExistWithExtension(string[] paths, string[] extFilters);
        Task<bool> MoveFilesToPath(string sourceFilename, string destFileName);
        Task<Tuple<bool, int, double>> MoveFilesToPath(string sourcePath,
                                                   Dictionary<string, string> extToPath,
                                                   bool isCheckSpecialFileSize = false,
                                                   bool isDelExceptSpecifiedExt = false,
                                                   bool isUnzip = false,
                                                   string extractPath = null,
                                                   bool isDelZipWhenFinished = true);

        List<FileInfo> GetFilesInfo(string[] paths, string[] extFilters = null);
        List<FileInfo> GetFilesInfo(string path, string[] extFilters = null);
      
        Task<Tuple<List<FileInfo>, List<string>>> UnZipFiles(string[] zipPaths, string extractPath = null, bool isDelZipWhenFinished = true);

        bool ZipFiles(string[] filePaths, string destnationPath, string outPutFileName);

        /// <summary>
        /// 清空資料夾(遞迴)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task EmptyFolder(string path);
    }
}
