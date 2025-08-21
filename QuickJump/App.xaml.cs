using System.Windows;
using DependencyInjection.Extensions.Parameterization;
using Microsoft.Extensions.DependencyInjection;
using QuickJump.Providers;
using QuickJump.ViewModels;

namespace QuickJump
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceProvider = BuildServiceProvider();
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Activate();
            mainWindow.Show();
        }

        private static ServiceProvider BuildServiceProvider()
                => new ServiceCollection()
                    .AddSingleton<ITokenCredentialProvider, TokenCredentialProvider>()
                    .AddSingleton<IItemsProvider, FileSystemSolutionsProvider>(pb => pb
                        .Value(@"c:\git\")
                        .Value(@"*.sln")
                    )
                    .AddSingleton<IItemsProvider, AzureManagementProvider>(pb => pb
                        .Value(new[] { "cfoportal", "poseidon" })
                    )
                    .AddSingleton<IItemsProvider, AzureDevopsProvider>(pb => pb
                        .Value(new[] { "SDBI", "Tribe External Reporting" })
                    )
                    .AddSingleton<IItemsProvider, ProcessWindowsProvider>()
                    .AddSingleton<IItemsProvider, EdgeBookmarksProvider>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<MainWindow>()
                    .AddSingleton<IItemLauncher, ItemLauncher>()
                    .BuildServiceProvider();
    }
}
