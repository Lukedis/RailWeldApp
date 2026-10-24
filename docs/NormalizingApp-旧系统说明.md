# NormalizingApp 旧系统说明（重写参考）

> 本文档基于对 `E:\WPF\NormalizingApp-master\NormalizingApp-master\NormalizingApp.sln` 源码的逐文件阅读整理，
> 用于在 `RailWeldApp`（.NET 10 + MVVM Toolkit）中重写时对照：**每个视图做什么、数据从哪来、库怎么写、参数地址怎么对**。
> 凡标注「需现场确认」的，是代码里没有写死、只能靠 PLC 程序或现场调试确认的部分。

---

## 1. 项目定位与技术栈

**中频感应加热正火热处理设备的上位机监控软件**：连接 S7-1200 PLC，采集加热/冷却过程中的电参数、三轴位置、温度、流量、压力，按焊缝编号归档到本地 Access 数据库，并提供工艺参数配方管理。

| 项    | 内容                                                                                   |
| ---- | ------------------------------------------------------------------------------------ |
| 框架   | WPF / .NET Framework 4.8（非 Core）                         |
| 通信   | HslCommunication `SiemensS7Net`（S7 协议，TCP 102）                                       |
| 数据库  | Access（`.accdb`），OleDb + ADOX + DAO 互操作                                              |
| 界面库  | MahApps.Metro、MaterialDesignThemes、Dragablz、CircularGauge（仪表）、DynamicDataDisplay（曲线） |
| MVVM | 自实现：`MVVM/NotifyObject.cs`（INPC 基类）、`MyCommand`（ICommand）                            |
| 序列化  | `BinaryFormatter`（配方、采集缓冲） + Newtonsoft.Json（主题配色）                                   |
| 解决方案 | `NormalizingApp`（主程序）、`NormalizingApp.Host`（PLC 模拟器，调试用）                             |

---

## 2. 启动与导航

```
LoginWindow（焊缝编号/操作员设置）
   └─ 「确定」→ new MainWindow().Show() → 关闭登录窗
MainWindow（Shell）
   ├─ 左侧汉堡菜单 + Snackbar 消息条
   ├─ 8 个页面以 UserControl 实例形式放进 MainWindowItem[]
   └─ 右上角菜单：重启计算机 / 关闭计算机 / 退出系统（调用 shutdown.exe）
```

- 登录窗**没有密码框**，只做「操作者 / 生产线 / 班组 / 日期 / 接头号」选择，用于生成焊缝编号；
- 页面切换是 `ItemsControl` 绑定 `MainWindowItem(名称, UserControl)`，**页面实例在 MainWindowViewModel 构造时一次性 new 出来**（非按需创建）；
- 主窗口初始化顺序（`MainWindowViewModel.InitData`）：
  1. 计算当日数据库路径并调用 `DBHelp.CreteFilesName()`（目录不存在就建、库不存在就从模板复制）；
  2. `S71KConnect.ConnectPLC()` 连接 PLC，成功则 `StartPLCRead()` 启动后台读取线程；
  3. 启动 UI 刷新线程（500ms 读通讯状态/延时/系统时间/焊缝编号）。
- 关闭窗口时把 MaterialDesign 调色板写入 `Data/Palette/Palette.txt`。

---

## 3. 视图一览

