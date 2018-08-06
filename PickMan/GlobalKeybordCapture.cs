using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PickMan
{
    // see: http://azumaya.s101.xrea.com/wiki/index.php?%B3%D0%BD%F1%2FC%A2%F4%2F%A5%B0%A5%ED%A1%BC%A5%D0%A5%EB%A5%D5%A5%C3%A5%AF
    public static class GlobalKeybordCapture
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Keys key);

        public const int WH_KEYBOARD_LL = 13;
        public const int HC_ACTION = 0;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        public sealed class KeybordCaptureEventArgs : EventArgs
        {
            internal KeybordCaptureEventArgs(KBDLLHOOKSTRUCT keyData)
            {
                KeyCode = keyData.vkCode;
                ScanCode = keyData.scanCode;
                Flags = keyData.flags;
                Time = keyData.time;
                Cancel = false;
            }

            public int KeyCode { get; }

            public int ScanCode { get; }

            public int Flags { get; }

            public int Time { get; }

            public bool Cancel { set; get; }
        }

        private static IntPtr s_hook;
        private static LowLevelKeyboardProc s_proc;
        public static event EventHandler<KeybordCaptureEventArgs> SysKeyDown;
        public static event EventHandler<KeybordCaptureEventArgs> KeyDown;
        public static event EventHandler<KeybordCaptureEventArgs> SysKeyUp;
        public static event EventHandler<KeybordCaptureEventArgs> KeyUp;

        static GlobalKeybordCapture()
        {
            BindKey();
            AppDomain.CurrentDomain.DomainUnload += (e, a) => { UnbindKey(); };
        }

        private static IntPtr HookProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            bool cancel = false;
            if (nCode == HC_ACTION)
            {
                KeybordCaptureEventArgs ev = new KeybordCaptureEventArgs(lParam);
                switch (wParam.ToInt32())
                {
                    case WM_KEYDOWN:
                        CallEvent(KeyDown, ev);
                        break;

                    case WM_KEYUP:
                        CallEvent(KeyUp, ev);
                        break;

                    case WM_SYSKEYDOWN:
                        CallEvent(SysKeyDown, ev);
                        break;

                    case WM_SYSKEYUP:
                        CallEvent(SysKeyUp, ev);
                        break;
                }
                cancel = ev.Cancel;
            }
            return cancel ? (IntPtr)1 : CallNextHookEx(s_hook, nCode, wParam, ref lParam);
        }

        public static bool IsCapture
        {
            get { return s_hook != IntPtr.Zero; }
        }

        public static bool DownCtrl()
        {
            return (GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
        }

        public static bool DownShift()
        {
            return (GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;
        }

        public static bool DownAlt()
        {
            return (GetAsyncKeyState(Keys.Alt) & 0x8000) != 0;
        }

        public static bool DownKey(Keys key)
        {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        public static void ReBindKey()
        {
            UnbindKey();
            BindKey();
        }

        private static void BindKey()
        {
            s_hook = SetWindowsHookEx(WH_KEYBOARD_LL,
                                      s_proc = new LowLevelKeyboardProc(HookProc),
                                      Marshal.GetHINSTANCE(typeof(GlobalKeybordCapture).Module),
                                      //Native.GetModuleHandle(null),
                                      0);
        }

        private static void UnbindKey()
        {
            if (s_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(s_hook);
                s_hook = IntPtr.Zero;
            }
        }

        private static void CallEvent(EventHandler<KeybordCaptureEventArgs> eh, KeybordCaptureEventArgs ev)
        {
            if (eh != null)
                eh(null, ev);
        }
    }
}
