using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public interface IItemsProvider
    {
        Task<IEnumerable<Item>> GetItems();
    }
}
