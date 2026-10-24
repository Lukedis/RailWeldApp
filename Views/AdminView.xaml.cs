using System.Windows.Controls;

namespace RailWeldApp.Views;

/// <summary>
/// 管理台外壳。逻辑全在 AdminViewModel：
/// 顶部导航绑命令、子页面绑 <c>CurrentPage</c>、弹窗绑 <c>Dialog.CurrentDialog</c>，
/// VM 与 View 的对应关系由 <c>Themes/ViewTemplates.xaml</c> 的 DataTemplate 决定，这里不需要代码。
/// </summary>
public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
    }
}
