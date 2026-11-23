using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RailWeldApp.Views
{
    public partial class AdminDashboardView : UserControl
    {
        private static readonly TimeSpan _fadeDuration = TimeSpan.FromMilliseconds(450);
        private readonly Dictionary<FrameworkElement, TranslateTransform> _transforms = new();
        private readonly List<FrameworkElement> _pending = [];

        public AdminDashboardView()
        {
            InitializeComponent();

            // 为控件注册滚动淡入动画
            RegisterScrollFade(FirstScreen);
            RegisterScrollFade(NumberBar);
            RegisterScrollFade(ProductCapability);
            RegisterScrollFade(Realtimedata);
            RegisterScrollFade(Threestageprocess);
            RegisterScrollFade(Narrowcardhorizontallayout);
            RegisterScrollFade(BigCard);
            RegisterScrollFade(End);

            // 在滚动事件中更新动画
            Loaded += (_, _) => UpdateScrollAnimations();
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateScrollAnimations();
        }

        private void UpdateScrollAnimations()
        {
            if (!IsLoaded || _pending.Count == 0)
                return;

            // 只检查还没出现的元素；全部出现后滚动不再做布局转换。
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                FrameworkElement element = _pending[i];

                if (!element.IsLoaded)
                    continue;

                Point position = element.TransformToAncestor(MainScrollViewer).Transform(new Point(0, 0));

                var visible = position.Y < MainScrollViewer.ViewportHeight && position.Y + element.ActualHeight > 0;

                if (!visible)
                    continue;

                _pending.RemoveAt(i);

                ShowElement(element);
            }
        }

        private void ShowElement(FrameworkElement element)
        {
            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, _fadeDuration));

            if (_transforms.TryGetValue(element, out TranslateTransform? transform))
            {
                transform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(40, 0, _fadeDuration)
                    {
                        EasingFunction = new CubicEase
                        {
                            EasingMode = EasingMode.EaseOut
                        }
                    });
            }
        }

        private void RegisterScrollFade(FrameworkElement element)
        {
            element.Opacity = 0;

            // 设置初始的位移变换
            var transform = new TranslateTransform(0, 40);

            element.RenderTransform = transform;

            _transforms[element] = transform;
            _pending.Add(element);
        }
    }
}
