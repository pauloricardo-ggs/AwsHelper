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
}