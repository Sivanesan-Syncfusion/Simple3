using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Amazon.Runtime;
using Amazon.S3;
using System.IO;
using Amazon.S3.Transfer;
using Bold_Meeting_Recordings.Helpers;
using Amazon.S3.Model;
using Bold_Meeting_Recordings.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bold_Meeting_Recordings.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppSettings _appSettings;
        private readonly AwsHelper awsHelper;
        private readonly IHttpContextAccessor httpContextAccessor;

        public HomeController(IOptions<AppSettings> appSettings, IHttpContextAccessor _httpContextAccessor)
        {
            _appSettings = appSettings.Value;
            awsHelper = new AwsHelper(appSettings);
            httpContextAccessor = _httpContextAccessor;
        }

        [HttpGet("")]
        public IActionResult UploadFile()
        {
            return View();
        }

        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [HttpPost("upload-file")]
        public async Task<JsonResult> UploadFile(IList<IFormFile> files, string key)
        {
            Startup.Progress = "Uploading...";
            string FileUploadUrl = string.Empty;
            string uploadFileName = string.Empty;

            try
            {
                var bucketName = !string.IsNullOrWhiteSpace(_appSettings.FolderName)
                    ? _appSettings.BucketName + @"/" + _appSettings.FolderName
                    : _appSettings.BucketName;
                foreach (IFormFile file in files)
                {
                    uploadFileName = file.FileName;
                    var name = Path.GetFileNameWithoutExtension(file.FileName);
                    var ext = Path.GetExtension(file.FileName);
                    var fileName = name + "_" + AwsHelper.RandomString(10) + ext;

                    var fileUploadSpace = !string.IsNullOrWhiteSpace(key) ? key + "/" + fileName : fileName;
                    fileUploadSpace = fileUploadSpace.Replace(" ", "_").ToLower();

                    var credentials = new BasicAWSCredentials(_appSettings.AccessKey, _appSettings.SecretKey);
                    var config = new AmazonS3Config
                    {
                        RegionEndpoint = Amazon.RegionEndpoint.USEast2
                    };

                    using (var client = new AmazonS3Client(credentials, config))
                    {
                        using (var newMemoryStream = new MemoryStream())
                        {
                            file.CopyTo(newMemoryStream);

                            var uploadRequest = new TransferUtilityUploadRequest
                            {
                                InputStream = newMemoryStream,
                                Key = fileUploadSpace,
                                BucketName = bucketName,
                                CannedACL = S3CannedACL.PublicRead
                            };

                            var fileTransferUtility = new TransferUtility(client);

                            uploadRequest.UploadProgressEvent +=
                            new EventHandler<UploadProgressArgs>
                                (uploadRequest_UploadPartProgressEvent);

                            await fileTransferUtility.UploadAsync(uploadRequest);
                        }
                    }

                    FileUploadUrl = awsHelper.GenerateAwsFileUrl(_appSettings.AwsUser, fileUploadSpace);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null
                    && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Check the provided AWS Credentials.");
                }
                else
                {
                    Console.WriteLine("Error occurred: " + amazonS3Exception.Message);
                }

                return Json(new { Status = false, FileUrl = FileUploadUrl, CustomerName = string.IsNullOrWhiteSpace(key) ? "Not specified" : key, FileName = uploadFileName });
            }

            return Json(new { Status = true, FileUrl = FileUploadUrl, CustomerName = string.IsNullOrWhiteSpace(key) ? "Not specified" : key, FileName = uploadFileName });
        }

        static void uploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
        {
            Startup.Progress = $"Uploading... {e.PercentDone}";
        }

        [HttpPost("send-progress")]
        public ActionResult Progress()
        {
            return this.Content(Startup.Progress.ToString());
        }

        [HttpGet("get-files")]
        public IActionResult GetFiles()
        {
            return View();
        }

        [HttpPost("file-list")]
        public IActionResult AllFiles(FileUploadFormModel model)
        {
            var key = model.Key.Replace(" ", "_").ToLower();
            var FetchedSource = new List<S3Object>();
            var FinalSource = new List<S3Object>();
            var credentials = new BasicAWSCredentials(_appSettings.AccessKey, _appSettings.SecretKey);

            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.USEast2
            };

            using var client = new AmazonS3Client(credentials, config);
            var allItems = client.ListObjectsAsync(_appSettings.BucketName).Result.S3Objects;

            if (key.Equals("all"))
            {
                FetchedSource = allItems;
            }
            else
            {
                foreach (var item in allItems)
                {
                    if (item.Key.Contains(key))
                    {
                        FetchedSource.Add(item);
                    }

                    if (item.Key.Contains(model.Key))
                    {
                        FetchedSource.Add(item);
                    }
                }
            }

            foreach (var item in FetchedSource)
            {
                var age = (DateTime.Now - item.LastModified).Days;

                if (age > 90)
                {
                    continue;
                }

                FinalSource.Add(item);
            }

            foreach (var item in FinalSource)
            {              
                item.ETag = AwsHelper.SizeSuffix(item.Size, 2); ;
            }

            ViewBag.UrlPrefix = $"https://{_appSettings.AwsUser}.{_appSettings.AwsRegion}.{_appSettings.AwsS3BaseUrl}";
            ViewBag.DataSource = FinalSource;
            ViewBag.DeleteObjectUrl = Url.Action("DeleteFile");
            ViewBag.DownloadObjectUrl = Url.Action("DownloadFile");

            return View();
        }

        public async Task<IActionResult> DownloadFile([FromQuery] string key)
        {
            try
            {               
                var credentials = new BasicAWSCredentials(_appSettings.AccessKey, _appSettings.SecretKey);
                var config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.USEast2
                };
                using var client = new AmazonS3Client(credentials, config);
                var fileTransferUtility = new TransferUtility(client);

                var objectResponse = await fileTransferUtility.S3Client.GetObjectAsync(new GetObjectRequest()
                {
                    BucketName = _appSettings.BucketName,
                    Key = key
                });

                if (objectResponse.ResponseStream == null)
                {
                    return NotFound();
                }
                return File(objectResponse.ResponseStream, objectResponse.Headers.ContentType, key);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null
                    && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception("Error occurred: " + amazonS3Exception.Message);
                }
            }

        }

        [HttpPost("delete-file")]
        public async Task<JsonResult> DeleteFile(string key)
        {
            try
            {
                var credentials = new BasicAWSCredentials(_appSettings.AccessKey, _appSettings.SecretKey);
                var config = new AmazonS3Config
                {
                    RegionEndpoint = Amazon.RegionEndpoint.USEast2
                };
                using var client = new AmazonS3Client(credentials, config);
                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.S3Client.DeleteObjectAsync(new DeleteObjectRequest()
                {
                    BucketName = _appSettings.BucketName,
                    Key = key
                });

            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                string message;

                if (amazonS3Exception.ErrorCode != null
                    && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    message = "Check the provided AWS Credentials.";
                }
                else
                {
                    message = amazonS3Exception.Message;
                }

                return Json(new { Result = false, Message = message });
            }

            return Json(new { Result = true });
        }
    }
}
