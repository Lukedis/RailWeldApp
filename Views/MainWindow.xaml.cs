using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Views
{
    /// <summary>
    /// 窗口壳。页面内容交给 <c>ViewHost</c>（绑定 <c>MainWindowViewModel.CurrentView</c> + DataTemplate），
    /// 这里只保留窗口自身的事：圆角偏好。
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;

            // 默认铺满可用区域，四周留一点缝，保留圆角（最大化会被系统改成直角）
            SourceInitialized += (_, _) =>
            {
                Rect area = SystemParameters.WorkArea;

                Width = Math.Max(area.Width - 16, 1600);
                Height = Math.Max(area.Height - 16, 900);
                Left = area.Left + 8;
                Top = area.Top + 8;

                ApplyRoundedWindowCorners();
            };
        }

        private void ApplyRoundedWindowCorners()
        {
            if (Environment.OSVersion.Version.Build < 22000)
            {
                // Windows 11 之前没有圆角偏好这个属性
                return;
            }

            const int dwmWindowCornerPreference = 33;
            const int dwmWindowCornerRound = 2;

            var handle = new WindowInteropHelper(this).Handle;

            if (handle == IntPtr.Zero)
            {
                return;
            }

            var preference = dwmWindowCornerRound;

            _ = DwmSetWindowAttribute(handle, dwmWindowCornerPreference, ref preference, sizeof(int));
        }

        [DllImport("dwmapi.dll", SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
