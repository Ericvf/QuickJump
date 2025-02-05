using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public class GithubSolutionsProvider : IItemsProvider
    {
        public Task<IEnumerable<Item>> GetItems()
        {
            var allFiles = Directory.GetFiles("f:\\github", "*.sln", SearchOption.AllDirectories);

            var result = allFiles.Select(f => MapFileToItem(f)).ToList();
            //result.AddRange(result);
            //result.AddRange(result);
            //result.AddRange(result);
            //result.AddRange(result);
            //result.AddRange(result);
            //result.AddRange(result);

            return Task.FromResult(result.AsEnumerable());
        }

        private static Item MapFileToItem(string filePath)
        {
            var id = filePath;
            var fileName = Path.GetFileName(filePath);
            var directoryName = Path.GetDirectoryName(filePath);

            return new Item
            {
                Id = id,
                Name = fileName,
                Description = filePath,
                Type = Item.Types.File,
            };
        }
    }
}
