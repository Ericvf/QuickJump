using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public interface IItemsProvider
    {
        Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken);

        string Name { get; }

        bool LoadDataOnActivate { get; }
    }
}
