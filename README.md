# RailWeldApp · 钢轨焊接接头热处理监控系统

基于 **.NET 10 + WPF** 的工业上位机（HMI），用于钢轨焊接接头的感应加热热处理产线：
实时采集西门子 PLC 与热处理电源数据，完成工艺曲线监控、手动/自动控制、故障报警、历史追溯与质量报告导出。

> 感应加热 · 三段温控 · 自动找缝 · 全程追溯

---

## 功能

| 模块 | 说明 |
| --- | --- |
| 作业登记 / 登录 | 账号 + 密码登录，管理员 / 操作员两种角色，密码 PBKDF2-SHA256 加盐存储 |
| 实时数据 | 电压、电流、频率、功率、能量；X/Y/Z 三轴位置·速度·电流；三路温度、三路流量、风压、焊缝长度、执行机构状态 |
| 工艺曲线 | 按作业采样的曲线回放，加热 / 均温 / 冷却阶段判定与工艺参数上下限比对 |
| 手动控制 | 上电 / 断电、吹风启停、Y/Z 轴点动、四个顶升动作、功率与时间设定值下发 |
| 自动控制 | 启停、配方下发、流程状态机与执行结果回显 |
| 温度监控 | 三通道温度曲线、设定值对比、超限报警 |
| 历史查询 | 按日期 / 关键字检索作业记录，采样点按需懒加载 |
| 故障报警 | 实时报警列表、等级与确认操作、通信异常追溯 |
| 报表导出 | QuestPDF 生成 A4 质量报告（工艺参数、阶段数据、判定结论） |
| 系统设置 | PLC / 串口参数、账户管理、密码修改 |

## 技术栈

| 层 | 技术 |
| --- | --- |
| 表现层 | WPF、MVVM（CommunityToolkit.Mvvm）、XAML 样式与设计令牌、Drawer/自定义控件 |
| 应用层 | Microsoft.Extensions.DependencyInjection、Options、Logging（Serilog 文件日志） |
| 通信 | S7.Net（S7-300 / S7-1200，DB 读写 + 心跳）、NModbus + System.IO.Ports（Modbus RTU） |
| 数据 | EF Core + SQLite（Code First 迁移、9 张业务表） |
| 报表 | QuestPDF（A4 PDF、中文字体注册） |
| 测试 | MSTest |

## 架构

```
RailWeldApp.slnx
├─ src/RailWeldApp             WPF 界面层：Views / ViewModels / Styles / 组合根（App.xaml.cs）
├─ src/RailWeldApp.Core        领域层：接口、模型、枚举、Options（无第三方 UI 依赖）
├─ src/RailWeldApp.Infrastructure  基础设施层：S7 / Modbus RTU 通信、EF Core + SQLite、后台采集
└─ src/RailWeldApp.Tests       单元测试
```

数据流：

```
PLC(DB) ──S7.Net──┐
                  ├─► PlcPollingBackgroundService (200ms) ─► PlcDataCache(快照)
电源(Modbus RTU) ─┘                                              │
                                                                 ▼
                                        ViewModel（订阅快照） ──► 实时曲线 / 报警 / 记录落库
                                                                 │
                                                                 ▼
                                                          EF Core + SQLite ─► QuestPDF 报告
```

界面层只依赖 `Core` 中的接口（`IPlcService`、`IModbusRtuService`、`IDatabaseService` 等），
通信协议与持久化实现可替换。

## 快速开始

环境要求：**Windows + .NET 10 SDK**（`global.json` 已固定 10.0.3xx/10.0.4xx 特性带）

```powershell
dotnet build .\RailWeldApp.slnx
dotnet run --project .\src\RailWeldApp
dotnet test .\RailWeldApp.slnx
```

首次启动会自动创建 SQLite 数据库并应用迁移，随后创建引导管理员账户：

```
账号：admin
口令：ChangeMe!2026   （见 src/RailWeldApp/appsettings.json，登录后请立即修改）
```

数据库与日志位置：

- 数据库：`%LocalAppData%\RailWeldApp\RailWeld.db`
- 日志：输出目录下 `Logs/railweld-*.log`（按天滚动，保留 30 天）

没有现场设备时，PLC 默认指向 `127.0.0.1`，可配合 `RailWeldSimulator` 模拟数据块读写；
Modbus RTU 需要虚拟串口对（如 com0com）或真实串口。

## 配置

配置文件：`src/RailWeldApp/appsettings.json`（支持注释）

| 节点 | 用途 |
| --- | --- |
| `PLC` | PLC IP / 端口 / CPU 型号 / Rack / Slot、读写超时、心跳间隔与失败阈值 |
| `RealtimeData` | 实时点位地址表（DB1 中电压、电流、三轴、温度、流量、报警字等） |
| `ManualControlAddress` | 手动控制点位（上电、吹风、点动、顶升、设定值） |
| `ProcessParametersAddress` | 工艺参数点位（找缝、补偿、三段温度/功率、喷风模式与上下限） |
| `ModbusRTU` | 串口号、波特率、校验位、从站地址、校验寄存器 |
| `Database` | SQLite 文件路径 |
| `Serilog` | 日志级别、输出模板、滚动策略 |
| `BootstrapAdmin` | 首次启动的引导管理员（生产环境请关闭或改口令） |

> 仓库中的 PLC 地址与串口参数为示例值，部署前需按现场 PLC 组态替换。

## 工程要点

- **采集生命周期**：200ms 轮询、3s 自动重连、同一错误 60s 内只记一条日志；启停通过 `SemaphoreSlim` + `CancellationTokenSource` 串行化。
- **心跳**：建链后先读一次心跳地址，确认 Rack/Slot/DB 真实可用；连续 3 次心跳失败判定离线。
- **并发安全**：串口句柄与 PLC 连接对象统一串行访问；UI 线程不直接触碰通信资源。
- **数据库升级**：识别旧版 `EnsureCreated` 数据库（无迁移历史），自动备份 → 重建 → 回填工艺配方，避免现场升级丢数据。
- **异常兜底**：`DispatcherUnhandledException` / `AppDomain.UnhandledException` / `TaskScheduler.UnobservedTaskException` 三层捕获，界面异常不中断采集。
- **历史查询**：用 generation 令牌丢弃过期异步结果，避免快速切换日期时旧数据覆盖新数据。

## 已知限制

- 部分页面仍在接入真实点位，存在占位 ViewModel；`App.xaml.cs` 中有对应说明。
- 尚未接入 CI，单元测试覆盖密码服务与工艺参数服务。
- 报表与界面预览为两套排版，修改模板时需同步。
- 仓库不含现场截图与客户信息。

## 说明

本项目用于技术展示，界面文案为中文。仓库内不含客户名称、产线编号与现场照片，
配置中的地址与串口参数均为示例值。
