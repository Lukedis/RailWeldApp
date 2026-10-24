# 图片素材来源

| 文件 | 用途 | 来源 | 许可 |
| --- | --- | --- | --- |
| `HomeHero.jpg` | 备用素材（首屏已改为纯 XAML 蓝线机械臂插画，当前不再加载） | [Wikimedia Commons — Weld of flash-butt welding.jpg](https://commons.wikimedia.org/wiki/File:Weld_of_flash-butt_welding.jpg) | CC BY-SA 4.0 |
| `HomeRailScene.jpg` | 总览页"现场产线"装饰图 | [Wikimedia Commons — Baikal Railway, Khabartuy Cape](https://commons.wikimedia.org/wiki/File:Baikal_Railway,_Railway_track,_Khabartuy_Cape,_Lake_Baikal,_Russia.jpg) | CC BY 4.0 |
| `HomeWeldSparks.jpg` | 总览页"焊接过程"装饰图 | [Openverse — TIG Welding Sparks (Public Domain Photos)](https://openverse.org/) | CC BY 2.0 |
| `LoginHero.png` | 登录页右侧大图 | 项目原有素材 | 内部 |
| `User.jpg` | 默认头像 | 项目原有素材 | 内部 |

## 使用注意

- `HomeHero.jpg` 是 **CC BY-SA 4.0**、`HomeRailScene.jpg` 是 **CC BY 4.0**、`HomeWeldSparks.jpg` 是 **CC BY 2.0**：对外发布时请在关于页/文档里保留署名与许可链接；如果不想承担署名义务，换成自有拍摄的现场照片即可，界面不需要改（文件名与资源键都不用变）。
- 尺寸：`HomeHero.jpg` 900×675（191 KB）、`HomeRailScene.jpg` 1200×675（228 KB）、`HomeWeldSparks.jpg` 900×675（71 KB）；`ImageResources.xaml` 里用 `DecodePixelWidth` 限制解码尺寸，避免 4K 屏上放大解码。

## 音效

`Assets/Sound/` 下的 4 个 WAV（`UINotification` / `UISuccess` / `UIError` / `UISwipe`）由 `ISoundService` 统一播放，通过 `helper:SoundHelper.ClickSound` 挂在按钮上：

```xml
<Button helper:SoundHelper.ClickSound="UISwipe" />
```

现有挂载点：`AccentButton`（所有主按钮）、`HeaderIconButton`（通知）、`AdminNavCtaStyle`（返回登录）、`LoginPrimaryButtonStyle`（登录）、`DrawerControl`（抽屉开合）。
