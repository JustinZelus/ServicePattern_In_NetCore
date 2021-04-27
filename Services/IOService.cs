
using Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ionic.Zip;
using Core.Models;
using NLog;

namespace Services
{
    public class IOService : IIOService
    {
        private Logger _logger { get; set; }
        public IOService(Logger logger)
        {
            _logger = logger;
        }
        public Task<double> CaculateTotalSize(string path)
        {
            double Size = 0;
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                FileInfo[] fi = directoryInfo.GetFiles();

                foreach (var f in fi)
                {
                    Size += f.Length;
                }

            }
            catch (Exception ex)
            {
                _logger.Info("檢查是否超出FTP限制容量發生錯誤:" + ex.Message.ToString());
            }

            return Task.FromResult(Size);
        }

        public Task<double> CaculateTotalSize(string[] paths)
        {
            double size = 0.0;
            try
            {
                foreach (var path in paths)
                {
                    var fileInfos = new DirectoryInfo(path).GetFiles();
                    foreach (var fi in fileInfos)
                    {
                        size += fi.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info("計算檔案容量發生錯誤:" + ex.Message.ToString());
            }

            return Task.FromResult(size);
        }

        public Task<bool> CreateDirectory(string path)
        {
            var result = true;
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                _logger.Info("新增" + path + "資料夾發生錯誤:" + ex.Message.ToString());
                result = false;
            }
            return Task.FromResult(result);
        }

        public Task<bool> CreateDirIfNotExist(string[] paths)
        {
            bool result = true;

            if (paths is null)
                return Task.FromResult(result);

            foreach (var p in paths)
            {
                try
                {

                    if (!Directory.Exists(p))
                        CreateDirectory(p);
                }
                catch (Exception ex)
                {

                    _logger.Info("檢查" + p + "資料夾時發生錯誤:" + ex.Message.ToString());
                    result = false;
                    break;
                }
                finally
                {
                    if (Directory.Exists(p))
                        result = true;

                }
            }

            return Task.FromResult(result);
        }

        public Task<bool> CreateDirIfNotExist(string path)
        {
            bool result = true;

            if (path is null)
                return Task.FromResult(result);
            try
            {
                if (!Directory.Exists(path))
                     Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                _logger.Info("建資料夾錯誤: " + ex.Message);
                result = false;
            }
            finally
            {
                if (Directory.Exists(path))
                    result = true;
            }
            return Task.FromResult(result);
        }

        public Task<bool> DeleteDirectory(string dirPath)
        {
            var result = true;
            try
            {
                Directory.Delete(dirPath, true);
            }
            catch (Exception ex)
            {
                _logger.Info("刪除資料夾錯誤:" + ex.Message.ToString());
                result = false;
            }
            return Task.FromResult(result);
        }

        public Task<bool> DeleteFile(string fileName)
        {
            var result = true;
            try
            {
                File.Delete(fileName);
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Info("刪除檔名:" + fileName + "錯誤: " + ex.Message);
            }
            return Task.FromResult(result);
        }

        public async Task<bool> DeleteFileIfExist(string fileName)
        {
            var result = true;
            try
            {
                //檢查picture / movie內是否有相同檔名照片，有就刪除
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    _logger.Info("已刪除檔案{0}", fileName);
                }
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Info("刪除檔名錯誤: " + ex.Message);
            }

            return result;
        }

