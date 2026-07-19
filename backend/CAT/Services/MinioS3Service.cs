using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.AspNetCore.Http;

namespace CAT.Services
{
    public class MinioS3Service
    {
        private readonly IAmazonS3? _s3Client;
        private readonly string _bucketName;
        private readonly string _baseUrl;
        private readonly bool _isConfigured;

        public MinioS3Service(IConfiguration configuration)
        {
            _bucketName = configuration["MinIO:BucketName"] ?? "cattle-track";
            _baseUrl = configuration["MinIO:BaseUrl"] ?? "http://minio:9000";
            var accessKey = configuration["MinIO:AccessKey"];
            var secretKey = configuration["MinIO:SecretKey"];
            var serviceUrl = configuration["MinIO:ServiceUrl"] ?? "http://minio:9000";

            _isConfigured = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey);
            if (!_isConfigured)
                return;

            var credentials = new BasicAWSCredentials(
                accessKey,
                secretKey
            );

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
                SignatureVersion = "4",
                Timeout = TimeSpan.FromSeconds(60),
                MaxErrorRetry = 2
            };

            _s3Client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> UploadFileInS3Async(IFormFile file, Guid cowId)
        {
            if (file == null || file.Length == 0)
                return null;
            if (!_isConfigured || _s3Client == null)
                throw new InvalidOperationException("MinIO не настроен. Задайте MinIO:AccessKey и MinIO:SecretKey через конфигурацию.");

            try
            {
                await EnsureBucketExistsAsync();

                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{cowId}{fileExtension}";

                using var stream = file.OpenReadStream();

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    AutoCloseStream = false
                };

                var response = await _s3Client.PutObjectAsync(request);

                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка загрузки файла в MinIO: {ex.Message}", ex);
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            if (!_isConfigured || _s3Client == null)
                return;

            try
            {
                var bucketExists = await _s3Client.DoesS3BucketExistAsync(_bucketName);
                if (!bucketExists)
                {
                    await _s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = _bucketName
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при проверке/создании бакета MinIO: {ex.Message}", ex);
            }
        }

        public async Task<bool> CheckS3AccessAsync()
        {
            if (!_isConfigured || _s3Client == null)
                return false;

            try
            {
                var bucketsResponse = await _s3Client.ListBucketsAsync();
                return bucketsResponse.Buckets.Count >= 0 &&
                       await _s3Client.DoesS3BucketExistAsync(_bucketName);
            }
            catch (AmazonS3Exception)
            {
            }
            catch (Exception)
            {
            }

            return false;
        }

        public string GetFileUrl(string fileName)
        {
            return $"{_baseUrl}/{_bucketName}/{fileName}";
        }
    }
}
