using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
using Amazon;

namespace AwsHelper.Services
{
    public class AwsProfileService
    {
        public List<string> GetAvailableProfiles()
        {
            var credsFile = new SharedCredentialsFile();
            return credsFile.ListProfileNames().OrderBy(p => p).ToList();
        }

        public AWSCredentials? GetCredentialsForProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return null;

            var credsFile = new SharedCredentialsFile();
            if (credsFile.TryGetProfile(profileName, out var profile))
            {
                return profile.GetAWSCredentials(null);
            }

            return null;
        }

        public bool ProfileExists(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;

            var credsFile = new SharedCredentialsFile();
            return credsFile.TryGetProfile(profileName, out _);
        }

        public (string AccessKey, string SecretKey)? GetProfileKeys(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return null;

            var credsFile = new SharedCredentialsFile();
            if (credsFile.TryGetProfile(profileName, out var profile))
            {
                return (profile.Options.AccessKey, profile.Options.SecretKey);
            }

            return null;
        }
    }
}
