using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace QuickJump.Providers
{
    public class AzureManagementProvider : IItemsProvider
    {
        private readonly ArmClient armClient;
        private readonly string[]? subscriptionFilters;

        public AzureManagementProvider(ITokenCredentialProvider tokenCredentialProvider, string[]? subscriptionFilters = null)
        {
            var tokenCredential = tokenCredentialProvider.GetCredential();
            armClient = new ArmClient(tokenCredential);

            this.subscriptionFilters = subscriptionFilters;
        }

        public string Name => nameof(AzureManagementProvider);

        public string Key => Name;

        public bool LoadDataOnActivate => false;

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            var subscriptions = armClient.GetSubscriptions();
            var filteredSubscriptions = subscriptions.Where(s => subscriptionFilters?.Any(f => s.Data.DisplayName.Contains(f)) ?? true);
            foreach (var subscription in filteredSubscriptions)
            {
                foreach (var resourceGroup in subscription.GetResourceGroups())
                {
                    var item2 = MapResourceToItem(resourceGroup);
                    await value(item2);

                    await foreach (var resource in resourceGroup.GetGenericResourcesAsync(cancellationToken: cancellationToken))
                    {
                        var item = MapResourceToItem(resource);
                        await value(item);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        private Item MapResourceToItem(ResourceGroupResource resourceGroup)
        {
            return new Item
            {
                Id = resourceGroup.Id,
                Name = resourceGroup.Data.Name,
                Type = Types.Uri,
                Description = $"{resourceGroup.Data.ResourceType.Type} -  {resourceGroup.Id}",
                Path = $"https://portal.azure.com/#@/resource{resourceGroup.Id}",
                Category = Categories.Azure,
                Icon = MapIcon(resourceGroup.Data.ResourceType),
                Provider = Key,
            };
        }

        private Item MapResourceToItem(GenericResource resource)
        {
            return new Item
            {
                Id = resource.Id,
                Name = resource.Data.Name,
                Type = Types.Uri,
                Description = $"{resource.Data.ResourceType.Type} -  {resource.Id}" ,
                Path = $"https://portal.azure.com/#@/resource{resource.Id}",
                Category = Categories.Azure,
                Icon = MapIcon(resource.Data.ResourceType),
                Provider = Key,
            };
        }

        private static string MapIcon(ResourceType resourceType)
        {
            switch (resourceType.Type)
            {
                case "sites": return "appservice";
                case "components": return "insights";
                case "servers": return "sqlserver";
                case "databases": return "sqlserver";
                case "networkWatchers/flowLogs": return "networkwatcher";
                case "serverFarms": return "plan";
                case "resourceGroups": return "resourcegroup";
                case "vaults": return "keyvault";
                case "virtualNetworks": return "virtualnetwork";
                case "frontdoors": return "frontdoor";
                case "storageAccounts": return "storage";
                case "networkSecurityGroups": return "nsg";

                default:
                    return "azureportal";
            }
        }
    }
}
