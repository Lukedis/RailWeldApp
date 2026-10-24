# 各视图演示数据（截图 + 虚拟数据）

> 目录：`docs/screens/`（11 张，2100×1290，150% DPI）
> 用途：按这些虚拟数据设计后端 —— 每节列出「截图 → 视图文件 → 需要的 VM 成员 → 截图里用的具体值」。
> 页面里所有绑定都带 `FallbackValue`，VM 没接好时不会报错，只是显示 0 / — / 空列表。

| # | 截图 | 视图 | 一句话 |
|---|---|---|---|
| 1 | `01-login.png` | `Views/LoginView.xaml` | 账号密码登录（左侧表单 + 右侧产品图） |
| 2 | `02-dashboard.png` | `Views/AdminDashboardView.xaml` | 产品首页，两个 CTA 跳到实时数据 / 温度监视 |
| 3 | `03-realtime-data.png` | `Views/AdminRealTimeDataView.xaml` | 三轴表 + 12 路传感器 + 8 路状态灯 + 4 路电参数 |
| 4 | `04-manual-control.png` | `Views/AdminManualControlView.xaml` | 三轴反馈 + 8 张手动控制卡 |
| 5 | `05-auto-control.png` | `Views/AdminAutoControlView.xaml` | 传感器 / 伺服 / 工艺三卡 + 配方管理 |
| 6 | `06-temp-monitor.png` | `Views/AdminTempMonitorView.xaml` | 8 路温度/流量电平条 + 大数字 |
| 7 | `07-history-query.png` | `Views/AdminHistoryQueryView.xaml` | 焊缝列表 + 指标统计 + 采样明细 |
| 8 | `08-fault-alarm.png` | `Views/AdminFaultAlarmView.xaml` | 状态条 + 报警记录 + 统计 |
| 9 | `09-report-export.png` | `Views/AdminReportExportView.xaml` | 报告纸张预览（A4 纵向）+ 打印 |
| 10 | `10-settings.png` | `Views/AdminSettingsView.xaml` | Modbus / PLC 两路连接卡片 + 错误记录 |
| 11 | `11-account-panel.png` | `Views/AccountPanelView.xaml` | 账户面板：开工设置 / 注册操作员 |
| 12 | `12-datepicker.png` | `Styles/DatePickerStyle.xaml` | 日期选择器（字段 + 展开的日历） |

---

## 1. LoginView

| 属性 | 类型 | 截图值 |
| --- | --- | --- |
| `UserName` | string | `admin` |
| `Password` | string | `123456` |
| `ShowPassword` | bool | false |
| `RememberUserName` | bool | true |
| `ErrorMessage` | string | 空 |
| `NoticeMessage` | string | 空 |
| `VersionText` | string | 只读显示 |

命令：`SignInCommand`、`SignInWithCodeCommand`、`ForgotPasswordCommand`。登录成功后应把当前用户写入全局（姓名/工号/角色），报告和记录都要用。

---

## 2. AdminDashboardView

纯展示页，只有一个 CTA 命令：`OpenRealTimeDataCommand`、`OpenTempMonitorCommand`。无需数据。

---

## 3. AdminRealTimeDataView

**三轴**（double）：

| 轴 | 实际位置 | 实际速度 | 目标位置 | 目标速度 | 实际电流 |
| --- | --- | --- | --- | --- | --- |
| X | 56.5 | 1100 | 60 | 1000 | 2.4 |
| Y | 13.5 | 600 | 13.5 | 600 | 0.8 |
| Z | 135 | 300 | 135 | 300 | 1.2 |

**12 路传感器**（double）：温度 812.4 / 742.6 / 655.2 / 38.6；流量 13.6 / 12.1 / 11.4 / 0；位移 Y 1.8 / Z 6.4；压力 0.44；备用 0。

**8 路开关量**（bool）：IGBT 电源 ✓、Y 到位 ✓、Z 到位 ✓、顶料缸安全位 ✓、允许加工 ✓、送料完成 ✗、自动运行 ✓、备用 ✗。

**电参数**（double）：电压 380、电流 210、功率 128、能量 3600。量程条写死在 XAML（600 V / 400 A / 200 kW / 20000 kW·s）。

页脚 + 页头：`WeldId = "072T2609203"`、`AppVersion`、`IsCommunicationOk`、`CommunicationDelayMs`、`ClockText`。

---

## 4. AdminManualControlView

三轴反馈同实时数据页（`XAxisActualPosition` … `ZAxisCurrent`）。

设定值：`PowerSetpoint = 55`、`TimeSetpoint = 8`。