| #   | 视图                                | 菜单名  | 作用                                | 数据来源            | 是否写 PLC / 库  |
| --- | --------------------------------- | ---- | --------------------------------- | --------------- | ------------ |
| 1   | `LoginWindow`                     | —    | 焊缝编号与操作员设置                        | INI + 内存        | 否            |
| 2   | `MainWindow`                      | —    | 壳窗口、导航、通信状态、电源操作                  | 全局 `userItem`   | 否            |
| 3   | `HomePage`                        | 机床信息 | 实时总览：4 个仪表 + 三轴 + 温度/流量/长度 + 限位指示 | DB2             | 否            |
| 4   | `ManualPage`                      | 手动控制 | 手动电源/喷风/气缸点动、手动功率设定               | DB7 写入 + DB2 显示 | **写 DB7**    |
| 5   | `AutoPage`                        | 自动控制 | 工艺参数设定 + 配方新建/保存/装载/删除            | DB6 写入 + 配方文件   | **写 DB6**    |
| 6   | `CurvePage`                       | 实时曲线 | 8 路实时曲线（温度/压力/流量）+ **采集缓冲与入库**    | DB2             | **写 Access** |
| 7   | `DataQueryPage`                   | 历史查询 | 按日期/焊缝编号查历史记录，表格 + 回放曲线           | Access          | 读库           |
| 8   | `AlarmSystemPage`                 | 故障报警 | 报警字解析成中文报警列表、复位                   | DB2 报警字         | 写 DB5.200.0  |
| 9   | `SystemSet`                       | 系统设置 | **空页面**（只有一个空 GroupBox 占位）        | —               | 否            |
| 10  | `UserPaletteSelector` / `Palette` | 主题设置 | MaterialDesign 主题配色选择、持久化         | Palette.txt     | 否            |
| —   | `TextInputBox`                    | —    | 通用文本输入弹窗                          | —               | —            |
| —   | `CMessageBox`                     | —    | 自定义模态消息框                          | —               | —            |
| —   | `IORegionControl`                 | —    | 成对输入/输出小控件（上可编辑、下只读），自动页/手动页大量使用  | —               | —            |
| —   | `SampleProgressDialog`            | —    | 采样进度对话框（代码中未见引用，疑似遗留）             | —               | —            |

---

## 4. 各视图细化

### 4.1 LoginWindow —— 焊缝编号设置

- 控件：操作者 ComboBox、生产线 ComboBox（1/2/3）、班组 ComboBox、日期 DatePicker、接头号 TextBox；

- 编号规则（`LoginWindowViewModel.GetProductNumber`）：
  
  ```
  完整焊缝编号 = 地名(AreaName) + 线名(LineName) + 班组(GroupName) + 日期(yyMMdd) + 接头号(SpliceNumber)
  ```
  
  - 地名/班组来自 `Data/Name/name.txt`（需现场确认内容格式）；
  - 日期同时决定**数据库日期**（`ProductNumber.AccessDate`，即归档到哪一天的分库）；
  - 每次选择都写回 `Data/CFG/LoginCFG.ini`（`[设置] 姓名/接头号` 等），下次启动沿用；

- 工序完成后自动调用 `PartNumberAuto()`：接头号 +1 并写回 INI。

### 4.2 HomePage —— 实时总览

- 4 个 `CircularGauge`：电压 V / 电流 A / 功率 KW / 能量 KWS（单位取界面原文字样）；
- 三轴表格：X/Y/Z 的**实际位置、实际速度、设定位置、设定速度、实际电流**（各 5 列）；
- 温度 4 路、流量 3 路（第 4 路的 `Flow4` 属性有绑定但 VM 从未赋值，恒为 0）、长度 2 路、压力 1 路；
- 一组状态灯：电源状态、Y/Z 气缸前后限位、1#~4# 顶料缸上下限位；
- 刷新方式：`HomePageViewModel` 起一个 100ms 后台线程（`Thread.Sleep(100)`）调用 `UpData()`，从 `S71KConnect.userItem` 拷到绑定属性（`StringFormat=F2/F3`）。

### 4.3 ManualPage —— 手动控制

8 组按钮，**每组都是"点动"**：写 `true` 后立刻写 `false`，由 PLC 侧自锁/动作：

| 按钮组            | PLC 地址                  |
| -------------- | ----------------------- |
| 电源启动 / 停止      | `DB7.66.0` / `DB7.66.1` |
| 喷风启动 / 停止      | `DB7.72.0` / `DB7.72.1` |
| Y 跟踪气缸 启动 / 停止 | `DB7.74.0` / `DB7.74.1` |
| Z 跟踪气缸 启动 / 停止 | `DB7.74.2` / `DB7.74.3` |
| 1# 顶料缸 启动 / 停止 | `DB7.76.0` / `DB7.76.1` |
| 2# 顶料缸 启动 / 停止 | `DB7.76.2` / `DB7.76.3` |
| 3# 顶料缸 启动 / 停止 | `DB7.76.4` / `DB7.76.5` |
| 4# 顶料缸 启动 / 停止 | `DB7.76.6` / `DB7.76.7` |
| 手动功率设定         | `DB7.80`（float）         |
| 手动功率时间设定       | `DB7.68`（float）         |

每次写入前都判断 `userItem.ConnectionState`，未连接直接返回。

