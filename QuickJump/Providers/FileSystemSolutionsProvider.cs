using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public class FileSystemSolutionsProvider : IItemsProvider
    {
        public async Task<IEnumerable<Item>> GetItems()
        {
            var allFiles = Directory.EnumerateFiles("c:\\git\\rabocop", "*.sln", SearchOption.AllDirectories);

            var result = allFiles.Select(f => MapFileToItem(f)).ToList();
            return result;
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
                Description = directoryName,
                Type = Types.File,
                Category = Categories.Solution,
            };
        }
    }
}