16 个命令（每个都是 `ICommand`）：`PowerOnCommand` / `PowerOffCommand`、`AirBlowStartCommand` / `AirBlowStopCommand`、`YAxisLeftCommand` / `YAxisRightCommand`、`ZAxisUpCommand` / `ZAxisDownCommand`、`Jack1UpCommand` / `Jack1DownCommand` … `Jack4UpCommand` / `Jack4DownCommand`。

---

## 5. AdminAutoControlView

**位移传感器**（double）：Y 传感器 12.48、Z 传感器 11.925、Y 焊缝零位 13.5、焊缝高度 1、Y 补偿零位 13.5、Z 补偿零位 135；补偿使能 ✓、找焊缝使能 ✓。

**伺服轴**（double，每轴 8 个）：起始位置/速度、焊缝极限/速度、加热距离/速度、喷风距离/速度

| 轴 | 起始 | 极限 | 加热 | 喷风 |
| --- | --- | --- | --- | --- |
| X | 56.5 / 1100 | 12 / 1000 | 8 / 800 | 6 / 900 |
| Y | 13.5 / 600 | 6 / 500 | 4 / 400 | 3 / 450 |
| Z | 135 / 300 | 120 / 280 | 10 / 200 | 8 / 220 |

**工艺参数**（double）：起始 650 ℃ / 55 % / 2.5；转变 780 / 72 / 1.8；最终 720 / 45 / 3.2。

**冷却方式**：`CoolingModes = ["温度控制","时间控制","不冷却"]`，选中：时间控制 / 温度控制 / 温度控制。

**阈值**（double）：风压 0.42（上限 0.6、下限 0.2）、流量 12.4（20 / 5）、能量 3600（5000 / 1000）。

**配方**：`RecipeName = "标准工艺 A"`，`Recipes = ["标准工艺 A","厚板工艺","试验 3 号"]`，`SelectedRecipe = "标准工艺 A"`，`RecipeParameters` 6 行（Number / Name / Value）。

页头只读：`CurrentWeldNumber = "072T2609203"`、`CurrentWeldMeta = "1 线 / 白班 / 张伟"`。

命令：`NewRecipeCommand`、`SaveRecipeCommand`、`LoadRecipeCommand`、`DeleteRecipeCommand`。

---

## 6. AdminTempMonitorView

8 路（double）：轨顶 812.4、轨脚 742.6、内轨脚 655.2、冷却 38.6、压力 0.44、流量 13.6 / 12.1 / 11.4。量程写死在 XAML：1000 / 1000 / 1000 / 200 ℃、1.0 MPa、20 L/min ×3。

---

## 7. AdminHistoryQueryView

`SelectedDate = 2026-09-20`、`SearchText = ""`、`ResultCount = 26`、`HasResults = true`。

`WeldRecords`（26 条）：`SerialNumber = "072T2609xx"`、`StartedAtText = "08:12"`…、`MetaText = "张伟 · 2026-09-20 08:56 · 加热 62 s / 冷却 58 s · 工号 10086"`、`IsAbnormal`（每 9 条一条超限）。

`Metrics`（8 行）：

| 指标 | 起始 | 峰值 | 末值 | 均值 | 超限 | PeakRatio |
| --- | --- | --- | --- | --- | --- | --- |
| 轨顶温度 | 712.0 | 812.4 | 648.3 | 760.1 | 0 | 0.81 |
| 轨脚温度 | 688.5 | 742.6 | 612.4 | 705.8 | 0 | 0.74 |
| 内轨脚温度 | 602.1 | 655.2 | 540.9 | 618.3 | 2 | 0.66 |
| 冷却温度 | 31.2 | 38.6 | 27.9 | 33.4 | 0 | 0.19 |
| 喷风压力 | 0.38 | 0.46 | 0.31 | 0.41 | 1 | 0.46 |
| 流量 轨顶/轨腰/轨底 | 11.2 / 10.4 / 9.6 | 13.6 / 12.1 / 11.4 | 9.8 / 9.1 / 8.7 | 11.9 / 10.7 / 10.1 | 0 | 0.68 / 0.61 / 0.57 |

`Samples`（70 行）：`Time = "1.0 s"`…，加热段只有 `Temp1~3`，冷却段（>40 行）才有 `Temp4/Pressure/Flow1~3`，其余显示 `—`。

---

## 8. AdminFaultAlarmView

`HasActiveAlarm = true`、`ActiveCount = 2`、`TodayCount = 12`、
`BannerText = "2 条未复位报警 · 最近 14:32 急停按下 · 已持续 3 分 12 秒"`、
`LatestText = "14:32 · 急停按下（F1000）"`、
`LevelOptions = ["全部","故障","警告","提示"]`、`SelectedLevel = "全部"`。

`AlarmRows`（12 条，最新在上）：

