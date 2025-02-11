using System;
using System.Runtime.InteropServices;

namespace QuickJump
{
    public class TrayIconHelper
    {
        private const int NIM_ADD = 0x00000000;
        private const int NIM_DELETE = 0x00000002;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int WM_USER = 0x0400;
        private const int IDI_APPLICATION = 0x7F00;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static void AddTrayIcon(IntPtr windowHandle, string tooltip)
        {
            IntPtr hInstance = GetModuleHandle(null); 
            var myIcon = LoadIcon(hInstance, IDI_APPLICATION);

            NOTIFYICONDATA nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = windowHandle,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_USER + 1,
                hIcon = myIcon,
                szTip = tooltip
            };

            Shell_NotifyIcon(NIM_ADD, ref nid);
        }

        public static void RemoveTrayIcon(IntPtr windowHandle)
        {
            NOTIFYICONDATA nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = windowHandle,
                uID = 1
            };

            Shell_NotifyIcon(NIM_DELETE, ref nid);
        }
    }
}
