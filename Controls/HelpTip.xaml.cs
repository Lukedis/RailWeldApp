using System.Windows;
using System.Windows.Controls;

namespace RailWeldApp.Controls;

// 问号提示：Text 写提示内容，悬停问号显示统一风格的 ToolTip。
public partial class HelpTip : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(HelpTip),
            new PropertyMetadata(string.Empty));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public HelpTip()
    {
        InitializeComponent();
    }
}