### 4.4 AutoPage —— 自动控制与配方（核心）

页面分三块，和你在新版做的三卡布局一致：

1. **位移传感器参数**：Y/Z 传感器值（只读）、Y 轴焊缝零位、焊缝高度、Y/Z 补偿零位、补偿使能、找焊缝使能；
2. **伺服轴运动位置**：X/Y/Z 各 4 组（起始位置/焊缝极限/加热距离/喷风距离）× 位置 + 速度；
3. **工艺参数**：起始/转变/最终三段（温度、功率、冷却参数）+ 冷却方式（温度/时间）+ 风压/流量/能量上下限。

每个输入框的属性 setter 里**直接写 PLC**（值变化即下发），写入前同样判断连接状态。

### 4.5 CurvePage —— 实时曲线 + 采集入库（最关键的一页）

- 4 个 `DynamicDataDisplay` 绘图区、8 条曲线：
  
  | 曲线                   | 数据源          | 图例颜色      |
  | -------------------- | ------------ | --------- |
  | 轨顶温度 / 轨脚边温度 / 内轨脚温度 | DB2 温度 1/2/3 | 红 / 绿 / 蓝 |
  | 冷却温度                 | DB2 温度 4     | 红         |
  | 喷风压力                 | DB2 压力       | 红         |
  | 轨顶 / 轨腰 / 轨底流量       | DB2 流量 1/2/3 | 绿 / 蓝 / 黄 |

- 采集是**事件驱动**的：
  
  - DB2 `112` 加热运行 = true → 清空温度曲线、记 `HeatStartTime`、`heatEnabed = true`；
  - DB2 `113` 冷却运行 = true → 清空冷却曲线、取一次焊缝编号、`coolingEnabed = true`；
  - DB2 `140` 工艺完成 = true → 触发 `CheckFileBuff()` 入库 + 焊缝编号自增；

- 一个 **200ms 后台线程**（`UpdataCurve`）在有使能时，把当前值作为 `Point(秒, 值)` 追加到曲线，同时**每 200ms 生成一条 `DataBaseRecord` 并整体序列化**到 `Data/Temp/dat.xml`（`BinaryFormatter`）；
  
  - 加热阶段写：`heatTime`、电压/电流/功率/频率/能量、温度 1~3；
  - 冷却阶段写：`coolingTime`、压力、温度 4、流量 1~3，并按焊缝编号回填到加热阶段那条记录（同一行）；

- `CheckFileBuff()` 入库流程：
  
  ```
  读 Data/Temp/dat.xml → 取第一条的 serialNumber
    → 查 DataSheet 是否已有该编号
    → 有则 DELETE，无则跳过
    → BulckInsert（DAO 批量写入 DataSheet）
    → 删除缓冲文件、清空列表
  ```
  
  这样同一焊缝编号的加热/冷却数据合成一条完整记录，重复采集会被新数据覆盖。

### 4.6 DataQueryPage —— 历史查询

- 无 ViewModel，全部逻辑在 `DataQueryPage.xaml.cs`；
- 日期选择 → 拼出 `Data/WorkData/{yyyy}/{yyyyMM}/{yyyyMMdd}.accdb` 路径 → 赋给 `DBHelp.FileName`；
- 左侧列表：该库里去重后的焊缝编号（并统计重复行数）；
- 选中编号 → `DataGrid` 显示 18 列（编号、操作员、工号、加热时间、电压、电流、功率、频率、能量、三路温度、冷却时间、温度 4、压力、三路流量）；
- 右侧按钮：电压/电流/功率/能量/加热温度/冷却温度/流量/压力 回放曲线（用库里逐行数据画，X 轴为行号）。

### 4.7 AlarmSystemPage —— 故障报警

- 200ms 线程读 DB2 的报警字 1（`114`）和报警字 2（`116`），按位展开成 32 条中文报警，去重后加入列表（带时间）；

- 「复位」按钮：清空列表 + 写 `DB5.200.0 = true`（PLC 侧复位报警字）；

