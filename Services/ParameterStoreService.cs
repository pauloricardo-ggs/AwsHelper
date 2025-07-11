using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace AwsHelper.Services;

public class ParameterStoreService
{
    public async Task UploadVariablesAsync(string input, string prefix, string accessKey, string secretKey)
    {
        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        using var ssmClient = new AmazonSimpleSystemsManagementClient(credentials, Amazon.RegionEndpoint.USEast1);

        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            var fullPath = $"{prefix.TrimEnd('/')}/{key}";

            var request = new PutParameterRequest
            {
                Name = fullPath,
                Value = value,
                Type = ParameterType.String,
                Overwrite = true
            };

            await ssmClient.PutParameterAsync(request);
        }
    }

    public async Task<(List<ParameterMetadata> Parameters, string? NextToken)> GetParametersAsync(
        string prefix, 
        string accessKey, 
        string secretKey, 
        int maxItems = 10, 
        string? nextToken = null,
        string? searchTerm = null)
    {
        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        using var ssmClient = new AmazonSimpleSystemsManagementClient(credentials, Amazon.RegionEndpoint.USEast1);

        var request = new GetParametersByPathRequest
        {
            Path = prefix.TrimEnd('/'),
            Recursive = true,
            MaxResults = maxItems,
            NextToken = nextToken
        };

        var response = await ssmClient.GetParametersByPathAsync(request);
        
        var parameters = response.Parameters.Select(p => new ParameterMetadata
        {
            Name = p.Name,
            Value = p.Value,
            Type = p.Type,
            LastModifiedDate = p.LastModifiedDate ?? DateTime.MinValue,
            ARN = p.ARN ?? "",
            Version = p.Version ?? 0
        }).ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            parameters = parameters.Where(p => 
                p.Name.ToLower().Contains(searchLower) ||
                p.Value.ToLower().Contains(searchLower)
            ).ToList();
        }

        return (parameters, response.NextToken);
    }
}

public class ParameterMetadata
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime LastModifiedDate { get; set; }
    public string ARN { get; set; } = "";
    public long Version { get; set; }
    public string LastModifiedUser { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Amazon.SimpleSystemsManagement.Model.Tag> Tags { get; set; } = new();
}