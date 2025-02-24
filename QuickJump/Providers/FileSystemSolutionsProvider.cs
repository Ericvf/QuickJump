using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public class FileSystemSolutionsProvider : IItemsProvider
    {
        private readonly string filePath;
        private readonly string filePattern;

        public string Name => nameof(FileSystemSolutionsProvider);

        public bool LoadDataOnActivate => false;

        public FileSystemSolutionsProvider(string filePath, string filePattern)
        {
            this.filePath = filePath;
            this.filePattern = filePattern;
        }

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            await Task.Delay(250);

            var allFiles = SafeWalk(Directory.EnumerateFiles(filePath, filePattern, SearchOption.AllDirectories));

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

            return new Item
            {
                Id = id,
                Name = fileName,
                Description = id,
                Type = Types.File,
                Category = Categories.Solution,
                Provider = Name,
                Icon = "visualstudio",

            };
        }

        private static IEnumerable<T> SafeWalk<T>(IEnumerable<T> source)
        {
            var enumerator = source.GetEnumerator();
            bool? hasCurrent = null;

            do
            {
                try
                {
                    hasCurrent = enumerator.MoveNext();
                }
                catch
                {
                    hasCurrent = null;
                }

                if (hasCurrent ?? false)
                    yield return enumerator.Current;

            } while (hasCurrent ?? true);
        }
    }
}
