using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using KafeYana.Application.IServicios;
using KafeYana.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KafeYana.Infrastructure.Servicios
{
    public class R2StorageService : IR2StorageService
    {
        private readonly CloudflareR2Options _opts;
        private readonly AmazonS3Client _client;

        public R2StorageService(IOptions<CloudflareR2Options> opts)
        {
            _opts = opts.Value;

            var credentials = new BasicAWSCredentials(_opts.AccessKeyId, _opts.SecretAccessKey);

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_opts.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };

            _client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> SubirAsync(Stream stream, string contentType, string key)
        {
            var request = new PutObjectRequest
            {
                BucketName = _opts.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                DisablePayloadSigning = true
            };

            await _client.PutObjectAsync(request);

            return $"{_opts.PublicUrl.TrimEnd('/')}/{key}";
        }

        public async Task EliminarAsync(string key)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _opts.BucketName,
                Key = key
            };

            await _client.DeleteObjectAsync(request);
        }
    }
}
