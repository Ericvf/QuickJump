using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickJump
{
    class HotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        private const int ERROR_HOTKEY_ALREADY_REGISTERED = 0x581;

        [DllImport("user32.dll", SetLastError = true)]
        private extern static bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private extern static bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource source;
        private HwndSourceHook hook;
        private List<int> ids;

        public event EventHandler<PressedEventArgs> Pressed;
        public event EventHandler<ButtonEventArgs> IconClicked;

        public HotkeyManager(Window window)
        {
            source = (HwndSource)PresentationSource.FromVisual(window);
            hook = new HwndSourceHook(WndProc);
            source.AddHook(hook);
            ids = new List<int>();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);

        private const int WM_CANCELMODE = 0x001F;
        private const int WM_INITMENUPOPUP = 0x0117;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSCHAR = 0x0106;

        private const int WM_USER = 0x0400;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_USER + 1:
                    switch ((int)lParam)
                    {
                        case WM_LBUTTONDOWN:
                            IconClicked?.Invoke(this, new ButtonEventArgs(false));
                            handled = true;
                            break;
                        case WM_RBUTTONDOWN:
                            Debug.WriteLine("right click");
                            IconClicked?.Invoke(this, new ButtonEventArgs(true));
                            handled = true;
                            break;
                    }
                    break;
                //case WM_SYSKEYDOWN:
                case WM_SYSKEYUP:
                case WM_SYSCHAR:
                    handled = true;
                    return IntPtr.Zero;
                case WM_HOTKEY:
                    {
                        Pressed?.Invoke(this, new PressedEventArgs((int)wParam));
                        handled = true;
                        break;
                    }

                case WM_INITMENUPOPUP:
                    // If the high word is set to 1, the PopupMenu is the Window Menu
                    if (lParam.ToInt32() >> 16 == 1)
                    {
                        SendMessage(hwnd, WM_CANCELMODE, 0, 0);
                    }
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        public void Register(int id, ModifierKeys modifiers, Key key)
        {
            if (!RegisterHotKey(source.Handle, id, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key)))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == ERROR_HOTKEY_ALREADY_REGISTERED)
                {
                    throw new HotkeyAlreadyRegisteredException(modifiers, key);
                }
                else
                {
                    throw new Win32Exception(error);
                }
            }

            ids.Add(id);
        }

        public void Unregister(int id)
        {
            if (!UnregisterHotKey(source.Handle, id))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void Dispose()
        {
            foreach (int id in ids)
            {
                Unregister(id);
            }
            source.RemoveHook(hook);
        }
    }

    class PressedEventArgs : EventArgs
    {
        public int Id { get; private set; }

        public PressedEventArgs(int id)
        {
            Id = id;
        }
    }

    class ButtonEventArgs : EventArgs
    {
        public bool IsRight { get; private set; }

        public ButtonEventArgs(bool isRight)
        {
            IsRight = isRight;
        }
    }
    class HotkeyAlreadyRegisteredException : Exception
    {
        public ModifierKeys Modifiers { get; private set; }

        public Key Key { get; private set; }

        public HotkeyAlreadyRegisteredException(ModifierKeys modifiers, Key key)
        {
            Modifiers = modifiers;
            Key = key;
        }
    }
}
