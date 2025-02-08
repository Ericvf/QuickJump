using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace QuickJump.Providers
{
    public class AzureManagementProvider : IItemsProvider
    {
        private readonly ArmClient armClient;

        public AzureManagementProvider(ITokenCredentialProvider tokenCredentialProvider)
        {
            var tokenCredential = tokenCredentialProvider.GetCredential();
            armClient = new ArmClient(tokenCredential);
        }

        public string Name => nameof(AzureManagementProvider);

        public bool LoadDataOnActivate => false;

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            var subscriptions = armClient.GetSubscriptions();
            foreach (var subscription in subscriptions
                .Where(s => s.Data.DisplayName.Contains("cfoportal", System.StringComparison.OrdinalIgnoreCase))
            )
            {
                foreach (var resourceGroup in subscription.GetResourceGroups())
                {
                    await foreach (var resource in resourceGroup.GetGenericResourcesAsync(cancellationToken: cancellationToken))
                    {
                        var item = MapResourceToItem(resource);
                        await value(item);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        private Item MapResourceToItem(GenericResource resource)
        {
            return new Item
            {
                Id = resource.Id,
                Name = resource.Data.Name,
                Type = Types.Uri,
                Description = resource.Id,
                Path = $"https://portal.azure.com/#@/resource{resource.Id}",
                Category = Categories.Azure,
                Provider = Name,
            };
        }
    }
}