- 报警位定义：
  
  | 位        | 报警         | 位         | 报警          |
  | -------- | ---------- | --------- | ----------- |
  | 字1.0     | 急停按下       | 字2.0      | Z 轴跟踪气缸下降故障 |
  | 字1.1     | 系统未上电      | 字2.1      | 1# 顶料缸上升故障  |
  | 字1.2     | IGBT 电源故障  | 字2.2      | 1# 顶料缸下降故障  |
  | 字1.3     | 气压不足       | 字2.3      | 2# 顶料缸上升故障  |
  | 字1.4     | 能量报警       | 字2.4      | 2# 顶料缸下降故障  |
  | 字1.5     | 喷风模式未选择    | 字2.5      | 3# 顶料缸上升故障  |
  | 字1.6/1.7 | 备用         | 字2.6      | 3# 顶料缸下降故障  |
  | 字1.8     | X 轴伺服故障    | 字2.7      | 4# 顶料缸上升故障  |
  | 字1.9     | Y 轴伺服故障    | 字2.8      | 4# 顶料缸下降故障  |
  | 字1.10    | Z 轴伺服故障    | 字2.9~2.15 | 备用          |
  | 字1.11    | 冷却水温过高     |           |             |
  | 字1.12    | 冷却水温过低     |           |             |
  | 字1.13    | Y 跟踪气缸前进故障 |           |             |
  | 字1.14    | Y 跟踪气缸后退故障 |           |             |
  | 字1.15    | Z 跟踪气缸上升故障 |           |             |

### 4.8 SystemSet / 主题

- `SystemSet.xaml`：**空壳**（`GroupBox Header="参数设置"`，里面只有空 Grid），重写时可忽略或自定内容；
- `UserPaletteSelector`：MaterialDesign 调色板选择，保存/加载 `Data/Palette/Palette.txt`（JSON）。

---

## 5. 数据采集链路

```
S7-1200 PLC
  │  DB2.0 起 141 字节（一次批量读）
  ▼
S71KConnect.ThreadBackgroundReadPlc()   ← 独立线程，while(true)，无 sleep
  │  解析到静态对象 Models.UserItems
  ▼
┌──────────────┬──────────────┬──────────────┐
│ HomePage     │ CurvePage    │ AlarmPage    │
│ 100ms 定时拷 │ 200ms 采样   │ 200ms 读报警 │
│ 到绑定属性   │ + 序列化缓冲 │ 位            │
└──────────────┴──────┬───────┴──────────────┘
                      │ 工艺完成（DB2.140）
                      ▼
             去重 → DAO 批量写 Access
```

### DB2 数据块布局（141 字节）

| 偏移      | 类型      | 含义          | 偏移      | 类型     | 含义             |
| ------- | ------- | ----------- | ------- | ------ | -------------- |
| 0       | float   | 电压 V        | 80      | float  | 温度 1（轨顶）       |
| 4       | float   | 电流 A        | 84      | float  | 温度 2（轨脚边）      |
| 8       | float   | 频率 Hz       | 88      | float  | 温度 3（内轨脚）      |
| 12      | float   | 功率 kW       | 92      | float  | 温度 4（冷却）       |
| 16      | float   | 能量（界面标 KWS） | 96      | float  | 流量 1（轨顶）       |
| 20      | float   | X 实际位置      | 100     | float  | 流量 2（轨腰）       |
| 24      | float   | X 实际速度      | 104     | float  | 流量 3（轨底）       |
| 28      | float   | X 设定位置      | 108     | float  | 压力             |
| 32      | float   | X 设定速度      | 112     | bool   | 加热运行           |
| 36      | float   | X 实际电流      | 113     | bool   | 冷却运行           |
| 40–56   | float×5 | Y 轴同上       | 114     | ushort | 报警字 1          |
| 60–76   | float×5 | Z 轴同上       | 116     | ushort | 报警字 2          |
| 118     | float   | 长度 1        | 126     | bool   | Y 气缸前限位        |
| 122     | float   | 长度 2        | 127     | bool   | Y 气缸后限位        |
| 128/129 | bool    | Z 气缸前/后限位   | 130–137 | bool   | 1#~4# 顶料缸上/下限位 |
| 138     | bool    | 电源状态        | 139     | bool   | 喷气状态           |
| 140     | bool    | 工艺完成        |         |        |                |

> 布尔量是**按单字节**（`TransBool(byte[], offset)`）解析的，即每个 BOOL 在 DB2 里独占一个字节；与 PLC 侧 DB 定义必须一致，需现场确认。

---

## 6. 数据库设计与操作

