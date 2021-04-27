using Amazon.S3.Model;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Core.Services
{
    public interface IS3Service
    {
        Task<ResponseObject> UploadFilesAsync(string directoryPath, string keyPrefix, string bucketName);
        Task<ResponseObject> UploadFileAsync(string filePath, string keyPrefix, string bucketName);
        Task<string> PutObject(String bucketName, String objectName, String filePath);
        Task<double> GetFolderSize(string bucketName, string folderName);
        Task<List<S3Object>> GetObjects(string bucketName, string folderName);
        Task<int> DeleteObjectsAsync(string bucketName, List<string> objectKeys);
    }
}
