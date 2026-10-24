# C# 编码规范（性能 · 可读性 · 鲁棒性）

> 适用：.NET 8+ / C# 12+，桌面（WPF/WinUI）、后台服务、控制台程序均可参考。
> 配套文件：`.editorconfig`、`Directory.Build.props`、`.xamlstyler.json`（见第 2 节）。
> 用法：新项目直接拷贝这三份配置 + 本文档，再按本文的清单自检。

---

## 1. 三条总原则

1. **先正确，再快，最后才是省。** 没有测量过的优化一律不做。
2. **可读性优先于聪明。** 一年后看不懂的代码等于不存在。
3. **失败要显式。** 异常、返回值、日志、类型系统，四者至少用一种把错误表达出来。

---

## 2. 项目基线配置

### 2.1 `Directory.Build.props`（放在解决方案根目录）

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>

    <!-- 风格规则参与编译 -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest</AnalysisLevel>

    <!-- 关键：警告即错误，防止规范形同虚设 -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- 只在库项目打开 -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

> 局部需要绕过时用 `#pragma warning disable IDE1006` 或 `dotnet build /p:TreatWarningsAsErrors=false`，
> **不要为了绕过而全局关掉**。

### 2.2 `.editorconfig` 必须包含的项

```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
csharp_new_line_before_open_brace = all
csharp_style_namespace_declarations = file_scoped:warning
csharp_using_directive_placement = outside_namespace:warning
dotnet_sort_system_directives_first = true

# 单表达式成员用 =>，构造函数不用
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_constructors = false:suggestion
csharp_style_expression_bodied_accessors = true:suggestion

# var 只在类型显而易见时使用
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion

csharp_prefer_braces = when_multiline:suggestion
dotnet_style_readonly_field = true:warning
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
```

### 2.3 命名规则（可直接复制）

```ini
dotnet_naming_rule.interfaces_start_with_i.severity = warning
dotnet_naming_rule.interfaces_start_with_i.symbols = interfaces
dotnet_naming_rule.interfaces_start_with_i.style = i_prefix_pascal

dotnet_naming_rule.types_are_pascal_case.severity = warning
dotnet_naming_rule.types_are_pascal_case.symbols = types
dotnet_naming_rule.types_are_pascal_case.style = pascal

dotnet_naming_rule.members_are_pascal_case.severity = warning
dotnet_naming_rule.members_are_pascal_case.symbols = members
dotnet_naming_rule.members_are_pascal_case.style = pascal

dotnet_naming_rule.private_fields_underscore.severity = warning
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel

dotnet_naming_rule.locals_camel_case.severity = warning
dotnet_naming_rule.locals_camel_case.symbols = locals_and_params
dotnet_naming_rule.locals_camel_case.style = camel

dotnet_naming_rule.async_methods_end_with_async.severity = suggestion
dotnet_naming_rule.async_methods_end_with_async.symbols = async_methods
dotnet_naming_rule.async_methods_end_with_async.style = pascal_async_suffix

dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_symbols.types.applicable_kinds = class,struct,enum,delegate
dotnet_naming_symbols.members.applicable_kinds = property,method,event
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_symbols.locals_and_params.applicable_kinds = local,parameter
dotnet_naming_symbols.async_methods.applicable_kinds = method
dotnet_naming_symbols.async_methods.required_modifiers = async

dotnet_naming_style.pascal.capitalization = pascal_case
dotnet_naming_style.camel.capitalization = camel_case
dotnet_naming_style.i_prefix_pascal.capitalization = pascal_case
dotnet_naming_style.i_prefix_pascal.required_prefix = I
dotnet_naming_style.underscore_camel.capitalization = camel_case
dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.pascal_async_suffix.capitalization = pascal_case
dotnet_naming_style.pascal_async_suffix.required_suffix = Async
```

### 2.4 日常命令

```bash
dotnet format                              # 统一格式（提交前执行）
dotnet format --verify-no-changes          # CI 校验，有差异即失败
dotnet build --no-incremental              # 确认分析器全量跑过
```

---

## 3. 命名规范

