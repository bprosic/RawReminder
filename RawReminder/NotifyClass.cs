using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RawReminder
{
    /// <summary>
    /// https://codereview.stackexchange.com/questions/127742/minimize-the-console-window-to-tray
    /// </summary>
    class NotifyClass
    {

        #region pInvoke
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        private enum ShowWindowCommands
        {
            Hide = 0,
            Normal = 1,
            ShowMinimized = 2,
            Maximize = 3,
            ShowMaximized = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNA = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11
        }

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        private const uint SC_CLOSE = 0xF060;
        private const uint MF_ENABLED = 0x00000000;
        private const uint MF_DISABLED = 0x00000002;

        #endregion

        private static NotifyIcon Tray = default(NotifyIcon);
        private static IntPtr Me = default(IntPtr);

        public void activateConsole()
        {
            // Get The Console Window Handle
            Me = GetConsoleWindow();

            // Disable Close Button (X)
            EnableMenuItem(GetSystemMenu(Me, false), SC_CLOSE, (uint)(MF_ENABLED | MF_DISABLED));
            Console.Title = "Reminder";
            MenuItem mExit = new MenuItem("Exit", new EventHandler(Exit));
            MenuItem mShow = new MenuItem("Show console", new EventHandler(ShowConsole));
            ContextMenu Menu = new ContextMenu(new MenuItem[] { mShow, mExit });

            Tray = new NotifyIcon()
            {
                Icon = new Icon("Reminder.ico"),
                Visible = true,
                Text = Console.Title,
                ContextMenu = Menu
            };

            Tray.DoubleClick += new EventHandler(DoubleClick);

            // Detect When The Console Window is Minimized and Hide it
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (WindowPlacement())
                        ShowWindow(Me, (int)ShowWindowCommands.Hide);
                    Thread.Sleep(1000);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Application.Run();
        }

        private static bool WindowPlacement()
        {
            WINDOWPLACEMENT wPlacement = new WINDOWPLACEMENT();
            GetWindowPlacement(Me, ref wPlacement);
            if (wPlacement.showCmd == (int)ShowWindowCommands.ShowMinimized)
                return true;
            else
                return false;

        }

        private static void DoubleClick(object sender, EventArgs e)
        {
            if (WindowPlacement())
                ShowWindow(Me, (int)ShowWindowCommands.Restore);
            else
                ShowWindow(Me, (int)ShowWindowCommands.Hide);
        }

        public void ForceExit()
        {
            Tray.Dispose();
            Application.Exit();
            Environment.Exit(1);
        }

        private static void Exit(object sender, EventArgs e)
        {
            Tray.Dispose();
            Application.Exit();
            Environment.Exit(1);
        }

        private static void ShowConsole(object sender, EventArgs e)
        {
            ShowWindow(Me, (int)ShowWindowCommands.Restore);
        }

        private static void Wait(int timeout)
        {
            using (AutoResetEvent AREv = new AutoResetEvent(false))
                AREv.WaitOne(timeout, true);
        }
    }
}