using Azure.Core;

namespace QuickJump.Providers
{
    public interface ITokenCredentialProvider {
        TokenCredential GetCredential();
    }
}