| 元素 | 规则 | 示例 |
|---|---|---|
| 命名空间 | PascalCase，点分层级，与目录一致 | `RailWeldApp.Services.Plc` |
| 类 / 结构 / 记录 | PascalCase，名词 | `PlcPollingService` |
| 接口 | `I` + PascalCase，形容词或名词 | `IPlcService` |
| 枚举 | 单数 PascalCase；成员 PascalCase | `ConnectionState.Disconnected` |
| 方法 | PascalCase，动词开头 | `ConnectAsync()`、`ParseFrame()` |
| 异步方法 | 以 `Async` 结尾 | `ReadRegistersAsync()` |
| 属性 | PascalCase，名词 | `IsConnected`、`AlarmCount` |
| 事件 | PascalCase，过去式或 `On…` | `ConnectionLost`、`SnapshotUpdated` |
| 私有字段 | `_camelCase` | `_navigationService` |
| 常量 | PascalCase（不用全大写） | `MaxRetryCount` |
| 局部变量 / 参数 | camelCase | `snapshot`、`cancellationToken` |
| 类型参数 | `T`、`TResult` | `Task<TResult>` |
| 布尔成员 | `Is/Has/Can/Should` 前缀 | `HasError`、`CanExecute` |
| XAML 控件名 | camelCase（`x:Name`） | `headerSearchBox` |
| 资源键 | 角色后缀 | `AccentBrush`、`LoginTitleStyle`、`RadiusMd` |

**禁止**：

- 拼音命名（`huChan`）、无意义缩写（`d1`、`tmp2`）
- 类型名与命名空间同名（`RailWeldApp.Plc.Plc`）
- 单字母变量（循环 `i/j`、泛型 `T` 除外）

---

## 4. 可读性规则

### 4.1 文件与方法规模

| 规模 | 建议 |
|---|---|
| 一个文件一个公共类型 | 必须 |
| 单个方法 ≤ 30 行 | 超过就拆，拆不动说明职责过多 |
| 单个类 ≤ 400 行 | 超过按职责拆服务/分部类 |
| 方法参数 ≤ 4 个 | 超过用参数对象（`record`） |
| 嵌套层级 ≤ 3 | 用卫语句拉平 |

### 4.2 卫语句 + 早返回

```csharp
public async Task ConnectAsync(CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(_options);
    if (_options.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(_options));

    if (_client.IsConnected) return;          // 早返回，别包在 if 里

    await _client.OpenAsync(cancellationToken).ConfigureAwait(false);
}
```

### 4.3 表达式体 `=>` 的使用边界

```csharp
public bool IsConnected => _client?.IsConnected == true;               // ✅ 属性
public void Back() => _navigation.NavigateTo<LoginViewModel>();        // ✅ 单表达式方法
public int Count => _items.Count;                                      // ✅ 索引器/访问器同理

public PlcService(IOptions<PlcOptions> options, ILogger<PlcService> logger)
{
    _options = options.Value;                                          // ✅ 构造用块体
    _logger = logger;
}
```

**不用 `=>` 的情况**：构造、需要多语句、需要注释解释、异常路径。

### 4.4 方法命名要"读起来像一句话"

```csharp
// ❌ 主方法读起来仍是实现细节
private async Task DoWorkAsync() { var r = await _c.ReadAsync(); ... }

// ✅ 主方法读起来像伪代码
private async Task PollOnceAsync(CancellationToken cancellationToken)
{
    var raw = await ReadRawAsync(cancellationToken).ConfigureAwait(false);
    var snapshot = Parse(raw);
    _cache.Update(snapshot);
    RaiseSnapshotUpdated(snapshot);
}
```

### 4.5 注释原则

- **默认不写注释**，用命名和结构表达意图
- 只在"为什么这样做"不明显时写一行 `//`：算法来源、协议坑、性能取舍、临时的业务妥协
- **不写"是什么"的注释**（`// 把 i 加 1`）
- 不用 XML 文档注释堆砌私有成员；公共 API 用 `<summary>` 一句话说明
- 代码改了就删过期注释，宁可没有

### 4.6 分层职责（MVVM / 服务化）

| 层 | 只做 | 不做 |
|---|---|---|
| View（XAML） | 布局、样式、绑定 | 业务逻辑、数据访问 |
| ViewModel | 状态、命令、校验、格式化 | 直接 IO、直接访问数据库/串口 |
| Service | IO、协议、并发、重试 | 引用 UI 类型 |
| Model / DTO | 纯数据 | 行为、IO |

---

### 4.7 `static` 使用规则

判据：**这个东西属于"类型本身"还是"某个实例"？**

> 不依赖任何实例数据 + 没有可变状态 + 不访问外部资源 → 可以 `static`。
> 任意一条不满足，就不要 `static`。

**该用 `static`**

| 场景 | 例子 |
|---|---|
| 纯函数工具类 | `Crc16.Compute(span)`、`HalfConverter.Convert(...)` |
| 常量 / 不可变共享值 | `public const int MaxRetryCount = 3;`、`static readonly TimeSpan DefaultTimeout` |
| 依赖属性标识符（WPF 框架要求） | `public static readonly DependencyProperty IsActiveProperty` |
| 扩展方法容器 | `public static class StringExtensions` |
| 昂贵但只读、线程安全的缓存 | `static readonly Regex Pattern`、冻结后的 `Brush` |
| 不捕获外部变量的 lambda | `items.Select(static x => x.Value)`（避免每次分配闭包） |

