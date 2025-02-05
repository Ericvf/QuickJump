using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public class StaticItemsProvider : IItemsProvider
    {
        public Task<IEnumerable<Item>> GetItems()
        {
            var x = new List<Item>([
                new Item { Id = "Apple", Name = "Apple" },
            new Item { Id = "Banana", Name = "Banana" },
            new Item { Id = "Cherry", Name = "Cherry" },
            new Item { Id = "Date", Name = "Date" },
            new Item { Id = "Elderberry", Name = "Elderberry" },
            new Item { Id = "Fig", Name = "Fig" },
            new Item { Id = "Grape", Name = "Grape" },
            new Item { Id = "Honeydew", Name = "Honeydew" },
            new Item { Id = "Indian Fig", Name = "Indian Fig" },
            new Item { Id = "Jackfruit", Name = "Jackfruit" },
            new Item { Id = "Kiwi", Name = "Kiwi" },
            new Item { Id = "Lemon", Name = "Lemon" },
            new Item { Id = "Mango", Name = "Mango" },
            new Item { Id = "Nectarine", Name = "Nectarine" },
            new Item { Id = "Orange", Name = "Orange" },
            new Item { Id = "Papaya", Name = "Papaya" },
            new Item { Id = "Quince", Name = "Quince" },
            new Item { Id = "Raspberry", Name = "Raspberry" },
            new Item { Id = "Strawberry", Name = "Strawberry" },
            new Item { Id = "Tangerine", Name = "Tangerine" },
            new Item { Id = "Ugli Fruit", Name = "Ugli Fruit" },
            new Item { Id = "Vanilla Bean", Name = "Vanilla Bean" },
            new Item { Id = "Watermelon", Name = "Watermelon" },
            new Item { Id = "Xigua (Chinese Watermelon)", Name = "Xigua (Chinese Watermelon)" },
            new Item { Id = "Yellow Passionfruit", Name = "Yellow Passionfruit" },
            new Item { Id = "Zucchini", Name = "Zucchini" },
            new Item { Id = "Dragon Fruit", Name = "Dragon Fruit" }
            ]);


            return Task.FromResult(x.AsEnumerable());
        }
    }
}
