using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RailWeldApp.Interfaces;
using RailWeldApp.Services;
using RailWeldApp.Enums;

namespace RailWeldApp.Controls;

[TemplatePart(Name = _pART_Overlay, Type = typeof(FrameworkElement))]
[TemplatePart(Name = _pART_DrawerContainer, Type = typeof(FrameworkElement))]
[TemplatePart(Name = _pART_Transform, Type = typeof(TranslateTransform))]
public class DrawerControl : ContentControl
{
    private const string _pART_Overlay = "PART_Overlay";
    private const string _pART_DrawerContainer = "PART_DrawerContainer";
    private const string _pART_Transform = "PART_Transform";

    private FrameworkElement? _overlay;
    private FrameworkElement? _drawerContainer;
    private TranslateTransform? _drawerTransform;

    public ISoundService? SoundService { get; set; }

    static DrawerControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawerControl), new FrameworkPropertyMetadata(typeof(DrawerControl)));
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(DrawerControl),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnDrawerPropertyChanged));

    public DrawerPosition Position
    {
        get => (DrawerPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(
            nameof(Position),
            typeof(DrawerPosition),
            typeof(DrawerControl),
            new PropertyMetadata(DrawerPosition.Right, OnDrawerPropertyChanged));

    public object? DrawerContent
    {
        get => GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public static readonly DependencyProperty DrawerContentProperty =
        DependencyProperty.Register(
            nameof(DrawerContent),
            typeof(object),
            typeof(DrawerControl),
            new PropertyMetadata(null));

    public double DrawerSize
    {
        get => (double)GetValue(DrawerSizeProperty);
        set => SetValue(DrawerSizeProperty, value);
    }

    public static readonly DependencyProperty DrawerSizeProperty =
        DependencyProperty.Register(
            nameof(DrawerSize),
            typeof(double),
            typeof(DrawerControl),
            new PropertyMetadata(320d, OnDrawerPropertyChanged));

    public Brush DrawerBackground
    {
        get => (Brush)GetValue(DrawerBackgroundProperty);
        set => SetValue(DrawerBackgroundProperty, value);
    }

    public static readonly DependencyProperty DrawerBackgroundProperty =
        DependencyProperty.Register(
            nameof(DrawerBackground),
            typeof(Brush),
            typeof(DrawerControl),
            new PropertyMetadata(Brushes.White));

    private static void OnDrawerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DrawerControl drawer)
            return;

        // 只有打开状态变化才播放动画。
        drawer.UpdateDrawer(e.Property == IsOpenProperty);
    }

    public override void OnApplyTemplate()
    {
        if (_overlay != null)
            _overlay.MouseDown -= Overlay_MouseDown;

        base.OnApplyTemplate();

        _overlay = GetTemplateChild(_pART_Overlay) as FrameworkElement;
        _drawerContainer = GetTemplateChild(_pART_DrawerContainer) as FrameworkElement;
        _drawerTransform = GetTemplateChild(_pART_Transform) as TranslateTransform;

        if (_overlay != null)
            _overlay.MouseDown += Overlay_MouseDown;

        UpdateDrawer(false);
    }

    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        IsOpen = false;
    }

    private void UpdateDrawer(bool animate)
    {
        if (_overlay == null || _drawerContainer == null || _drawerTransform == null)
            return;

        UpdateDrawerSize();

        GetClosedPosition(out var closedX, out var closedY);

        var targetX = IsOpen ? 0 : closedX;
        var targetY = IsOpen ? 0 : closedY;

        // 非打开/关闭操作：清除旧动画并直接定位。
        if (!animate)
        {
            _drawerTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _drawerTransform.BeginAnimation(TranslateTransform.YProperty, null);
            _overlay.BeginAnimation(UIElement.OpacityProperty, null);

            _drawerTransform.X = targetX;
            _drawerTransform.Y = targetY;
            _overlay.Opacity = IsOpen ? 1 : 0;
            _overlay.IsHitTestVisible = IsOpen;
            return;
        }

        // 仅在打开抽屉时播放音效。
        // 优先用外部注入的实例；没注入就从 App 容器里取（视图里不再需要 code-behind 赋值）。
        if (IsOpen)
            (SoundService ?? (Application.Current as App)?.Services.GetService(typeof(ISoundService)) as ISoundService)?.Play("Popup");

        var duration = TimeSpan.FromMilliseconds(200);
        var easing = new CubicEase
        {
            EasingMode = IsOpen ? EasingMode.EaseOut : EasingMode.EaseIn
        };

        // 只动画当前方向需要的坐标。
        if (Position is DrawerPosition.Left or DrawerPosition.Right)
        {
            _drawerTransform.BeginAnimation(
                TranslateTransform.YProperty, null);

            _drawerTransform.BeginAnimation(
                TranslateTransform.XProperty,
                new DoubleAnimation
                {
                    From = _drawerTransform.X,
                    To = targetX,
                    Duration = duration,
                    EasingFunction = easing
                });
        }
        else
        {
            _drawerTransform.BeginAnimation(
                TranslateTransform.XProperty, null);

            _drawerTransform.BeginAnimation(
                TranslateTransform.YProperty,
                new DoubleAnimation
                {
                    From = _drawerTransform.Y,
                    To = targetY,
                    Duration = duration,
                    EasingFunction = easing
                });
        }

        _overlay.BeginAnimation(
            UIElement.OpacityProperty,
            new DoubleAnimation
            {
                From = _overlay.Opacity,
                To = IsOpen ? 1 : 0,
                Duration = duration
            });

        _overlay.IsHitTestVisible = IsOpen;
    }

    private void GetClosedPosition(out double x, out double y)
    {
        x = 0;
        y = 0;

        switch (Position)
        {
            case DrawerPosition.Left:
                x = -DrawerSize;
                break;

            case DrawerPosition.Right:
                x = DrawerSize;
                break;

            case DrawerPosition.Top:
                y = -DrawerSize;
                break;

            case DrawerPosition.Bottom:
                y = DrawerSize;
                break;
        }
    }

    private void UpdateDrawerSize()
    {
        if (_drawerContainer == null)
            return;

        switch (Position)
        {
            case DrawerPosition.Left:
                _drawerContainer.HorizontalAlignment = HorizontalAlignment.Left;
                _drawerContainer.VerticalAlignment = VerticalAlignment.Stretch;
                SetWidth(DrawerSize);
                SetHeight(double.NaN);
                break;

            case DrawerPosition.Right:
                _drawerContainer.HorizontalAlignment = HorizontalAlignment.Right;
                _drawerContainer.VerticalAlignment = VerticalAlignment.Stretch;
                SetWidth(DrawerSize);
                SetHeight(double.NaN);
                break;

            case DrawerPosition.Top:
                _drawerContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
                _drawerContainer.VerticalAlignment = VerticalAlignment.Top;
                SetWidth(double.NaN);
                SetHeight(DrawerSize);
                break;

            case DrawerPosition.Bottom:
                _drawerContainer.HorizontalAlignment = HorizontalAlignment.Stretch;
                _drawerContainer.VerticalAlignment = VerticalAlignment.Bottom;
                SetWidth(double.NaN);
                SetHeight(DrawerSize);
                break;
        }
    }

    private void SetWidth(double value)
    {
        if (_drawerContainer != null && _drawerContainer.Width != value)
            _drawerContainer.Width = value;
    }

    private void SetHeight(double value)
    {
        if (_drawerContainer != null && _drawerContainer.Height != value)
            _drawerContainer.Height = value;
    }
}