### 6.1 分库策略

```
Data/
├─ AccessTemplate/Template.accdb     # 空库模板（含 DataSheet 表结构）
├─ WorkData/{yyyy}/{yyyyMM}/{yyyyMMdd}.accdb   # 每日生产库（登录时选的日期决定）
├─ UserData/User.accdb               # 用户库（代码中未见引用）
├─ Recipe/DataRecipe.xml             # 配方（实际是二进制序列化）
├─ Temp/dat.xml                      # 采集缓冲（实际是二进制序列化）
├─ CFG/LoginCFG.ini                  # 登录配置
├─ Name/name.txt                     # 地名/班组等名称列表
├─ Palette/Palette.txt               # 主题配色
└─ Log/                              # 日志目录（代码中未见写入）
```

建库逻辑：`DBHelp.CreteFilesName()` 判断目录/文件是否存在，不存在则从模板 `File.Copy` 生成。

### 6.2 表结构（`DataSheet`，按 DAO 写入字段推断）

| 字段           | 类型   | 说明   | 字段             | 类型   | 说明      |
| ------------ | ---- | ---- | -------------- | ---- | ------- |
| serialNumber | text | 焊缝编号 | temperature1~4 | text | 四路温度    |
| operatorName | text | 操作员  | flow1~3        | text | 三路流量    |
| jobNumber    | text | 工号   | pressure       | text | 压力      |
| voltage      | text | 电压   | heatTime       | text | 加热用时(s) |
| current      | text | 电流   | coolingTime    | text | 冷却用时(s) |
| power        | text | 功率   | energy         | text | 能量      |
| frequency    | text | 频率   |                |      |         |

> 所有数值字段都是 **text**，写入时统一 `ToString("f1")`，查询后需自行转数值。

### 6.3 数据库操作 API

| 方法                                             | SQL / 实现                                         | 用途            |
| ---------------------------------------------- | ------------------------------------------------ | ------------- |
| `DBHelp.ExecuteCommand(sql)` / `(sql, params)` | OleDbCommand                                     | 增删改           |
| `DBHelp.GetDataSet(sql)`                       | OleDbDataAdapter                                 | 查询成 DataSet   |
| `DBHelp.GetReader(sql)`                        | OleDbDataReader                                  | 逐行读           |
| `CQServices.AddHeatRecords(record)`            | `INSERT INTO [HeatDataSheet](...)`               | 单条插入          |
| `CQServices.GetAllRecords()`                   | `SELECT * FROM [DataSheet]`                      | 全部记录          |
| `CQServices.GetRecordBySql(sql)`               | 自定义 SQL → `List<DataBaseRecord>`                 | 查询映射          |
| `CQServices.GetRecordSql(sql)`                 | 自定义 SQL → `List<string>`（取第一列）                   | 编号去重检查        |
| `CQServices.DeleteRecord(编号)`                  | `DELETE DataSheet... WHERE serialNumber = '...'` | 按编号删          |
| `CQServices.BulckInsert(list)` → `DBHelp.add`  | DAO Recordset 逐条 AddNew/Update                   | 批量写入（实际采集用这条） |

> **注意**：`AddHeatRecords` 写的是 `HeatDataSheet` 表，而查询/删除/批量插入用 `DataSheet`；`DBHelp.CreateSheetData()` 建的是中文列名表。
> 三者不一致，说明单条插入这条路径要么没被使用、要么对应旧版模板。**重写时以 `DataSheet` + 18 个英文字段为准。**

---

## 7. 配方系统

### 7.1 结构

```csharp
MyDataRecipe      { string ListName; List<MyDataRecipeItem> MyDataRecipeList; }
MyDataRecipeItem  { string ValueName; string Address; object Data; RecipeDataType DataType; }
RecipeDataType    { MyBool, MyFloat, MyInt }
```

- 持久化：`Data/Recipe/DataRecipe.xml`（**BinaryFormatter 二进制**，不是 XML）；
- 按钮语义：
  - **新建**：名称不能为空、不能重名 → 用当前界面值生成一个配方存盘；
  - **保存**：覆盖当前选中配方（先删后加）；
  - **装载**：把选中配方逐项 `Write(Address, value)` 下发到 PLC，并记住上次选择（读 INI）；
  - **删除**：确认后移除并重写文件。