| 时间 | 级别 | 代码 | 内容 | 活动 | 持续 |
| --- | --- | --- | --- | --- | --- |
| 14:32:05 | 故障 | F1000 | 急停按下 | ✓ | 3 分 12 秒 |
| 14:31:40 | 警告 | F1003 | 气压不足 | ✓ | 3 分 37 秒 |
| 14:20:11 | 警告 | F1003 | 气压不足 | ✗ | 12 秒 |
| 13:58:02 | 故障 | F1008 | X 轴伺服故障 | ✗ | 48 秒 |
| 13:41:26 | 警告 | F1011 | 冷却水温过高 | ✗ | 2 分 05 秒 |
| 13:12:44 | 提示 | F2001 | 工艺完成 | ✗ | — |

（其余 6 条同格式，见 `08-fault-alarm.png`）

命令：`AcknowledgeAllCommand`、`ResetCommand`。

---

## 9. AdminReportExportView

`SelectedDate = 2026-09-20`、`SearchText = ""`。

`WeldItems`（12 条）：`SerialNumber`、`TimeText`、`ResultText`（合格/超限）、`IsAbnormal`。

`Report`（选中焊缝的报告）：

```
SerialNumber  072T2609205      OperatorName  张伟
JobNumber     10086            ProducedAt    2026-09-20 08:56
EquipmentName RW-01            RecipeName    标准工艺 A
ShiftName     1 线 / 白班       ReportNo      RPT-20260920-0005
IsQualified   false            ResultText    超限 · 不合格
ResultDetail  内轨脚温度峰值 655.2 ℃ 超出上限 650 ℃（2 个采样点），建议复检。
```

`ProcessRows`（3 行）：起始 650 / 55 / 时间控制 / 2.5 s；转变 780 / 72 / 温度控制 / 1.8 ℃；最终 720 / 45 / 温度控制 / 3.2 ℃。

`MetricRows`（8 行）：与历史查询一致，另带 `MinText`（如 `560/650`）、`Judgement`（合格/超限）。

命令：`PrintCommand`、`ExportPdfCommand`。打印源是 XAML 里的 `ReportPaper`（`Views/AdminReportExportView.xaml` 中带 `x:Name` 的 Border）。

---

## 10. AdminSettingsView

这一页的 VM 已有实现，截图为**未连接**状态：

| 属性 | 值 |
| --- | --- |
| `IsPlcConnected` / `IsPlcDisconnected` | false / true |
| `PlcConnectionStatus` | `未连接` |
| `PlcLastError` | null |
| `IsModbusConnected` / `IsModbusDisconnected` | false / true |
| `ModbusConnectionStatus` | `未连接` |
| `ModbusLastError` | null |
| `ConnectionErrors` | 空（失败时 `Insert(0, CommunicationErrorItem(Time, CommunicationType, Message))`） |

命令：`ConnectPlcCommand`、`DisconnectPlcCommand`、`ConnectModbusCommand`、`DisconnectModbusCommand`（当前 `CanExecute` 返回 false，按钮是禁用的）。

---

## 11. AccountPanelView

面板是纯 UI，DataContext 建议单独给一个账户 VM：

| 属性 | 类型 | 截图值 |
| --- | --- | --- |
| `CurrentUserName` | string | `张伟` |
| `CurrentUserMeta` | string | `工号 10086 · 操作员 · 1 线` |
| `CurrentUserAvatar` | ImageSource? | null（显示默认照片） |
| `StartupLines` / `StartupLine` | 集合 / string | `["1 线","2 线","3 线"]` / `1 线` |
| `StartupTeams` / `StartupTeam` | 集合 / string | `["白班","中班","夜班"]` / `白班` |
| `StartupDate` | DateTime? | `2026-09-20` |
| `StartupSerial` | string | `203` |
| `StartupPreviewNumber` | string | `072T26092203` |
| `NewOperatorName` / `NewOperatorJobNumber` / `NewOperatorAccount` / `NewOperatorPassword` | string | 空 |
| `OperatorRoles` / `NewOperatorRole` | 集合 / string | `["操作员","质检员","管理员"]` / `操作员` |
| `RegisterResultText` | string | `账号创建后即可用于登录` |

命令：`ChangeAvatarCommand`、`ConfirmStartupCommand`、`RegisterOperatorCommand`。

---

## 复现方式

演示数据是一次性的临时文件（`ViewModels/_DemoData.temp.cs`），截图完已删除，工程里不留任何假数据。要重新截图：

1. 把对应的 partial 类与属性加回 VM（值参考本文）；
2. `dotnet build`；
3. `powershell -File E:\AppAI\tools\uia_shot.ps1 -Out <png> -Submit -InvokeId navXxx`（登录页去掉 `-Submit`）。
