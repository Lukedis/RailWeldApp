using System.Windows;
using System.Windows.Controls;

namespace RailWeldApp.Controls;

/// <summary>
/// 执行机构状态磁贴：名称 + 开关状态，勾/横线图标 + 浅色底。
/// 用法：
/// <code>
/// &lt;control:ActuatorStatusTile Label="IGBT 电源"
///                               OnText="已通电"
///                               OffText="未通电"
///                               IsOn="{Binding IgbtPowerOn, FallbackValue=False}" /&gt;
/// </code>
/// </summary>
public partial class ActuatorStatusTile : UserControl
{
    public ActuatorStatusTile()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(ActuatorStatusTile),
            new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty IsOnProperty =
        DependencyProperty.Register(
            nameof(IsOn),
            typeof(bool),
            typeof(ActuatorStatusTile),
            new PropertyMetadata(false));

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public static readonly DependencyProperty OnTextProperty =
        DependencyProperty.Register(
            nameof(OnText),
            typeof(string),
            typeof(ActuatorStatusTile),
            new PropertyMetadata("运行"));

    public string OnText
    {
        get => (string)GetValue(OnTextProperty);
        set => SetValue(OnTextProperty, value);
    }

    public static readonly DependencyProperty OffTextProperty =
        DependencyProperty.Register(
            nameof(OffText),
            typeof(string),
            typeof(ActuatorStatusTile),
            new PropertyMetadata("停止"));

    public string OffText
    {
        get => (string)GetValue(OffTextProperty);
        set => SetValue(OffTextProperty, value);
    }
}
