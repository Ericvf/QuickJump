using System.IO;
using Azure.Core;
using Azure.Identity;

namespace QuickJump.Providers
{
    public class TokenCredentialProvider : ITokenCredentialProvider
    {
        private const string authenticationRecordCache = "authenticationRecordCache.json";
        private const string TokenCacheName = "TokenCredentialProvider";

        public TokenCredential GetCredential()
        {
            var cacheOptions = new TokenCachePersistenceOptions
            {
                Name = TokenCacheName,
                UnsafeAllowUnencryptedStorage = false,
            };

            AuthenticationRecord authRecord = null;

            if (File.Exists(authenticationRecordCache))
            {
                using var authRecordStream = new FileStream(authenticationRecordCache, FileMode.Open, FileAccess.Read);
                authRecord = AuthenticationRecord.Deserialize(authRecordStream);
            }

            var credentialOptions = new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = cacheOptions,
                AuthenticationRecord = authRecord,
            };

            var credential = new InteractiveBrowserCredential(credentialOptions);

            if (authRecord == null)
            {
                authRecord = credential.Authenticate();
                using var authRecordStream = new FileStream(authenticationRecordCache, FileMode.Create, FileAccess.Write);
                authRecord.Serialize(authRecordStream);
            }

            return credential;
        }
    }
}