**不该用 `static`**

| 场景 | 原因 |
|---|---|
| 有连接/生命周期状态的服务 | 全局可变状态，多线程不安全、无法测试隔离 |
| 需要依赖注入的类型 | 静态类拿不到 `ILogger`、`IOptions`，只能自己 new |
| 静态可变字段 / 属性 | 隐式全局状态，并行测试互相污染 |
| **静态事件** | 订阅者被静态根长期引用，典型内存泄漏 |
| 直接操作 UI 的辅助方法 | 线程来源不明确，难排查 |

**危险等级**

| 形式 | 等级 |
|---|---|
| `static class`（纯函数、无状态） | 🟢 推荐 |
| `static readonly` 不可变对象 | 🟡 注意对象自身是否线程安全 |
| `static` 可变字段 / 属性 | 🔴 禁止（改用实例 + DI） |
| `static` 事件 | 🔴 禁止（改用实例事件 + 显式取消订阅） |

```csharp
// ❌ 全局可变状态
public static class AlarmStore
{
    private static readonly List<Alarm> _items = new();
    public static void Add(Alarm alarm) => _items.Add(alarm);
}

// ✅ 实例 + 容器管理生命周期
public sealed class AlarmStore
{
    private readonly ConcurrentQueue<Alarm> _items = new();
    public void Add(Alarm alarm) => _items.Enqueue(alarm);
}
```
### 4.8 `var` 还是显式类型

判据：**把 `var` 换成显式类型后，代码变啰嗦但没有变清楚 → 用 `var`；变清楚 → 用显式。**

**该用 `var`**

```csharp
var options = new PlcOptions();                        // 右边就是类型
var names = new List<string>();                        // 泛型写两遍是噪音
var map = new Dictionary<string, List<PlcSnapshot>>(); // 右边太长，var 更易读
var timer = Stopwatch.StartNew();
var query = from x in items where x.Alarm select x;    // 匿名类型只能 var
```

**该写显式**

```csharp
// 1. 抽象与实现分离，类型本身就是契约
IPlcService plc = new PlcService(...);
IReadOnlyList<PlcSnapshot> history = _cache.History;

// 2. 数值宽度/精度有业务含义
long totalCount = 0;
decimal amount = 0m;

// 3. 右边是方法调用，类型在调用点不可见
bool authenticated = await VerifyCredentialsAsync(userName, password, cancellationToken);
ReadOnlySpan<byte> frame = BuildFrame(snapshot);
Task<PlcSnapshot> pending = _client.ReadAsync(cancellationToken);

// 4. 类型决定行为
var x = 1 / 2;                 // 0，整型除法，静默丢精度
double y = 1.0 / 2;            // 0.5，意图明确
```

**典型边界案例（务必显式）**

```csharp
// ❌ 类型不可见，读者要跳转到方法定义
var authenticated = await VerifyCredentialsAsync(...);

// ✅ 显式写出"闸门"的契约
bool authenticated = await VerifyCredentialsAsync(...);
```

**三条纪律**

1. 同一文件内保持一致，不要一个方法 `var`、下一个写 `List<string>`
2. 公开 API（返回值、参数、字段）永远显式
3. 初始化表达式需要换行才写得下时，直接写显式类型

**配置**

```ini
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
```

级别用 `suggestion` 而非 `warning`："是否明显"是主观判断，用警告去卡会导致到处 `#pragma` 绕过。

WPF / XAML 相关见 [WPF与XAML规范.md](./WPF与XAML规范.md)。
## 5. 性能规则

### 5.1 异步：不要阻塞，不要同步等待

```csharp
// ❌ UI 线程上可能死锁
public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

// ✅ 用异步作用域
await using var scope = provider.CreateAsyncScope();

// ✅ 必须同步释放时，切到线程池再等
public void Dispose() => Task.Run(() => DisposeAsync().AsTask()).GetAwaiter().GetResult();
```

- 库代码统一 `.ConfigureAwait(false)`；UI 代码内需要回到 UI 线程时才省略
- **禁止** `async void`（事件处理器除外，且必须自带 try/catch）
- 禁止 `.Result`、`.Wait()`、`GetAwaiter().GetResult()` 出现在可能运行于同步上下文的位置

### 5.2 分配：热路径零分配