### 7.2 参数 → 地址映射（31 项）

| 参数         | 地址        | 类型    | 参数     | 地址        | 类型    |
| ---------- | --------- | ----- | ------ | --------- | ----- |
| Y 轴焊缝零位    | DB6.312   | float | 起始温度设定 | DB6.320   | float |
| 焊缝高度       | DB6.308   | float | 转变温度设定 | DB6.328   | float |
| Y 轴补偿零位    | DB6.188   | float | 最终温度设定 | DB6.336   | float |
| Z 轴补偿零位    | DB6.232   | float | 起始功率设定 | DB6.316   | float |
| Y 自动补偿使能   | DB6.186.0 | bool  | 转变功率设定 | DB6.324   | float |
| Z 自动补偿使能   | DB6.230.0 | bool  | 最终功率设定 | DB6.332   | float |
| 自动找焊缝使能    | DB6.306.0 | bool  | 喷风温度控制 | DB6.340.1 | bool  |
| X 轴起始位置    | DB6.274   | float | 喷风时间控制 | DB6.340.0 | bool  |
| X 轴起始位速度   | DB6.278   | float | 喷风温度设定 | DB6.346   | float |
| X 轴找焊缝最大距离 | DB6.282   | float | 喷风时间设定 | DB6.342   | float |
| X 轴找焊缝速度   | DB6.286   | float | 风压上限   | DB6.226   | float |
| X 轴加热距离    | DB6.290   | float | 风压下限   | DB6.230   | float |
| X 轴加热位速度   | DB6.294   | float | 流量上限   | DB6.234   | float |
| X 轴喷风距离    | DB6.298   | float | 流量下限   | DB6.238   | float |
| X 轴喷风移动速度  | DB6.302   | float | 能量上限   | DB6.242   | float |
|            |           |       | 能量下限   | DB6.246   | float |

> 代码里有**两处地址疑似笔误/重叠**，重写时必须和 PLC 程序逐项核对：
> 
> 1. `X轴加热距离` 的输入框 setter 写的是 **`DB6.286`**（和"X轴找焊缝速度"同址），而读取 (`ReadFloat`) 和配方映射用的都是 **`DB6.290`** —— 改这个参数会覆盖找焊缝速度；
> 2. `Z自动补偿使能` 写 **`DB6.230.0`**（字节 230 的 bit0），而 `风压下限` 写 **`DB6.230`**（float，占 230–233 字节），两者重叠。
> 
> 另有 `喷风温度控制` / `喷风时间控制` 用 `DB6.340.1` / `DB6.340.0` 两个位，注意不要和相邻 float 冲突。

---

## 8. 配置与数据目录

| 路径                                   | 用途                       |
| ------------------------------------ | ------------------------ |
| `Data/CFG/LoginCFG.ini`              | 操作员姓名索引、接头号              |
| `Data/Name/name.txt`                 | 地名/线名/班组名称列表             |
| `Data/Palette/Palette.txt`           | MaterialDesign 调色板（JSON） |
| `Data/AccessTemplate/Template.accdb` | 每日库模板                    |
| `Data/WorkData/...`                  | 每日生产数据                   |
| `Data/Recipe/DataRecipe.xml`         | 工艺配方（二进制）                |
| `Data/Temp/dat.xml`                  | 采集缓冲（二进制）                |
| `Data/UserData/User.accdb`           | 用户库（未见引用）                |
| `Data/Log/`                          | 日志目录（未见写入）               |

---

## 9. 已知问题 / 重写注意点

