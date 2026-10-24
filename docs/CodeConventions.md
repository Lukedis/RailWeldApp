# RailWeld 代码规范

> 本文是这个项目的**唯一编码约定来源**。新增代码先看这里，改动约定时同步更新本文。
> 约定不是风格偏好，都是踩过坑或量过代价之后定下来的。

---

## 1. 分层与目录

```
RailWeldApp/
├─ App.xaml(.cs)          应用入口：装配 DI、初始化数据库、显示主窗口
├─ View/                  XAML 视图（只做布局与绑定，不写业务）
├─ ViewModel/             视图模型（状态 + 命令 + 校验）
├─ Model/                 POCO / 缓存 / 选项类（PlcOptions、AuthOptions、PlcDataCache…）
├─ DataModel/             EF Core 实体与 DbContext
├─ Services/              业务与基础设施实现（PLC、Modbus、认证、音效、日志、偏好）
├─ Interface/             服务契约（每个实现都有接口，便于替换与测试）
├─ DependencyInjection/   ServiceCollection 扩展
├─ DependencyPropertys/   附加属性（TextBoxHelper、PassWordBoxHelper、ButtonHelper）
├─ Assets/Colors/         颜色资源字典（色值唯一来源，见第 8 节）
├─ Styles/                控件样式与模板
├─ Themes/                导航按钮、抽屉等复合控件的模板
└─ docs/                  本规范与设计说明
```

**依赖方向**：`View → ViewModel → Interface ← Services`。
View 不认识 Service，ViewModel 不认识 View 类型（只通过 `INavigationService` 发导航请求），
Service 不引用任何 WPF 类型（`SoundService` 是唯一例外，它封装 `System.Media`）。

---

## 2. 命名

| 类别 | 规则 | 示例 |
|---|---|---|
| 类 / 方法 / 属性 | PascalCase | `AuthenticationService`、`SignInAsync` |
| 私有字段 | `_camelCase` | `_authenticationService` |
| 常量 | PascalCase（不用全大写） | `MaxUserNameLength` |
| 接口 | `I` 前缀 | `IPlcService` |
| 异步方法 | `Async` 后缀 | `ReadBytesAsync` |
| 命令 | 生成自方法名，不加动词前缀 | `SignInCommand` |
| XAML 元素 | `x:Name` 用 PascalCase；样式键用 `控件 + 用途` | `RootScale`、`LoginPrimaryButton` |
| 颜色键 | `用途 + Brush`（画刷）/ 无后缀（色值） | `SurfaceCardBrush` / `SurfaceCard` |

- **禁止**中文标识符，注释和日志文案用中文。
- **禁止**拼音命名（`yonghuming`）、无意义缩写（`tmp`、`data2`）。

---

## 3. MVVM

### 3.1 基类

```csharp
ViewModelBase : ObservableValidator   // 全部 VM 的根
PageViewModel : ViewModelBase         // 页面 VM，携带 PageName
```

继承 `ObservableValidator` 意味着任何 VM 都能直接用 DataAnnotations 做表单校验。

### 3.2 属性与校验

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SignInCommand))]   // 影响命令可用性时必加
[NotifyDataErrorInfo]                                 // 需要实时校验反馈时必加
[Required(ErrorMessage = "请输入用户名")]
[MaxLength(50, ErrorMessage = "用户名不能超过 50 个字符")]
private string _userName;
```

- 字段带下划线前缀，由源生成器产出 `UserName` 属性，**不要手写属性**。
- 校验规则写在特性上，命令里只调用 `ValidateAllProperties()`，不要堆 `if (string.IsNullOrWhiteSpace(...))`。
- 派生属性（`HasError` 等）用 `[NotifyPropertyChangedFor]` 联动，不要手动 `OnPropertyChanged`。

### 3.3 命令

```csharp
[RelayCommand(CanExecute = nameof(CanSignIn))]
private async Task SignInAsync(CancellationToken cancellationToken) { … }