| 手法 | 用途 |
|---|---|
| `readonly record struct` 快照 | 每次采集一条数据，避免堆分配 |
| `ArrayPool<T>.Shared.Rent/Return` | 协议缓冲区、临时大数组 |
| `Span<T>`、`stackalloc` | 解析定长帧 |
| `StringBuilder` / `string.Create` | 循环内拼字符串 |
| `BinaryPrimitives` | 大小端数值解析，零分配 |
| 复用 `List<T>`（`Clear()` 而非 `new`） | 每帧收集数据 |

```csharp
// ❌ 每轮 3 次分配
var data = values.Select(v => (byte)v).ToArray();

// ✅ 一次池租用
var buffer = ArrayPool<byte>.Shared.Rent(values.Count);
try { /* 填充/解析 */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

### 5.3 集合与 LINQ

- 热路径（≥ 10 次/秒）里**不用 LINQ**，用 `for`
- `Any()` 优于 `Count() > 0`；`FirstOrDefault()` 优于 `Where().FirstOrDefault()`
- 大批量更新 UI 集合：整体替换 `ObservableCollection`，别逐条 `Add`
- 需要频繁按 key 查找用 `Dictionary<TKey,TValue>`，别用 `List.Find`

### 5.4 字符串与格式化

```csharp
// ❌ 每次调用都解析格式串、装箱
var s = "值：" + value.ToString();

// ✅ 热路径用插值处理器（.NET 6+ 编译期生成）
var s = $"值：{value}";
```

- 比较用 `StringComparison.Ordinal` / `OrdinalIgnoreCase`
- 解析数字/日期**必须传 `CultureInfo.InvariantCulture`**，否则换系统语言就崩

### 5.5 WPF / UI 线程

| 规则 | 原因 |
|---|---|
| UI 线程上禁止阻塞（`Thread.Sleep`、`.Result`、同步 IO） | 直接卡界面 |
| 所有 `Brush` / `Pen` 创建后 `Freeze()` | 冻结后无变更通知开销，可跨线程共享 |
| 列表开虚拟化（`VirtualizingStackPanel` + `Recycling`） | 千行数据只渲染可见项 |
| **禁止 `Effect`**（模糊/阴影） | 一次 DropShadow 能让帧耗时从 6ms 涨到 160ms |
| 样式/颜色全部走资源字典，禁止控件内联 `#RRGGBB` | 避免重复对象、便于统一换肤 |
| 启动时把 IO/解码丢 `Task.Run` | 缩短首屏时间 |
| 资源字典按页面懒加载 | 减少启动解析量 |

### 5.6 时间与计时

```csharp
// ❌ 每轮取系统时区，慢且受时区影响
var now = DateTime.Now;

// ✅ 计时用单调时钟
var start = Stopwatch.GetTimestamp();
var elapsed = Stopwatch.GetElapsedTime(start);

// ✅ 存时间用带偏移的 UTC
public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
```

---

## 6. 鲁棒性规则

### 6.1 异常：精确捕获，兜底记录后重抛

```csharp
try
{
    await ReadAsync(cancellationToken).ConfigureAwait(false);
}
catch (TimeoutException ex)
{
    _logger.LogWarning(ex, "读取超时，地址 {Address}", address);
}
catch (IOException ex) when (ex.HResult == 0x8007_0005)
{
    _logger.LogWarning("串口已断开，准备重连");
    throw new DeviceDisconnectedException(ex);
}
catch (Exception ex)
{
    _logger.LogError(ex, "未预期异常");   // 兜底：记全上下文
    throw;                                // 必须重抛，不要吞
}
```

**禁止**：空 `catch {}`、`catch (Exception) { }`、只 `catch` 不记录。

### 6.2 超时与取消

```csharp
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
timeout.CancelAfter(TimeSpan.FromSeconds(3));
await _client.SendAsync(frame, timeout.Token).ConfigureAwait(false);
```

- 所有 IO 方法都接受 `CancellationToken`（默认 `default`）
- 长时间循环每轮 `cancellationToken.ThrowIfCancellationRequested()`
- 不要把 `CancellationToken.None` 传进可能永久等待的操作

### 6.3 重试与重连

- 指数退避：1s → 2s → 4s → 8s，上限 30s，**不要固定 100ms 死循环**
- 重试前判断错误是否可重试（参数错误、协议错误不重试）
- 连续失败 N 次后进入"降级状态"并暴露给 UI，不要静默

### 6.4 输入校验

```csharp
ArgumentNullException.ThrowIfNull(options);
ArgumentException.ThrowIfNullOrWhiteSpace(name);
ArgumentOutOfRangeException.ThrowIfNegative(count);
```