        public async Task<Tuple<bool,int>> DeleteFileIfExistWithExtension(string[] paths, string[] extFilters)
        {
            var result = true;
            int count = 0;
            try
            {
                foreach (var path in paths)
                {
                    var fileInfos = new DirectoryInfo(path).GetFiles();
                    //檢查picture / movie內是否有相同檔名照片，有就刪除
                    foreach (var fi in fileInfos)
                    {
                        if (!extFilters.Contains(fi.Extension.ToUpper()))
                        {
                            await DeleteFileIfExist(fi.FullName);
                            count++;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Info("刪除非指定的副檔名之檔案錯誤: " + ex.Message);
            }

            return Tuple.Create(result,count);
        }

        public Task<bool> MoveFilesToPath(string sourceFilename, string destFileName)
        {
            var result = true;
            try
            {
                File.Move(sourceFilename, destFileName);
            }
            catch (Exception ex)
            {
                _logger.Info("將" + sourceFilename + "搬移至:" + destFileName + "發生錯誤:" + ex.Message.ToString());
                result = false;
            }
            return Task.FromResult(result);
        }

        public async Task<Tuple<bool, int, double>> MoveFilesToPath(string sourcePath,
                                                   Dictionary<string, string> extToPath,
                                                   bool isCheckSpecialFileSize = false,
                                                   bool isDelExceptSpecifiedExt = false,
                                                   bool isUnzip = false,
                                                   string extractPath = null,
                                                   bool isDelZipWhenFinished = true)
        {
            var isFinished = true;
            int movedFileCount = 0;
            double movedFileTotalSize = 0;
            string[] zipFiles = new string[0];
            List<string> unZipTempPaths = new List<string>();
            try
            {
                sourcePath = Path.GetFullPath(sourcePath);
                FileInfo[] files = new DirectoryInfo(sourcePath).GetFiles();
                if (files.Length == 0)
                    return Tuple.Create(isFinished, movedFileCount, movedFileTotalSize);

                if (isUnzip)
                {
                    zipFiles = new DirectoryInfo(sourcePath).GetFiles("*.zip").Select(o => o.FullName).ToArray();
                }

                string[] extensions = extToPath.Keys.ToArray();

                if (zipFiles.Length > 0 && !string.IsNullOrEmpty(extractPath))
                {
                    _logger.Info("開始解壓縮檔案{0}",extractPath);
                    // 正規化路徑
                    string extraPath = Path.GetFullPath(extractPath);
                    (List<FileInfo> extractFiles, List<string> tempPath) = await UnZipFiles(zipFiles, extraPath, isDelZipWhenFinished);
                    _logger.Info("解壓縮檔案{0}結束", extractPath);
                    unZipTempPaths = tempPath;
                    files = files.Where(o => !o.Extension.Equals(".zip")).ToArray();
                    if (extractFiles.Count > 0)
                    {
                        _logger.Info("解壓出的檔案數量: {0}", extractFiles.Count);
                        files = files.Concat(extractFiles).ToArray();
                    }

                }

                _logger.Info("開始搬移檔案");
                foreach (var f in files)
                {
                    if (extensions.Length > 0 && !extensions.Contains(f.Extension.ToUpper()))
                    {
                        if (isDelExceptSpecifiedExt)
                        {
                            _logger.Info("刪除其它副檔名檔案{0}", f.FullName);
                            await DeleteFile(f.FullName);
                        }
                            
                    }
                    else
                    {
                        //檢查檔案大小是否為0.0(異常)，是就刪除(因應IOS無法抓異常照片，導致其他照片也抓不下來)
                        if (isCheckSpecialFileSize && f.Length == 0.0)
                        {
                            if(await DeleteFile(f.FullName))
                                _logger.Info("刪除異常檔案: " + f.FullName + " , size: " + f.Length);
                        }
                        else
                        {
                            var sourceFileName = f.FullName;
                            var destFileName = extToPath[f.Extension.ToUpper()]
                                                + "\\" + f.FullName.Substring(f.FullName.LastIndexOf("\\") + 1);

                            //System.IO.File.Move() doesn't support overwriting of an existing file,
                            //So delete it first.

                            await DeleteFileIfExist(destFileName);
                               

                            if (await MoveFilesToPath(sourceFileName, destFileName))
                            {
                                //計算搬移檔案size
                                _logger.Info("檔案{0} 已移至 {1}", sourceFileName, destFileName);
                                movedFileTotalSize += f.Length;
                                movedFileCount++;
                            }
                        }
                    }
                }
                _logger.Info("搬移檔案結束");

                foreach (var path in unZipTempPaths)
                {
                    if(await DeleteDirectory(path))
                        _logger.Info("刪除zip暫時資料夾{0}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.Info("搬移檔案過程錯誤: " + ex.Message);
                isFinished = false;
            }

            return Tuple.Create(isFinished, movedFileCount, movedFileTotalSize);
        }


        public List<FileInfo> GetFilesInfo(string[] paths, string[] extFilters = null)
        {
            List<FileInfo> result = new List<FileInfo>();
            try
            {
                foreach (var path in paths)
                {
                    var files = new DirectoryInfo(path).GetFiles();
                    if (extFilters != null && extFilters.Length > 0)
                    {
                        files = files.Where(o => !extFilters.Contains(o.Extension.ToUpper())).ToArray();
                    }
                    result.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                _logger.Info("取得檔案資訊清單錯誤: " + ex.Message);
            }
          
            return result;
        }

        public List<FileInfo> GetFilesInfo(string path, string[] extFilters = null)
        {
            List<FileInfo> result = new List<FileInfo>();
            try
            {
                var files = new DirectoryInfo(path).GetFiles();
                if (extFilters != null && extFilters.Length > 0)
                {
                    files = files.Where(o => !extFilters.Contains(o.Extension.ToUpper())).ToArray();
                }
                result.AddRange(files);
            }
            catch (Exception ex)
            {
                _logger.Info("取得檔案資訊清單錯誤: " + ex.Message);
            }

            return result;
        }

        public async Task<Tuple<List<FileInfo>,List<string>>> UnZipFiles(string[] zipPaths, 
                                                                        string extractPath = null, 
                                                                        bool isDelZipWhenFinished = true)
        {
            var result = new List<FileInfo>();
            string[] zPaths = zipPaths;
            string extraPath = extractPath;
            List<string> destinationPaths = new List<string>();


            // 正規化路徑
            extraPath = Path.GetFullPath(extraPath);

            if (!extraPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extraPath += Path.DirectorySeparatorChar;

            bool deleteZipFile = true;
          
            foreach (var zipPath in zPaths)
            {
                //建一個暫時資料夾，解決套件會產生垃圾檔案問題
                var tempFolderName = zipPath.Substring(zipPath.LastIndexOf("\\") + 1);
                //獲取完整路徑
                var destinationPath = Path.GetFullPath(Path.Combine(extractPath, tempFolderName));
                try
                {
                    if (!await CreateDirIfNotExist(destinationPath))
                    {
                        _logger.Info("目標資料夾{0}不存在", destinationPath);
                        break;
                    }
                    else
                    {
                        _logger.Info("開始將檔案解壓縮至{0}", destinationPath);
                        destinationPaths.Add(destinationPath);
                    }
                    using (var archive = ZipFile.Read(zipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {

                            if (entry.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                entry.FileName.EndsWith(".mov", StringComparison.OrdinalIgnoreCase))
                            {
                                //順序匹配是最安全的，不區分大小寫。
                                if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                                {
                                    try
                                    {
                                        entry.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);
                                        var f = Path.GetFullPath(Path.Combine(destinationPath, entry.FileName));
                                        if (File.Exists(f))
                                            result.Add(new FileInfo(f));
                                    }
                                    catch (Exception ex)
                                    {
                                        //若解到有問題的檔案，但被解出來。要刪除，否則打不開。
                                        _logger.Info("archive.Entries 錯誤，解出有問題檔案 : " + ex.Message);
                                        await DeleteFileIfExist(destinationPath);
                                        continue;
                                    }
                                }
                            }
                        }
                        _logger.Info("將檔案解壓縮至{0}結束", destinationPath);
                    }
                }
                catch (Exception ex)
                {
                    //如果是被其他應用程式鎖住，先不要刪除檔案.
                    if (ex.Message.ToString().IndexOf("it is being used by another process") > 0)
                        deleteZipFile = false;
                    _logger.Info("UnZipFiles 錯誤: " + ex.Message);
                    continue;
                }
                finally
                {
                    if (deleteZipFile)
                    {
                        //刪除壓縮檔
                        if (isDelZipWhenFinished)
                        {
                            await DeleteFile(zipPath);
                            _logger.Info("刪除壓縮檔{0}", zipPath);
                        }
                            
                    }
                }

            }

            return Tuple.Create(result, destinationPaths);
        }

        public bool ZipFiles(string[] filePaths, string destnationPath,string outPutFileName)
        {
            var result = true;
            string destPath = "";
            try
            {
                using (var zip = new ZipFile(""))
                {
                    zip.AddFiles(filePaths, "\\");
                    destPath = Path.GetFullPath(Path.Combine(destnationPath, outPutFileName));
                    zip.Save(destPath);
                }
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Info("壓縮檔案{0}錯誤:{1}", destPath, ex.Message);
            }
            return result;
        }

        public bool CheckFileExist(string fileName)
        {
            var result = true;
            try
            {
                if (!File.Exists(fileName))
                    result = false;
            }
            catch (Exception ex)
            {
                result = false;
                _logger.Info("檢查檔案是否存在錯誤: {0} ", ex.Message);
            }
            return result;
        }

        public async Task<bool> DeleteFiles(string[] filesName)
        {
            var result = true;
            foreach (var fileName in filesName)
            {
                try
                {
                    await DeleteFileIfExist(fileName);
                }
                catch (Exception ex)
                {
                    result = false;
                    _logger.Info("刪除檔名:" + fileName + "錯誤: " + ex.Message);
                    continue;
                }
            }
            return result;
        }

        public async Task EmptyFolder(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    if (file.Exists)
                        file.Delete();
                }
                foreach (DirectoryInfo directory in di.EnumerateDirectories())
                {
                    FileAttributes attr = File.GetAttributes(directory.FullName);

                    //遞迴
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        await EmptyFolder(directory.FullName);
                }
            }
            catch (IOException ex)
            {
                _logger.Info("清空資料夾錯誤: " + ex.Message);
            }
        }

        public async Task<bool> IsDirectoryEmpty(string path)
        {
            var result = false;
            try
            {
                IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
                using (IEnumerator<string> en = items.GetEnumerator())
                {
                    result = !en.MoveNext();
                }
            }
            catch (IOException ex)
            {
                _logger.Info("IsDirectoryEmpty錯誤: " + ex.Message);
            }
            return result;
        }
    }
}
