using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public class FileSystemSolutionsProvider : IItemsProvider
    {
        public string Name => nameof(FileSystemSolutionsProvider);

        public bool LoadDataOnActivate => false;

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            await Task.Delay(1000);
            var allFiles = Directory.EnumerateFiles("c:\\git\\", "*.sln", SearchOption.AllDirectories);

            var result = allFiles.Select(f => MapFileToItem(f)).ToList();
            foreach (var item in result)
            {
                await value(item);
            }
        }

        private Item MapFileToItem(string filePath)
        {
            var id = filePath;
            var fileName = Path.GetFileName(filePath);
            var directoryName = Path.GetDirectoryName(filePath);

            return new Item
            {
                Id = id,
                Name = fileName,
                Description = id,
                Type = Types.File,
                Category = Categories.Solution,
                Provider = Name,
            };
        }
    }
}
