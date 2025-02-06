using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace QuickJump.Providers
{
    public class AzureManagementProvider : IItemsProvider
    {
        public async Task<IEnumerable<Item>> GetItems()
        {
            var cacheOptions = new TokenCachePersistenceOptions
            {
                Name = "AzureManagementProviderTokenCache",
                UnsafeAllowUnencryptedStorage = false,
            };

            var credentialOptions = new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = cacheOptions
            };

            var armClient = new ArmClient(new InteractiveBrowserCredential(credentialOptions));

            var subscriptions = armClient.GetSubscriptions();
            var results = new List<Item>();

            foreach (var subscription in subscriptions.Where(s => s.Data.DisplayName.Contains("cfoportal", System.StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var resourceGroup in subscription.GetResourceGroups())
                {
                    foreach (var resource in resourceGroup.GetGenericResources())
                    {
                        var item = MapResourceToItem(resource);
                        results.Add(item);  
                    }
                }
            }

            return results.ToArray();
        }

        private static Item MapResourceToItem(GenericResource resource)
        {
            return new Item
            {
                Id = resource.Id,
                Name = resource.Data.Name,
                Type = Types.Uri,
                Description = resource.Id,
                Path = $"https://portal.azure.com/#@/resource{resource.Id}",
                Category = Categories.Azure,
            };
        }
    }
}