| #   | 问题                                                              | 影响                                            | 重写建议                                                     |
| --- | --------------------------------------------------------------- | --------------------------------------------- | -------------------------------------------------------- |
| 1   | `ThreadBackgroundReadPlc` 是 `while(true)` 且**没有 sleep**，读失败也不退避 | 空转占 CPU、通信压力大                                 | 换成 `PeriodicTimer`/`BackgroundService`，固定 100–200ms，失败退避 |
| 2   | `StopPLCRead()` 用 `Thread.Abort()`                              | .NET Core+ 直接 `PlatformNotSupportedException` | `CancellationToken` + `Task`                             |
| 3   | 采集时**每 200ms 把整个列表 `BinaryFormatter` 序列化一次**                    | IO 放大、GC 压力、掉电可恢复但很重                          | 内存缓冲 + 定时批量落库；别再每拍写盘                                     |
| 4   | `BinaryFormatter` 已被 .NET 9+ 移除                                 | 无法直接迁移                                        | 配方/缓冲改 `System.Text.Json` 或直接进 SQLite                    |
| 5   | 曲线/报警/主页/主窗各自起线程 + 定时器轮询同一份 `userItem`                          | 线程多、无同步                                       | 统一由采集服务发布快照，VM 订阅                                        |
| 6   | `DataSheet` / `HeatDataSheet` / 中文表头三种表名并存                      | 容易写错库                                         | 统一一张表 + 迁移脚本                                             |
| 7   | 数值全存 text                                                       | 无法排序/聚合                                       | 改 float/int 列（SQLite 无类型压力）                              |
| 8   | 数据库按天分库、路径靠 `..\..\` 相对目录                                       | 部署位置一变就失效                                     | 用 `AppContext.BaseDirectory` + 配置项                       |
| 9   | `AutoPage` 每个属性 setter 直接写 PLC                                  | 输入过程中可能连发多次写                                  | 改为"修改→保存/下发"显式提交                                         |
| 10  | 登录窗没有密码校验                                                       | 无权限控制                                         | 需要的话补用户表 + 角色                                            |
| 11  | `SystemSet` 空页、`DataQueryPage` 无 VM、`SampleProgressDialog` 未使用  | 结构不齐                                          | 新版按页面补齐 VM                                               |
| 12  | 配方里两处地址疑似重复/笔误                                                  | 下发可能覆盖                                        | 与 PLC 程序逐项核对地址表                                          |

---

## 10. 与 RailWeldApp（新版）的对应关系

| 旧（NormalizingApp）   | 新（RailWeldApp）                                 | 说明                                  |
| ------------------- | ---------------------------------------------- | ----------------------------------- |
| `LoginWindow`       | `LoginView`                                    | 新版改为账号密码 + 品牌页                      |
| `MainWindow` + 汉堡菜单 | `AdminView` + 顶部导航                             | 无侧边栏的 Web 式外壳                       |
| `HomePage`（仪表盘）     | `AdminDashboardView` + `AdminRealTimeDataView` | 拆成产品首页 + 实时数据                       |
| `ManualPage`        | `AdminManualControlView`                       | 8 组点动按钮 → 8 张控制卡                    |
| `AutoPage`          | `AdminAutoControlView`                         | 三卡参数 + 配方管理                         |
| `CurvePage`         | `AdminTempMonitorView`                         | 曲线改为电平条 + 大数字（性能优先）                 |
| `DataQueryPage`     | `AdminHistoryQueryView`（占位）                    | 待接 SQLite 查询                        |
| `AlarmSystemPage`   | `AdminFaultAlarmView`（占位）                      | 待接报警表                               |
| `SystemSet` + 主题    | `AdminSettingsView`                            | 设置页留了扩展位                            |
| `Template.accdb` 分库 | EF Core + SQLite                               | `DataModels/ApplicationDataContext` |

**已经在做/建议保持的对应关系**（供重写时对照契约）：

- 采集：`IPlcService` / `PlcPollingBackgroundService`（新版）+ `PlcSnapshot`（对应旧 `userItem`）；
- 通信：`ModbusRtuService`（新版走 Modbus RTU，旧版是 S7 直连；地址表需按协议重新映射）；
- 配方：`AdminAutoControlViewModel.CoolingModes`、阈值、伺服参数等属性与旧版 31 项参数一一对应；
- 记录：旧版每 200ms 一条 → 新版建议**每秒聚合一行**（性能实测见 `docs/` 中相关记录）。

---

## 11. 无硬件调试

```powershell
# 1. 启动模拟器（监听 127.0.0.1:102，生成 DB2 141 字节数据）
NormalizingApp.Host\bin\Debug\NormalizingApp.Host.exe

