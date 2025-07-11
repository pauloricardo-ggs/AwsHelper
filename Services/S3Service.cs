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

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                Timeout = TimeSpan.FromMinutes(15), // Timeout de 15 minutos para uploads
                MaxErrorRetry = 3,
                RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
                UseHttp = false, // Forçar HTTPS
                BufferSize = 8192 * 4 // Buffer maior para uploads
            };

            return new AmazonS3Client(credentials, config);
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

        public async Task<string> UploadObjectAsync(string profileName, string bucketName, string key, Stream fileStream, string contentType = "application/octet-stream")
        {
            try
            {
                using var s3Client = CreateS3Client(profileName);
                
                // Para arquivos maiores que 100MB, usar multipart upload
                if (fileStream.Length > 100 * 1024 * 1024)
                {
                    return await UploadLargeObjectAsync(s3Client, bucketName, key, fileStream, contentType);
                }
                
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                // Usar CancellationToken com timeout personalizado
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
                var response = await s3Client.PutObjectAsync(request, cts.Token);
                
                _logger.LogInformation("Arquivo {Key} enviado com sucesso para o bucket {BucketName} usando o perfil {ProfileName}", key, bucketName, profileName);
                return response.ETag;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout ao fazer upload do arquivo {Key} para o bucket {BucketName} usando o perfil {ProfileName}. O upload foi cancelado por timeout.", key, bucketName, profileName);
                throw new InvalidOperationException($"O upload do arquivo '{key}' foi cancelado por timeout. Tente novamente ou verifique sua conexão.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do arquivo {Key} para o bucket {BucketName} usando o perfil {ProfileName}", key, bucketName, profileName);
                throw;
            }
        }

        private async Task<string> UploadLargeObjectAsync(IAmazonS3 s3Client, string bucketName, string key, Stream fileStream, string contentType)
        {
            _logger.LogInformation("Iniciando upload multipart para arquivo grande: {Key}", key);
            
            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            var initResponse = await s3Client.InitiateMultipartUploadAsync(initiateRequest);
            var uploadId = initResponse.UploadId;

            try
            {
                const int partSize = 5 * 1024 * 1024; // 5MB por parte
                var partETags = new List<PartETag>();
                var partNumber = 1;
                var buffer = new byte[partSize];

                while (true)
                {
                    var bytesRead = await fileStream.ReadAsync(buffer, 0, partSize);
                    if (bytesRead == 0) break;

                    using var partStream = new MemoryStream(buffer, 0, bytesRead);
                    var uploadRequest = new UploadPartRequest
                    {
                        BucketName = bucketName,
                        Key = key,
                        UploadId = uploadId,
                        PartNumber = partNumber,
                        InputStream = partStream
                    };

                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    var uploadResponse = await s3Client.UploadPartAsync(uploadRequest, cts.Token);
                    partETags.Add(new PartETag(partNumber, uploadResponse.ETag));
                    
                    _logger.LogDebug("Parte {PartNumber} enviada para {Key}", partNumber, key);
                    partNumber++;
                }

                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    UploadId = uploadId,
                    PartETags = partETags
                };

                var completeResponse = await s3Client.CompleteMultipartUploadAsync(completeRequest);
                _logger.LogInformation("Upload multipart concluído com sucesso para {Key}", key);
                
                return completeResponse.ETag;
            }
            catch (Exception)
            {
                // Abortar upload multipart em caso de erro
                await s3Client.AbortMultipartUploadAsync(bucketName, key, uploadId);
                throw;
            }
        }

        public async Task<string> UploadObjectAsync(string profileName, string bucketName, string key, byte[] fileBytes, string contentType = "application/octet-stream")
        {
            _logger.LogInformation("Iniciando upload de {Size} bytes para {BucketName}/{Key} usando perfil {ProfileName}", 
                fileBytes.Length, bucketName, key, profileName);
            
            using var stream = new MemoryStream(fileBytes);
            var result = await UploadObjectAsync(profileName, bucketName, key, stream, contentType);
            
            _logger.LogInformation("Upload concluído para {BucketName}/{Key}", bucketName, key);
            return result;
        }

        public async Task<bool> DeleteObjectAsync(string profileName, string bucketName, string key)
        {
            try
            {
                using var s3Client = CreateS3Client(profileName);
                
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                await s3Client.DeleteObjectAsync(request);
                
                _logger.LogInformation("Arquivo {Key} deletado com sucesso do bucket {BucketName} usando o perfil {ProfileName}", key, bucketName, profileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar o arquivo {Key} do bucket {BucketName} usando o perfil {ProfileName}", key, bucketName, profileName);
                throw;
            }
        }
    }
}
