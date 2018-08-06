using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace PickMan
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            CopyCommand = new DelegateCommand(() =>
            {
                System.Windows.Clipboard.SetText(ColorCode.Text);
                Close();
            });

            CloseCommand = new DelegateCommand(() => Close());

            InitializeComponent();

            notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Icon = Properties.Resources.icon;
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("キーの再バインド(&R)", null, (sender, args) => { GlobalKeybordCapture.ReBindKey(); });
            notifyIcon.ContextMenuStrip.Items.Add("終了(&X)", null, (sender, args) => { System.Windows.Application.Current.Shutdown(); });
            notifyIcon.MouseClick += notifyIcon_MouseClick;

            GlobalKeybordCapture.KeyDown += (sender, args) =>
            {
                if (this.Visibility != Visibility.Visible
                    && (GlobalKeybordCapture.DownCtrl() || args.KeyCode == (int)Keys.LControlKey || args.KeyCode == (int)Keys.RControlKey)
                    && (GlobalKeybordCapture.DownShift() || args.KeyCode == (int)Keys.LShiftKey || args.KeyCode == (int)Keys.RShiftKey)
                    && (GlobalKeybordCapture.DownKey(Keys.P) || args.KeyCode == (int)Keys.P))
                {
                    ShowWindow();
                    args.Cancel = true;
                }
            };
        }

        public ICommand CopyCommand { get; }

        public ICommand CloseCommand { get; }

        NotifyIcon notifyIcon { get; }

        void ShowWindow()
        {
            Rectangle bound = Screen.FromHandle(((HwndSource)HwndSource.FromVisual(this)).Handle).WorkingArea;
            Visibility = Visibility.Visible;
            Left = (bound.Width - ActualWidth) * 0.5;
            Top = (bound.Height - ActualHeight) * 0.5;
        }

        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowWindow();
                }
            }
            catch { }
        }

        private void RootWindow_Closing(object sender, CancelEventArgs e)
        {
            Visibility = Visibility.Collapsed;
            e.Cancel = true;
        }

        private void RootWindow_Closed(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }

    class DelegateCommand : ICommand
    {
        Action Command { get; }

        Func<bool> CanExecutePredication { get; }

        public DelegateCommand(Action command)
        {
            Command = command;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Command();
        }
    }
}
