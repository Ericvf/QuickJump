using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QuickJump.Providers
{
    public class ProcessWindowsProvider : IItemsProvider
    {
        public string Name => nameof(ProcessWindowsProvider);

        public bool LoadDataOnActivate => true;

        public async Task GetItems(Func<Item, Task> value, CancellationToken cancellationToken)
        {
            var processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    var item = new Item()
                    {
                        Provider = Name,
                        Name = process.MainWindowTitle,
                        Category = Categories.ProcessWindow,
                        Description = $"{process.Id} {process.ProcessName} {process.MainWindowTitle} proc",
                        Id = process.MainWindowHandle.ToString(),
                        Type = Types.ProcessId,
                        Icon = "process",
                        Path = process.MainWindowHandle.ToString()
                    };

                    await value(item);
                }
            }
        }
    }
}
