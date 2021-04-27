using Cam4G_core.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cam4G_core.App_Data
{
    public class Tools
    {
        public static MoveFileResponseObj GetErrorResult(string message, int movedFileCount = 0, double movedFileTotalSize = 0, int deletedNum = 0, double deletedSize = 0)
        {
            MoveFileResponseObj resObj = new MoveFileResponseObj
            {
                IsSuccess = false,
                Message = message,
                DeletedNum = deletedNum,
                DeletedSize = deletedSize,
                MovedFileCount = movedFileCount,
                MovedFileTotalSize = movedFileTotalSize
            };
            return resObj;
        }

        public static MoveFileResponseObj GetSuccessResult(int movedFileCount = 0, double movedFileTotalSize = 0, int deletedNum = 0, double deletedSize = 0)
        {
            MoveFileResponseObj resObj = new MoveFileResponseObj
            {
                IsSuccess = true,
                Message = "",
                DeletedNum = deletedNum,
                DeletedSize = deletedSize,
                MovedFileCount = movedFileCount,
                MovedFileTotalSize = movedFileTotalSize
            };
            return resObj;
        }

        public static (string, int) Post(string URl, object Data, string AccessToken = "")
        {
            try
            {
                //  URl = TRelativeToAbsoluteUrl(URl);
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate
                { return true; };
                System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)4032;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URl);
                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/x-www-form-urlencoded";
                if (!string.IsNullOrEmpty(AccessToken))
                    request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AccessToken);

                NameValueCollection postParams = System.Web.HttpUtility.ParseQueryString(string.Empty);

                if (Data != null)
                {
                    foreach (PropertyInfo propertyInfo in Data.GetType().GetProperties())
                    {
                        postParams.Add(propertyInfo.Name, (propertyInfo.GetValue(Data, null)?.ToString() ?? string.Empty));
                    }

                    byte[] byteArray = Encoding.UTF8.GetBytes(postParams.ToString());
                    using (Stream reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(byteArray, 0, byteArray.Length);
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var result = reader.ReadToEnd();
                        return (result, (int)response.StatusCode);
                    }
                }
            }
            catch (WebException ex)
            {
                return (ex.Message, 0);
            }
        }

        #region Rijndael(AES)加密 加密解密須用同一把密鑰、IV(初始化向量)。
        public static string GetRijndaelEncrypt(string original, string iv, string key, int BlockSize = 128, PaddingMode pm = PaddingMode.PKCS7)
        {
            string result = "";
            try
            {
                string _original = Base64Encode(CreateMD5(original));

                byte[] Key = Encoding.ASCII.GetBytes(key);
                byte[] IV = Encoding.ASCII.GetBytes(iv);
                byte[] encrypted = EncryptStringToBytes(_original, Key, IV, BlockSize, pm);

                result = Convert.ToBase64String(encrypted);
            }
            catch (Exception ex)
            {
                return result;
            }
            return result;
        }

        public static string GetRijndaelDecrypt(string encryptStr, string iv, string key)
        {
            string roundtrip = "";
            try
            {
                byte[] Key = Encoding.ASCII.GetBytes(key);
                byte[] IV = Encoding.ASCII.GetBytes(iv);
                // Decrypt the bytes to a string.
                roundtrip = DecryptStringFromBytes(Base64Decode(encryptStr), Key, IV);
            }
            catch (Exception ex)
            {
                return roundtrip;
            }

            return roundtrip;
        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV, int BlockSize, PaddingMode pm)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;
                rijAlg.BlockSize = BlockSize;
                rijAlg.Padding = pm;
                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }
        #endregion

        private static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString().ToLower();
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static byte[] Base64Decode(string plainText)
        {
            return System.Convert.FromBase64String(plainText);
        }
    }
}
