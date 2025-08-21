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

        private ServiceProvider BuildServiceProvider()
                => new ServiceCollection()
                    .AddSingleton<ITokenCredentialProvider, TokenCredentialProvider>()
                    .AddSingleton<IItemsProvider, FileSystemSolutionsProvider>(pb => pb
                        .Value(@"c:\git\")
                        .Value(@"*.sln")
                    )
                    .AddSingleton<IItemsProvider, AzureManagementProvider>()
                    .AddSingleton<IItemsProvider, AzureDevopsProvider>()
                    .AddSingleton<IItemsProvider, ProcessWindowsProvider>()
                    .AddSingleton<IItemsProvider, EdgeBookmarksProvider>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<MainWindow>()
                    .AddSingleton<IItemLauncher, ItemLauncher>()
                    .BuildServiceProvider();
    }
}
