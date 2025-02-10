using DependencyInjection.Extensions.Parameterization;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using QuickJump.Providers;
using QuickJump.ViewModels;
using System;
using System.Drawing;
using System.Windows;

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
                        .Value(@"c:\git\")
                        .Value(@"*.sln")
                    )
                    .AddSingleton<IItemsProvider, AzureManagementProvider>()
                    .AddSingleton<IItemsProvider, AzureDevopsProvider>()
                    .AddSingleton<IItemsProvider, ProcessWindowsProvider>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<MainWindow>()
                    .AddSingleton<IItemLauncher, ItemLauncher>()
                    .AddSingleton(provider =>
                       new TaskbarIcon
                       {
                           Icon = new Icon(GetResourceStream(new Uri("pack://application:,,,/QuickJump;component/Resources/trayicon.ico")).Stream),
                           ToolTipText = "QuickJump",
                       })
                    .BuildServiceProvider();
    }
}
