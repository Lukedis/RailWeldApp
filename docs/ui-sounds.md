# UI 音效对照表

音效文件在 `Assets/Sound/`，全部是 **PCM WAV**，由 `ISoundService` 统一播放与缓存。

> **缺文件不会崩**：`SoundService` 找不到资源或解码失败时只写一条 `Debug.WriteLine` 并返回 null，播放调用直接跳过。换音效、改名、删文件都不会再抛 `IOException`。

视图层通过附加属性挂载，不需要 VM 参与：

```xml
<Button helper:SoundHelper.ClickSound="Popup" />
<TabControl helper:SoundHelper.SelectionSound="Popup" />
<CheckBox helper:SoundHelper.CheckedSound="Popup" />
<TextBox helper:SoundHelper.TypingSound="{x:Null}" />   <!-- 显式关掉这个控件的打字音 -->
```

名字也支持逗号列表（`"Popup,Success"`），播放时随机挑一个。

## 现有 3 个音效

| 文件 | 时长 | 用途 |
| --- | --- | --- |
| `Popup.wav` | 1.02 s | 通用交互：按钮、下拉、页签、勾选、抽屉 |
| `Success.wav` | 1.75 s | 成功类动作：确认开工；后续可接「保存成功」 |
| `Failure.wav` | 1.05 s | 失败/危险类：登录出错、复位 PLC |

## 已挂载位置

| 控件 / 样式 | 音效 |
| --- | --- |
| `AccentButton`（连接设备、打印、确认…） | `Popup` |
| `SecondaryButton` / `GhostButton` / `LinkButtonStyle` / `IconTextButtonStyle` | `Popup` |
| 通知按钮（铃铛）/ `CircleIconButton` | `Popup` |
| 顶部导航 / 返回登录 | **静音** |
| 登录页（按钮、输入框、记住账号） | **静音** |
| 各页动作按钮（配方、确认全部、导出…） | `Popup` |
| 报警页「复位 PLC」 | `Failure` |
| 账户面板「确认开工」 | `Success` |
| 账户面板页签切换 | `Popup` |
| 日期选择器日历按钮 | `Popup` |
| 复选框勾选 / 取消 | `Popup` |
| 通知抽屉开合 | `Popup` |
| 手动控制圆形按钮 | `Popup` |
| 所有输入框打字 | **静音**（当前素材没有合适的短打字音） |

登录页的错误提示在 `LoginViewModel` 里播 `Failure`。

## 换音效的正确姿势

1. 新文件放进 `Assets/Sound/`（**必须是 PCM WAV**：44.1k/48k、16 bit；`SoundPlayer` 不认 MP3、浮点、ADPCM）；
2. `RailWeldApp.csproj` 里加一行 `<Resource Include="Assets\Sound\你的文件.wav" />`；
3. 改样式里的名字（`Popup` / `Success` / `Failure` 或新名字）；
4. 跑一遍 `E:\AppAI\tools\sound_report.py`，确认没有 `MISSING`。

## 维护脚本

- `E:\AppAI\tools\sound_report.py`：列出「被引用 / 缺失 / 未使用」的音效
- `E:\AppAI\tools\check_resources.py`：检查 XAML 里所有 `{StaticResource}` 是否都有定义
