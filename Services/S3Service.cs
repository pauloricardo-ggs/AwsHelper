using Amazon.S3;
using Amazon.S3.Model;
using AwsHelper.Models;
using S3Object = AwsHelper.Models.S3Object;
using S3Bucket = AwsHelper.Models.S3Bucket;
using Amazon;

namespace AwsHelper.Services
{
    public class S3Service
    {
        private readonly AwsProfileService _profileService;
        private readonly ILogger<S3Service> _logger;

        public S3Service(AwsProfileService profileService, ILogger<S3Service> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        private IAmazonS3 CreateS3Client(string profileName)
        {
            var credentials = _profileService.GetCredentialsForProfile(profileName);
            if (credentials == null)
            {
                throw new ArgumentException($"Perfil '{profileName}' não encontrado ou inválido.");
            }

            return new AmazonS3Client(credentials, RegionEndpoint.USEast1);
        }

        public async Task<List<S3Bucket>> ListBucketsAsync(string profileName)
        {
            try
            {
                using var s3Client = CreateS3Client(profileName);
                var response = await s3Client.ListBucketsAsync();
                
                // Verificar se Buckets não é null antes de fazer Select
                if (response.Buckets == null)
                {
                    return new List<S3Bucket>();
                }
                
                return response.Buckets.Select(b => new S3Bucket
                {
                    Name = b.BucketName,
                    CreationDate = b.CreationDate ?? DateTime.MinValue
                }).OrderBy(b => b.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar buckets S3 para o perfil {ProfileName}", profileName);
                throw;
            }
        }

        public async Task<List<S3Object>> ListObjectsAsync(string profileName, string bucketName, string prefix = "", int maxKeys = 1000)
        {
            try
            {
                using var s3Client = CreateS3Client(profileName);
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = maxKeys,
                    Prefix = prefix
                };

                var objects = new List<S3Object>();
                ListObjectsV2Response response;
                
                do
                {
                    response = await s3Client.ListObjectsV2Async(request);
                    
                    // Verificar se S3Objects não é null antes de fazer Select
                    if (response.S3Objects != null && response.S3Objects.Any())
                    {
                        objects.AddRange(response.S3Objects.Select(obj => new S3Object
                        {
                            Key = obj.Key,
                            BucketName = bucketName,
                            LastModified = obj.LastModified ?? DateTime.MinValue,
                            Size = obj.Size ?? 0,
                            StorageClass = obj.StorageClass?.Value ?? string.Empty,
                            ETag = obj.ETag ?? string.Empty
                        }));
                    }
                    
                    request.ContinuationToken = response.NextContinuationToken;
                    
                } while (response.IsTruncated == true && objects.Count < maxKeys);

                return objects.OrderBy(o => o.Key).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar objetos do bucket {BucketName} para o perfil {ProfileName}", bucketName, profileName);
                throw;
            }
        }

        public async Task<Stream> DownloadObjectAsync(string profileName, string bucketName, string key)
        {
            try
            {
                using var s3Client = CreateS3Client(profileName);
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await s3Client.GetObjectAsync(request);
                return response.ResponseStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao baixar objeto {Key} do bucket {BucketName} para o perfil {ProfileName}", key, bucketName, profileName);
                throw;
            }
        }

        public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string profileName, string bucketName, string key)
        {
            try
            {
                using var s3Client = CreateS3Client(profileName);
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                return await s3Client.GetObjectMetadataAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter metadados do objeto {Key} do bucket {BucketName} para o perfil {ProfileName}", key, bucketName, profileName);
                throw;
            }
        }

        public async Task<bool> ObjectExistsAsync(string profileName, string bucketName, string key)
        {
            try
            {
                await GetObjectMetadataAsync(profileName, bucketName, key);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
