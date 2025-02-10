using System;
using System.Drawing;
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

            //_ = serviceProvider.GetRequiredService<TaskbarIcon>();

            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Activate();
            mainWindow.Show();
        }


        private ServiceProvider BuildServiceProvider()
                => new ServiceCollection()
                    .AddSingleton<ITokenCredentialProvider, TokenCredentialProvider>()
                    .AddSingleton<IItemsProvider, FileSystemSolutionsProvider>(pb => pb
                        .Value(@"f:\github\")
                        .Value(@"*.sln")
                    )
                    .AddSingleton<IItemsProvider, AzureManagementProvider>()
                    .AddSingleton<IItemsProvider, AzureDevopsProvider>()
                    .AddSingleton<IItemsProvider, ProcessWindowsProvider>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<MainWindow>()
                    .AddSingleton<IItemLauncher, ItemLauncher>()
                    .BuildServiceProvider();
    }
}