# 2. 启动上位机（S71KConnect 里 PLC IP 默认 127.0.0.1）
NormalizingApp\bin\Debug\NormalizingApp.exe
```

现场部署时只需改 `Lib/S71KConnect.cs` 第 13 行的 IP 与机架/槽位配置；新版对应 `appsettings.json` 的 PLC 配置节。

---

## 12. 数据量估算与落库建议

### 12.1 旧方案存了多少

采样是 **200ms 一拍（5 Hz）**，加热阶段每一拍都 `recLst.Add` 一行；冷却数据只是回填到其中一行的空字段里。所以**每次焊接约 300 行**（按加热 60s 计），而且每行都重复写 `serialNumber / operatorName / jobNumber`，只有时间戳和几个测量值在变。

按「加热 60s、冷却 60s、200 次/天、250 天/年」估算：

| 指标 | 数量 |
| --- | --- |
| 每行数据 | 18 个文本字段，Access 约 250–400 B |
| 每次焊接 | 300 行 ≈ **75–120 KB** |
| 每天 | 15–24 MB |
| 每年 | **3.8–6 GB**（按日分库，单库 15–24 MB） |
| 缓冲文件写入 | 每 200ms 重写整个列表，单次焊接累计约 **13–45 MB**（每天 3–9 GB 打在同一文件上） |

两个结论：

1. **入库行数虚高**：300 行里绝大部分是重复的表头字段，200ms 粒度的温度对追溯也没有意义（相邻两行常常只差 1℃）；
2. **缓冲文件写法更伤**：`BinaryFormatter` 每次全量序列化，写入量是数据本身的几十倍，长期跑对工控机磁盘（尤其 CF/SD 卡）不友好。

### 12.2 建议的新模型（两张表）

```sql
-- 焊缝主表：一行 = 一次焊接，查询/报表都只看这张
CREATE TABLE WeldRecord (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    SerialNumber   TEXT NOT NULL,
    OperatorName   TEXT,
    JobNumber      TEXT,
    StartedAt      TEXT NOT NULL,
    HeatSeconds    REAL,
    CoolingSeconds REAL,
    PeakTemp1      REAL,
    PeakTemp2      REAL,
    PeakTemp3      REAL,
    Energy         REAL,
    Result         TEXT              -- 正常 / 超限
);
CREATE UNIQUE INDEX IX_WeldRecord_Serial ON WeldRecord(SerialNumber);
CREATE INDEX IX_WeldRecord_Date ON WeldRecord(StartedAt);

-- 采样表：一行 = 1 秒，只存会变的量
CREATE TABLE WeldSample (
    WeldId    INTEGER NOT NULL,
    ElapsedMs INTEGER NOT NULL,
    Phase     INTEGER NOT NULL,      -- 0=加热 1=冷却
    Temp1     REAL, Temp2 REAL, Temp3 REAL, Temp4 REAL,
    Flow1     REAL, Flow2 REAL, Flow3 REAL,
    Pressure  REAL, Power REAL,
    PRIMARY KEY (WeldId, ElapsedMs)
);
```

要点：

- **落库降采样到 1 Hz**（2 分钟焊接 = 120 行），5 Hz 只在内存里给实时刷新用；
- 表头字段只存一次，样本表不重复编号/操作员；
- 峰值、总能量、时长在结束时算好写进主表，**统计报表不用扫样本表**；
- 曲线回放只查一次焊接的样本（120 行），表格页只查主表（一次焊接一行）。

同样的工况，新方案约 **8–10 KB/次**：每天 1.6–2 MB、每年 400–500 MB，比旧方案小一个数量级；即使保留 5 Hz 也只有约 2 GB/年，SQLite 完全扛得住。

### 12.3 历史数据迁移

旧库里"一次焊接 N 行"要先聚合再导入：

1. 按 `serialNumber` 分组：`min(heatTime)` 作起始、`max(heatTime)` 作加热时长、`max(coolingTime)` 作冷却时长；
2. 按行号重建 `ElapsedMs`（旧数据只有 `heatTime`/`coolingTime` 的秒字符串）；
3. 温度/流量/压力逐行写入 `WeldSample`，`Phase` 由 `coolingTime` 是否为空判断；
4. 去重后写 `WeldRecord`（旧表同一编号可能有多行重复，`DeleteRecord` 就是处理这个的）。

### 12.4 保留与导出

- 热数据保留 6 个月在线，之后按季度导出 CSV/Excel 归档并从库中删除；
- 导出以 `WeldRecord` 为主表，需要曲线时再 join `WeldSample`；
- 旧版"每天一个 `.accdb`"的分库方式可以放弃：单库 + 时间索引 + 定期归档，部署时少一个模板文件和一串目录。
