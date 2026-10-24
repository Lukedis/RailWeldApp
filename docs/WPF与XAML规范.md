# WPF / XAML 专项规范

> 配套：[C# 编码规范](./CSharp编码规范.md)、`.editorconfig`、`.xamlstyler.json`
> 适用：WPF（.NET 8+），WinUI 3 可参考大部分条目。

---

## 1. 目录与文件组织

```
项目
├─ View/                窗口与页面（一个页面一个 .xaml + .xaml.cs）
├─ ViewModel/           每个 View 对应一个 ViewModel
├─ Controls/            自定义控件（继承 Control / ContentControl）
├─ DependencyPropertys/ 附加属性与帮助类
├─ Interface/           服务契约
├─ Services/            实现（IO、协议、后台任务）
├─ DataModel/           实体与 DbContext
├─ Styles/              样式：Tokens、控件样式、页面样式
├─ Assets/
│  ├─ Colors/           颜色：Primitives + Global + Controls + Pages
│  ├─ Converter/        值转换器
│  ├─ Fonts/            字体
│  └─ Images/           图片
└─ docs/                规范与说明
```

| 规则 | 说明 |
|---|---|
| 一个 View 一个 VM | 命名必须对应：`LoginView` ↔ `LoginViewModel` |
| 页面专属样式放 `Styles/Pages/XxxViewStyles.xaml` | 不允许塞进 `Window.Resources` 或控件内联 |
| 全局令牌放 `Styles/Tokens.xaml` | 圆角、间距、字号、控件高度 |
| 颜色只放 `Assets/Colors/**` | `.xaml` 里禁止出现 `#RRGGBB` |

---

## 2. 命名

| 元素 | 规则 | 示例 |
|---|---|---|
| 控件 `x:Name` | camelCase | `headerSearchBox`、`adminContentHost` |
| 样式键 | 语义 + `Style` | `AccentButton`、`LoginHeadlineStyle` |
| 颜色键 | 用途 + `Brush`/`Color` | `SurfaceCardBrush`、`InkPrimary` |
| 令牌键 | 类别 + 尺寸缩写 | `RadiusMd`、`ControlHeightSm`、`FontSizeBody` |
| 转换器 | 功能 + `Converter` | `HalfConverter`、`BooleanToVisibilityConverter` |
| 附加属性类 | 用途 + `Helper` | `CornerRadiusHelper`、`TextBoxHelper` |

**禁止**：`Style1`、`MyStyle`、`Button2`、拼音命名、`RedBrush`（颜色应表达用途，不是色相）。

---

## 3. 样式与资源

### 3.1 三层资源结构

```
Primitives.xaml   → 只放 <Color>（唯一允许写十六进制的地方）
Global/*.xaml     → 语义画笔（Surface / Text / Accent）
Controls, Pages   → 具体控件与页面画笔
Styles/*.xaml     → 样式（引用上面两层）
```

### 3.2 引用方式

```xaml
<!-- ✅ 统一 StaticResource（性能好，无运行时查找） -->
<Border Background="{StaticResource SurfaceCardBrush}" CornerRadius="{StaticResource RadiusMd}" />

<!-- ❌ 禁止 DynamicResource：每次取值都要走资源查找 -->
<Border Background="{DynamicResource SurfaceCardBrush}" />
```

### 3.3 样式必须可复用

```xaml
<!-- ✅ 一份模板 + 附加属性控制圆角，避免复制模板 -->
<Style x:Key="AccentButton" TargetType="Button">
    <Setter Property="helper:CornerRadiusHelper.CornerRadius" Value="{StaticResource RadiusMd}" />
</Style>

<!-- ❌ 为不同尺寸复制整份 ControlTemplate -->
```

### 3.4 禁止魔法数字

```xaml
<!-- ❌ -->
<TextBlock FontSize="14" Margin="0,12,0,0" Foreground="#6E6E73" />
<Border CornerRadius="8" Height="40" />

<!-- ✅ -->
<TextBlock FontSize="{StaticResource FontSizeBody}"
           Margin="0,12,0,0"
           Foreground="{StaticResource TextSecondaryBrush}" />
<Border CornerRadius="{StaticResource RadiusMd}" Height="{StaticResource ControlHeightXl}" />
```

---

## 4. 布局

| 规则 | 说明 |
|---|---|
| 根容器不写死 `Width`/`Height` | 让窗口决定尺寸，内部用 `*` 分配 |
| 用 `Grid` 做定位，少用 `Canvas` | `Canvas` 只在画图表/示意图时用，且需给固定尺寸 |
| 用 `StackPanel` 只做一维排列 | 需要对齐/拉伸时用 `Grid` |
| 响应式靠 Grid 比例 | `ColumnDefinitions="1.08*,1*"` 而不是固定像素 |
| 需要滚动 ≠ 显示滚动条 | `VerticalScrollBarVisibility="Hidden"` 仍可滚轮滚动 |
| 窗口外部圆角交给 DWM | `WindowChrome` 无法画窗口外框，用 `DwmSetWindowAttribute` |

**禁止**：用 `Viewbox` 整体缩放页面（字体会糊、布局会失真）。

---

## 5. 绑定

```xaml
<!-- ✅ 编译期检查（.NET 8+ WPF 支持） -->
<TextBlock Text="{Binding UserName, UpdateSourceTrigger=PropertyChanged}" />

<!-- ✅ 明确模式与更新时机 -->
<TextBox Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- ✅ 只读展示用 OneWay / OneTime -->
<TextBlock Text="{Binding VersionText, Mode=OneTime}" />
```

| 规则 | 说明 |
|---|---|
| VM 属性用 `[ObservableProperty]` 生成 | 不手写 `INotifyPropertyChanged` 样板 |
| 命令用 `[RelayCommand]` | 不手写 `ICommand` 实现 |
| 集合用 `ObservableCollection<T>` | 大批量更新时整体替换，不逐条 `Add` |
| 绑定失败必须能看见 | Debug 下挂 `BindingErrorTraceListener` 输出到日志 |
| 不在 XAML 里做业务判断 | 转换器只做展示格式转换，不做业务规则 |

---

## 6. 自定义控件

```csharp
public sealed class NavigationButton : Button
{
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(NavigationButton),
            new PropertyMetadata(false));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}
```

| 规则 | 说明 |
|---|---|
| 依赖属性必须 `static readonly` | 类型级标识符 |
| 属性包装器用 `GetValue`/`SetValue` | 不要在包装器里加逻辑 |
| 模板在 `Themes/Generic.xaml` 或独立字典 | 不写在控件代码里 |
| 属性变化用 `PropertyChangedCallback` | 不在 setter 里做副作用 |

---

## 7. 性能红线

| 禁止 | 原因 | 替代 |
|---|---|---|
| **`Effect`（DropShadow / Blur）** | 一帧从 6ms 变 160ms | 用 1px 发丝线或背景色分层 |
| `DynamicResource` 大量使用 | 每次求值都查字典 | `StaticResource` |
| 控件内联颜色/字号 | 每个控件一份新对象 | 资源字典 + 令牌 |
| 未冻结的 `Brush`（代码创建） | 每次使用都触发变更通知 | 创建后 `Freeze()` |
| 列表不虚拟化 | 千行数据全渲染 | `VirtualizingStackPanel` + `Recycling` |
| UI 线程同步 IO | 界面卡死 | `async` + `await` |
| 高频 `PropertyChanged` | 每次触发绑定更新 | 合并更新或整体替换 |

**验证方式**：

```csharp
// 输入渲染耗时（Debug 下临时加）
RenderOptions.ProcessRenderMode  // 确认是 Default 而不是 SoftwareOnly
```

---

## 8. 主题与颜色

| 规则 | 说明 |
|---|---|
| 只做**一套**主题 | 除非明确需要深色，否则不要引入主题切换服务 |
| 颜色全部走语义令牌 | `SurfaceCanvas`、`TextPrimary`、`Accent`、`Danger` |
| 圆角统一走令牌 | 例如全部 8；需要直角的地方显式写 `CornerRadius="0"` 并注释原因 |
| 层级用明度差，不用边框 | 白卡片 → 浅灰画布；需要边界时才加 1px 发丝线 |

```xaml
<!-- ✅ 靠背景分层 -->
<Border Background="{StaticResource SurfaceCardBrush}" CornerRadius="{StaticResource RadiusMd}" />
```

---

## 9. 评审 Checklist

- [ ] `.xaml` 里没有 `#RRGGBB`
- [ ] 没有 `DynamicResource`
- [ ] 没有 `Effect`
- [ ] 没有 `Window.Resources` / `UserControl.Resources` 内联样式
- [ ] 根容器没有写死 `Width`/`Height`
- [ ] 控件 `x:Name` 是 camelCase
- [ ] 样式键语义化（不是 `Style1`）
- [ ] 列表开了虚拟化
- [ ] 绑定模式明确（`TwoWay`/`OneWay`/`OneTime`）
- [ ] 事件订阅有对应的取消订阅
- [ ] `dotnet build` 0 警告（含 XAML 编译警告）
