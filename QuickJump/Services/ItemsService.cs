using QuickJump.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickJump.Services
{
    public interface IItemsService
    {
        Task<IEnumerable<Item>> GetAllItems();
    }

    public class ItemsService : IItemsService
    {
        private readonly IEnumerable<IItemsProvider> itemsProviders;

        public ItemsService(IEnumerable<IItemsProvider> itemsProviders)
        {
            this.itemsProviders = itemsProviders;
        }

        public async Task<IEnumerable<Item>> GetAllItems()
        {
            var results = await Task.WhenAll(itemsProviders.Select(i => i.GetItems()).ToArray());
            return results.SelectMany(f => f);
        }
    }
}
