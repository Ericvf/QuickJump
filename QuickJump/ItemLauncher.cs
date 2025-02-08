using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QuickJump
{
    public class ItemLauncher : IItemLauncher
    {
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(int hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public Task LaunchItem(Item item)
        {
            switch (item.Type)
            {
                case Types.File:
                    Process.Start(
                        new ProcessStartInfo(item.Id)
                        {
                            UseShellExecute = true
                        });
                    break;
                case Types.ProcessId:
                    var windowHandle = Convert.ToInt32(item.Path);
                    ShowWindow(windowHandle, SW_RESTORE);
                    SetForegroundWindow(windowHandle);
                    break;

                default:
                case Types.Uri:
                    Process.Start(
                        new ProcessStartInfo(item.Path ?? $"http://google.com?q={item.Name}")
                        {
                            UseShellExecute = true
                        });
                    break;

            }

            return Task.CompletedTask;
        }
    }
}
