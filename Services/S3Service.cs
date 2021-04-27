using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Core.Models;
using Core.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class S3Service : IS3Service
    {
        public Logger _logger { get; set; }
        public string _AWSAccessKeyId { get; set; }
        public string _AWSSecretKey { get; set; }
        public string _bucketRegion { get; set; }
        public string _BucketName { get; set; }
        public S3Service()
        {
        }

        public async Task<string> PutObject(String bucketName, String objectName, String filePath)
        {
            IAmazonS3 s3Client = new AmazonS3Client(_AWSAccessKeyId, _AWSSecretKey, RegionEndpoint.GetBySystemName(_bucketRegion));
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = bucketName;
            request.Key = objectName;
            request.FilePath = filePath;
            
            PutObjectResponse PutResult = null;
            try
            {
                PutResult = await s3Client.PutObjectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.Info("S3 PutObjectAsync錯誤:{0}", ex.Message);
            }

            string responseETag = "";
            if (PutResult != null)
            {
                responseETag = PutResult.ETag;
            }

            return responseETag;
        }

        public async Task<ResponseObject> UploadFilesAsync(string directoryPath, string keyPrefix, string bucketName)
        {
            try
            {
                IAmazonS3 s3Client = new AmazonS3Client(_AWSAccessKeyId, _AWSSecretKey, RegionEndpoint.GetBySystemName(_bucketRegion));
                
                if (s3Client != null)
                {
                    var fileTransferUtility = new TransferUtility(s3Client);
                    var transferUtilityUploadDirectoryRequest = new TransferUtilityUploadDirectoryRequest
                    {
                        Directory = directoryPath,
                        KeyPrefix = keyPrefix,
                        BucketName = bucketName,
                        SearchOption = SearchOption.TopDirectoryOnly,
                        
                        CannedACL = S3CannedACL.PublicRead
                    };
                    await fileTransferUtility.UploadDirectoryAsync(transferUtilityUploadDirectoryRequest);

                    _logger.Info("S3多檔上傳完成");
                    return new ResponseObject { IsSuccess = true, Message = "" };
                }
            }
            catch (Exception ex)
            {
                _logger.Info("S3多檔上傳錯誤{0}",ex.Message);
                return new ResponseObject { IsSuccess = false, Message = "Upload file error: " + ex.Message };
            }
            return new ResponseObject { IsSuccess = false, Message = "Upload file error" };
        }

        public async Task<ResponseObject> UploadFileAsync(string filePath, string keyPrefix, string bucketName)
        {
            try
            {
                IAmazonS3 s3Client = new AmazonS3Client(_AWSAccessKeyId, _AWSSecretKey, RegionEndpoint.GetBySystemName(_bucketRegion));

                if (s3Client != null)
                {
                    var fileTransferUtility = new TransferUtility(s3Client);
                    var fileName = keyPrefix + "/" +  filePath.Substring(filePath.LastIndexOf("\\") + 1);
                    var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = bucketName,
                        FilePath = filePath,
                        StorageClass = S3StorageClass.StandardInfrequentAccess,
                        CannedACL = S3CannedACL.PublicRead,
                        Key = fileName//keyPrefix + "/"
                    };

                    await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);

                    _logger.Info("S3單檔上傳完成");
                    return new ResponseObject { IsSuccess = true, Message = "" };
                }
            }
            catch (Exception ex)
            {
                _logger.Info("S3單檔上傳錯誤{0}",ex.Message);
                return new ResponseObject { IsSuccess = false, Message = "Upload file error: " + ex.Message };
            }
            return new ResponseObject { IsSuccess = false, Message = "Upload file error" };
        }

        public async Task<List<S3Object>> GetObjects(string bucketName, string folderName)
        {
            List<S3Object> result = new List<S3Object>();
            try
            {
                IAmazonS3 s3Client = new AmazonS3Client(_AWSAccessKeyId, _AWSSecretKey, RegionEndpoint.GetBySystemName(_bucketRegion));
                ListObjectsRequest loRequest = new ListObjectsRequest();
                loRequest.BucketName = bucketName;
                loRequest.Prefix = folderName + "/";
                // loRequest.MaxKeys = maxKeys;
                if (s3Client != null)
                {
                    var loResponse = await s3Client.ListObjectsAsync(loRequest);
                    var S3Objects = loResponse.S3Objects;
                    result.AddRange(S3Objects);
                }
            }
            catch (Exception ex)
            {
                _logger.Info("S3 GetObjects錯誤{0}", ex.Message);
            }

            return result;
        }

        public async Task<double> GetFolderSize(string bucketName, string folderName)
        {
            var fileSize = 0.0;
            try
            {
                IAmazonS3 s3Client = new AmazonS3Client(_AWSAccessKeyId, _AWSSecretKey, RegionEndpoint.GetBySystemName(_bucketRegion));
                ListObjectsRequest loRequest = new ListObjectsRequest();
                loRequest.BucketName = bucketName;
                loRequest.Prefix = folderName + "/";
                // loRequest.MaxKeys = maxKeys;
                if (s3Client != null)
                {
                    var loResponse = await s3Client.ListObjectsAsync(loRequest);
                    var S3Objects = loResponse.S3Objects;
                    foreach (var s3O in S3Objects)
                    {
                        fileSize += s3O.Size;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info("S3 GetFolderSize錯誤{0}", ex.Message);
            }

            return fileSize;
        }

        public async Task<int> DeleteObjectsAsync(string bucketName,List<string> objectKeys)
        {
            int count = 0;
            double size = 0.0;
            DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucketName
            };
            foreach (var key in objectKeys)
            {
                multiObjectDeleteRequest.AddKey(key);
            }
            try
            {
                IAmazonS3 s3Client = new AmazonS3Client(_AWSAccessKeyId, _AWSSecretKey, RegionEndpoint.GetBySystemName(_bucketRegion));
                DeleteObjectsResponse response = await s3Client.DeleteObjectsAsync(multiObjectDeleteRequest);
                count = response.DeletedObjects.Count;
                size = response.ContentLength;
                _logger.Info("S3 已成功刪除{0}個項目", count);
            }
            catch (DeleteObjectsException ex)
            {
                _logger.Info("S3 刪除項目錯誤{0}", ex.Message);
            }
            return count;
        }

    }
}
