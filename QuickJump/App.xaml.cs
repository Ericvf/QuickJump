using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using QuickJump.Providers;
using QuickJump.Services;
using QuickJump.ViewModels;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;

namespace QuickJump
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceProvider = BuildServiceProvider();
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();

            _ = serviceProvider.GetRequiredService<TaskbarIcon>();

            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();


            //mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Activate();
            mainWindow.Show();
        }

        private ServiceProvider BuildServiceProvider()
            => new ServiceCollection()
                .AddSingleton<IItemsService, ItemsService>()
                .AddSingleton<IItemsProvider, StaticItemsProvider>()
                .AddSingleton<IItemsProvider, GithubSolutionsProvider>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>()
                .AddSingleton(provider =>
                   new TaskbarIcon
                   {
                       Icon = new Icon("icon.ico"),
                       ToolTipText = "QuickJump",
                   })
                .BuildServiceProvider();
    }
}