private bool CanSignIn() => !IsBusy && …;
```

- 异步命令用 `IAsyncRelayCommand`：它自带"执行中不可重入"，**不要**再写 `if (IsBusy) return;` 之外的锁。
- 只读展示、不改变状态的操作不要写成命令（用属性 + 绑定）。
- 命令里不要弹 `MessageBox`；用 VM 上的提示属性（`ErrorMessage` / `NoticeMessage`）由视图呈现。

### 3.4 与缓存 / 事件的关系

- **页面 VM 是 Transient**，可以被反复创建。任何 `+=` 订阅都必须在页面离开时 `-=`，
  否则单例缓存（`PlcDataCache` 等）会把旧 VM 留在内存里。
- 只想"读"共享数据时，不要订阅，直接把缓存实例暴露成属性，让 XAML 绑定：
  `public PlcDataCache Plc { get; }` → `{Binding Plc.IsConnected}`。
  绑定的失效通知由 WPF 用弱引用管理，不会造成泄漏。

---

## 4. 异步与线程

- **禁止** `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`，除了启动期的一次性初始化（见 `App.InitializeDatabase`，已在代码里注明原因）。
- 所有可能超过几毫秒的 IO 都提供 `CancellationToken` 参数，并透传下去。
- `Services` 层一律 `ConfigureAwait(false)`；`ViewModel` 层**不要**加（需要回到 UI 线程更新属性）。
- `ViewModel` 属性只能在其创建线程（UI 线程）上改；后台线程要更新界面时：
  - 优先改共享缓存（`PlcDataCache` 等 `ObservableObject`），由 WPF 自动封送到 UI 线程；
  - 不要自己 `Dispatcher.Invoke` 到处散落。
- 定时循环用 `while (!ct.IsCancellationRequested) { … await Task.Delay(…) }`，**禁止** `Thread.Sleep` 和 `DispatcherTimer` 做轮询。

---

## 5. 异常、结果类型与日志

**业务失败用返回值，不用异常。**

```csharp
public sealed record AuthenticationResult(bool Succeeded, AuthenticationFailureReason? Reason, string? Message);
```

- 可预期的失败（密码错误、PLC 未连接）→ 返回结果对象，界面给出可读中文提示。
- 不可预期的故障（数据库损坏、反序列化失败）→ 抛异常，在边界处捕获并记日志。
- Service 里**所有**异常都必须记日志，且日志里要有足够的上下文（用户名、DB 号、地址）。
- 日志**不得**记录密码、完整凭据；用户名过长时截断。
- 日志统一用注入的 `ILogger<T>`，不要 `Console.WriteLine`、不要静态 `Log.Logger` 直接调用。
- **日志路径固定为 exe 同级 `Logs\`（绝对路径）**，由 `LoggingExtensions` 写死。
  不要用 `Logs/x.log` 这种相对路径：Serilog 按"进程当前工作目录"解析，换个启动方式就找不到日志。
- 开发期已启用**绑定错误诊断**（`Infrastructure/BindingErrorTraceListener`，仅 Debug 构建挂载）：
  绑定名写错会以 `[绑定]` 前缀记进日志，而不是静默显示空白。

---

## 6. 依赖注入与生命周期

| 生命周期 | 适用 | 本项目示例 |
|---|---|---|
| Singleton | 无状态服务、进程级缓存 | `PlcService`、`AuthenticationService`、`PlcDataCache`、`SoundService`、`MainWindow`、`AdminView` |
| Transient | 页面视图与页面 VM | `LoginView`、`LoginViewModel`、各 `Admin*View/ViewModel` |
| Factory | `DbContext` | `AddDbContextFactory<ApplicationDataContext>` |

- **禁止服务定位器**（`App.Services.GetRequiredService<T>()`）出现在 VM/Service 里，一律构造函数注入。
  唯一允许的位置是 `App` 启动与退出流程。
- 新服务必须同时提供接口，并加 XML 注释说明线程安全性与失败语义。
- 注册顺序按"基础设施 → 业务 → 界面"分组，每组加一行注释。

---

## 7. XAML 约定

- 视图根元素**不要**写死 `Width/Height`：界面随窗口拉伸（侧栏固定宽、内容区用 `*`），窗口大小变化时重新布局而不是整体缩放。
- **视图里不写 `<UserControl.Resources>`**：XAML 只留布局。样式按作用范围放到：
  - `Styles/*.xaml` —— 全局控件样式（TextBox、Button、CheckBox…）；
  - `Styles/Pages/*.xaml` —— 只被某一个页面使用的样式，键名加页面前缀（`Login…`、`Settings…`）；
  - `Themes/*.xaml` —— 复合控件的模板（NavigationButton、DrawerControl）。
- 不要用"隐式样式"（只写 `TargetType`、不写 `x:Key`）去改全局控件外观，它会波及整个应用；
  需要时给它起名，再用 `ItemContainerStyle` / `Style=` 精确引用。
- 颜色只能用 `{StaticResource XxxBrush}`，**禁止**十六进制字面量和 `White`/`Black` 命名色。
- 布局优先 `Grid` + 行列的 `*` 分配；避免三层以上嵌套的 `StackPanel` 做对齐。
- 文本样式（字号/字重/前景色）在同一视图内聚合成 `Style`（放在 `UserControl.Resources`），不要逐控件重复设置。
- 图标统一用 `{StaticResource IconFont}`（Segoe Fluent Icons），不要混用 `Segoe MDL2 Assets`。
- 控件样式命名：`<页面/控件><用途>`，如 `LoginPillButtonStyle`、`LoginHeadlineStyle`。

### 7.0 视觉语言：macOS 27

界面按 macOS 控件规范组织，改样式时对照这张表：

| 项目 | 规则 |
|---|---|
| 主色 | 系统蓝 `#007AFF`（Hover `#3B95FF`、Press `#0062CC`） |
| 文字 | `#1D1D1F` / `#6E6E73` / `#8E8E93` / 占位 `#AEAEB2`（Apple 灰阶） |
| 按钮 | **圆角矩形**（半径 6/8，圆角矩形不是胶囊）：灰底深字、淡蓝底蓝字、淡红底深红字、蓝底白字、红底白字、纯蓝字；无渐变、无阴影 |
| 输入框 | 白底 + 1px 发丝描边 `#D1D1D6` + 圆角 6；聚焦时在**外侧**画 3px 半透明蓝焦点环（不改变边框、不挤压内容） |
| 复选框 | 18px 圆角方块，选中蓝底白勾（矢量 Path） |
| 侧栏 | 行高 32、字号 13、图标 16；选中态只填灰、文字图标变蓝，**不用左侧指示条** |
| 禁用 | 整体 `Opacity=0.4`，不换颜色 |
| 焦点环 | 画在**控件内侧**（`Margin=1.5` + 2px），不要画在外侧：外层会被 `ScrollViewer` / `ClipToBounds` 裁掉 |

### 7.0.1 颜色令牌（对齐 Apple HIG）

| 分组 | 令牌 | 说明 |
|---|---|---|
| 系统强调色 | `SystemRed / Orange / Yellow / Green / Mint / Teal / Cyan / Blue / Indigo / Purple / Pink / Brown / Gray` | Apple 调色板浅色取值，画刷同名 + `Brush` |
| 填充 | `FillPrimary / Secondary / Tertiary / Quaternary` | 黑 + 透明度，用于控件底色与分组 |
| 文字 | `InkPrimary / Strong / Body / Secondary / Tertiary / Quaternary / Placeholder / Disabled` | Apple 文字层级 |
| 材质 | `MaterialUltraThin / Thin / Medium / Thick / UltraThick` | 半透明白层，模拟毛玻璃（**不叠加模糊**） |
| 语义 | `Accent`(#007AFF) / `Danger`(#FF3B30) / `Success`(#34C759) / `Warning`(#FF9500) | 与系统强调色一致 |
| 卡片 | 白底 + 圆角 12/16 + 发丝描边，不用阴影（阴影即 Effect，性能红线） |

### 7.1 尺寸只用令牌

`Styles/Tokens.xaml` 是全站唯一的尺寸来源，页面里**不要**再手写 `8`、`12`、`44` 这类数字：

| 类别 | 令牌 | 值 |
|---|---|---|
| 圆角 | `RadiusXs / Sm / Md / Lg / Xl / 2Xl` | 5 / 6 / 8 / 10 / 12 / 16 |
| 控件高度 | `ControlHeightSm / Md / Lg / Xl / 2Xl` | 28 / 32 / 36 / 40 / 44 |
| 字号 | `FontSizeCaption / Small / Body / BodyLarge / Title / Heading / Display` | 11 / 12 / 13 / 15 / 17 / 22 / 34 |

用法：`CornerRadius="{StaticResource RadiusLg}"`、`Height="{StaticResource ControlHeightXl}"`。

### 7.2 控件形状：改属性，不复制模板

WPF 的 Button / TextBox 没有 `CornerRadius` 属性，所以模板里会写死圆角。
本项目用附加属性 `helper:CornerRadiusHelper.CornerRadius` 把圆角"抬"到控件上，模板统一读它：

```xaml
<!-- 一份模板，两种形状 -->
<Style x:Key="AccentButton" ... />                       <!-- 标准圆角 -->
<Style x:Key="AccentPillButtonStyle"                     <!-- 胶囊：只改尺寸和圆角 -->
       BasedOn="{StaticResource AccentButton}">
    <Setter Property="helper:CornerRadiusHelper.CornerRadius" Value="{StaticResource RadiusPill}" />
</Style>
```

**规矩：需要新形状时，先看能不能 `BasedOn` 已有样式 + 改圆角/高度；不要再复制一份 ControlTemplate。**

当前通用样式清单（页面直接用，不要再自己定义）：

| 控件 | 标准（32 高） | 大号（44 高，登录页） |
|---|---|---|
| 主按钮（蓝底白字） | `AccentButton` | `AccentLargeButtonStyle` |
| 次按钮（灰底深字） | `SecondaryButton` | `SecondaryLargeButtonStyle` |
| 浅蓝按钮（淡蓝底蓝字） | `TintedButtonStyle` | — |
| 破坏性（淡红底深红字） | `DangerButton` | — |
| 不可逆操作（红底白字） | `DangerFilledButton` | — |
| 文字按钮（蓝字） | `GhostButton` / `LinkButtonStyle` | — |
| 输入框 | `TextBoxStyle` | `LargeTextBoxStyle` |
| 密码框 | `PasswordBoxStyle` | `LargePasswordBoxStyle` + `PasswordVisibilityToggleStyle` |
| 搜索框（灰底） | `SearchTextBoxStyle` | — |

> 控件一律用圆角矩形：标准控件 `RadiusSm`(6)、大号控件 `RadiusMd`(8)、卡片 `RadiusXl/2Xl`。圆形元素（头像、密码显隐按钮）用 `HalfConverter` 绑 `ActualWidth`，不要写死半径——半径超过高度一半时 WPF 会画出椭圆。

---

## 8. 颜色与主题

**唯一来源是 `Assets/Colors/Primitives.xaml`（`<Color>`），其余文件只做语义化画刷。**

```
Assets/Colors/
├─ Primitives.xaml      唯一的色值表（#AARRGGBB）
├─ Global/              全局画刷：Surfaces / Text / Accent / Gradients
├─ Controls/            仅某控件使用：NavigationButton / Inputs
└─ Pages/               仅某页面使用：Login / AdminShell / AdminSettings
```

新增颜色的流程：

1. `Primitives.xaml` 加 `<Color x:Key="Foo">#FF123456</Color>`；
2. 对应分组文件加 `<SolidColorBrush x:Key="FooBrush" Color="{StaticResource Foo}" />`；
3. 视图里用 `{StaticResource FooBrush}`。

规则：

- 应用**只有浅色主题**（深色主题与切换服务已移除）。不要再引入 `DynamicResource` 做颜色引用，
  静态解析快约 5 倍，也没有资源失效回调。
- 登录页左侧品牌深色面板是全站唯一的深色区域，色值以 `Brand*` / `OnBrand*` 开头，只允许 `Pages/Login.xaml` 使用。

### 性能红线

- **禁止** `DropShadowEffect` / `BlurEffect` / `BitmapEffect`。实测：登录页曾有的 56px 阴影 + 两个 70/80px
  模糊光斑把整页重绘从 6ms 推到 **160ms**；远程桌面（无 GPU）环境下等于 7fps。
- 需要"深度/光影"时用：径向渐变、描边、底色层级。柔光用 `RadialGradientBrush`（成本可忽略）。
- 需要缓存静态复杂区块时才用 `CacheMode`，且必须确认外层没有 `Viewbox` 缩放（会糊）。

---

## 9. 性能预算

以下为 Release、开发机实测基线（虚拟机，绝对值偏低，比例才是关键）：

| 指标 | 预算 | 当前 |
|---|---|---|
| 端到端启动（进程 → 窗口出现） | < 800 ms | ~550 ms |
| 整页重绘一帧（软件栅格化） | < 10 ms | 5.5 – 6.1 ms |
| 单页视觉元素数 | < 300 | 140 – 204 |
| 单页绑定数 | < 40 | 13 – 24 |
| 页面构建 + 布局 | < 25 ms | 4.7 – 19.3 ms |
| 资源字典加载（20 本） | < 5 ms | ~2.5 ms |
| 数据更新 → UI（单次） | < 1 µs | 0.06 µs |
| 工作集 | < 250 MB | ~205 MB |
| Effect 数量 | **0** | 0 |

改动后若要验证：`dotnet build` 必须 0 警告 0 错误，然后至少做一次真实启动冒烟。

---

## 10. 数据访问

- 一律通过 `IDbContextFactory<ApplicationDataContext>` 创建短生命周期上下文（`await using`）。
- 只读查询加 `AsNoTracking()`；写操作保持跟踪。
- 表结构在 `OnModelCreating` 里集中配置，并写清列名映射注释。
- 启动时 `DatabaseService.ApplyMigrations()` 建库，首次运行由 `IAuthenticationService.EnsureAdministratorAsync` 初始化管理员。
- `ApplicationDataContext` **必须**有接收 `DbContextOptions<T>` 的构造函数，否则运行期抛"没有配置数据库提供程序"。

---

## 11. 认证边界

登录页只负责**界面状态、前端校验与交互流程**（`LoginViewModel`），认证实现由业务方负责。

- 接入点是 `LoginViewModel.VerifyCredentialsAsync(userName, password, ct)`，返回 `true/false`；
  需要区分"账号不存在 / 密码错误 / 锁定"等场景时，把它改成返回结果对象，并在 `SignInAsync` 里据此设置提示文案。
- 前端**不保存密码**、不做哈希、不查库；`RememberUserName` 只是界面状态，持久化由后端决定。
- 认证失败一律展示可读中文提示，日志里**不得**出现密码或完整凭据。

---

## 12. 提交前检查清单

- [ ] `dotnet build` 0 警告 0 错误
- [ ] 真实启动一次：窗口正常、登录页可交互（认证后端接入后需验证通过/失败两条路径）
- [ ] 涉及界面改动：渲染对比截图，确认没有 Effect、没有十六进制色值
- [ ] 涉及热路径（轮询 / 绑定）：确认没有引入新的每帧分配
- [ ] 新增颜色已同时加到 `Primitives.xaml` 与对应分组文件
- [ ] 新增服务已注册 DI，并提供接口 + XML 注释
- [ ] 新增事件订阅有对应的退订
- [ ] 日志里没有新的 `[绑定]` 告警