- 公开 API 一律校验入口参数；内部方法依赖调用方时用 `Debug.Assert`
- 外部数据（文件、网络、串口）**永远当作不可信**：先校验长度/校验和/范围再解析

### 6.5 资源与生命周期

```csharp
public sealed class PlcService : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        _semaphore.Dispose();
    }
}
```

- `IDisposable` / `IAsyncDisposable` 成对实现，`Dispose` 幂等
- 事件订阅必须配对 `-=`（窗口关闭、控件卸载时）
- `Timer`、`SemaphoreSlim`、`CancellationTokenSource` 都要 `Dispose`
- 有后台任务的类：`StartAsync` / `StopAsync` 成对，`StopAsync` 等待任务结束（带超时）

### 6.6 并发

| 规则 | 说明 |
|---|---|
| 共享状态一律不可变快照 | `readonly record struct Snapshot`，读方直接拿引用 |
| 用 `Interlocked` 做计数器 | 比 `lock` 便宜 |
| `lock` 内不做 IO、不 await | 会造成线程池饥饿 |
| 异步互斥用 `SemaphoreSlim(1,1)` | 不要 `lock` + `async` |
| UI 集合只在 UI 线程改 | 跨线程用 `Dispatcher.InvokeAsync` |

### 6.7 时间与数据一致性

- 存 `DateTimeOffset`（UTC），显示时 `ToLocalTime()`
- 数值比较用容差（浮点），不要 `==`
- 金额/计量用 `decimal`，不要 `double`

---

## 7. 代码评审 Checklist

提交前逐条自检：

**可读性**

- [ ] 命名符合第 3 节表格
- [ ] 方法 ≤ 30 行，嵌套 ≤ 3 层
- [ ] 主流程方法读起来像伪代码（调用顺序清晰）
- [ ] 没有"解释代码在做什么"的废话注释
- [ ] 没有注释掉的死代码

**性能**

- [ ] 热路径（≥10 次/秒）没有 LINQ、没有每轮 `new`、没有字符串拼接
- [ ] UI 线程没有阻塞调用
- [ ] 集合更新没有逐条触发通知
- [ ] WPF 里没有 `Effect`、没有内联颜色/字号

**鲁棒性**

- [ ] 所有 `async` 方法有 `CancellationToken`（IO 类）
- [ ] 没有 `.Result` / `.Wait()` / `async void`
- [ ] 没有空 `catch`
- [ ] 外部输入先校验再解析
- [ ] 实现了 `Dispose`/`DisposeAsync` 的类型都被正确释放
- [ ] 事件订阅有对应的取消订阅
- [ ] 没有 static 可变状态 / static 事件（服务一律实例 + DI）
- [ ] static 只用于纯函数、常量、依赖属性、扩展方法

**工程**

- [ ] `dotnet build` 0 警告 0 错误
- [ ] `dotnet format --verify-no-changes` 通过
- [ ] 新增公共 API 有参数校验

---

## 8. 常见反模式对照表

| ❌ 反模式 | ✅ 替代 |
|---|---|
| `catch (Exception) { }` | 精确捕获 + 记录 + 重抛 |
| `task.Result` / `.Wait()` | `await` |
| `async void` | `async Task` |
| 方法 200 行 | 拆成 5 个意图明确的小方法 |
| 每轮 `new List<T>()` | 复用集合或 `ArrayPool` |
| 热路径 LINQ | `for` 循环 |
| `DateTime.Now` 存库 | `DateTimeOffset.UtcNow` |
| `double` 存金额 | `decimal` |
| 字符串拼 SQL / 命令 | 参数化 |
| 布尔参数 `Do(true, false)` | 枚举或选项对象 |
| 静态可变状态 | 依赖注入 + 不可变快照 |
| `lock` 里 `await` | `SemaphoreSlim` |
| 魔法数字 `if (x > 3)` | 具名常量或配置 |
| 控件内联颜色/字号 | 资源字典 + 令牌 |
| 大方法里混 UI 与 IO | 分层（View / VM / Service） |

---

## 9. 新项目落地步骤

1. 拷贝 `.editorconfig`、`Directory.Build.props`、`.xamlstyler.json`
2. 打开 `Nullable`、`TreatWarningsAsErrors`
3. 建目录：`View / ViewModel / Services / Models / Interface / Styles / Assets`
4. 先写接口 + 测试，再写实现
5. 每个 PR 跑：`dotnet format --verify-no-changes` + `dotnet build --no-incremental`
6. 定期回看本文件的 Checklist，发现反复出现的问题就补进 `.editorconfig`
