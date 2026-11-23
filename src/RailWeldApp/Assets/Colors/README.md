# 颜色资源字典（单主题 · 浅色）

所有颜色集中在这里，`Styles/`、`Themes/`、`View/` 下的 XAML **不允许**出现十六进制色值或 `White`/`Black` 这类命名色，一律写成：

```xaml
Background="{StaticResource SurfaceCanvasBrush}"
Foreground="{StaticResource TextSecondaryBrush}"
BorderBrush="{StaticResource BorderBrush}"
```

> 用 `StaticResource` 而不是 `DynamicResource`：单主题下颜色不会变，静态解析比动态解析快约 5 倍（实测 0.8µs vs 5.6µs/次），也不会登记资源失效回调。

## 目录结构

```
Assets/Colors/
├─ Primitives.xaml              色值（<Color>，唯一写十六进制的地方）
├─ Global/                      全局画刷
│  ├─ Surfaces.xaml             表面 + 描边层级 + 遮罩
│  ├─ Text.xaml                 文字层级
│  ├─ Accent.xaml               主色蓝 + 语义色
│  └─ Gradients.xaml            页面底、侧栏、主按钮、Logo、登录展示区
├─ Controls/                    只被某个控件用的颜色
│  ├─ NavigationButton.xaml     导航项 + InfoBadge + 消息角标
│  └─ Inputs.xaml               TextBox / PasswordBox / 搜索框 / CheckBox
└─ Pages/                       只被某个页面用的颜色
   ├─ Login.xaml                展示区图表色、卡片描边、柔光
   ├─ AdminShell.xaml           品牌 Logo、侧栏提示卡、顶部搜索、通知抽屉提示卡
   └─ AdminSettings.xaml        设置页底色、卡片、状态徽章、表格头
```

画刷文件只引用 `Primitives.xaml` 的色值，**色值不出现第二处**；换主色只改 `Primitives.xaml` 的 `Accent*`。

## 登录页品牌面板（唯一的深色区域）

`Brand*` / `OnBrand*` 色值只服务登录页左侧品牌栏，**不允许**在其它页面使用：

| 键 | 用途 |
|---|---|
| `BrandPanelStart` / `BrandPanelEnd` | 面板背景渐变两端（深藏青） |
| `BrandChip` / `BrandHairline` | 面板内卡片底色与 1px 描边（半透明白） |
| `BrandGlow` | 右上角柔光（径向渐变，禁止换成 BlurEffect） |
| `OnBrand` / `OnBrandSecondary` / `OnBrandMuted` | 面板上的三级文字 |
| `BrandAccentText` | 面板上的强调文字（标签、图标） |

## 命名约定

| 前缀 | 含义 |
|---|---|
| `Surface*` / `Line*` | 表面色 / 线条色 |
| `Ink*` | 文字与图标（数字越大越弱） |
| `OnAccent` | 叠在强调色、危险色上的反白 |
| `Accent*` | 主色蓝；`AccentTint25…400` 由浅到深的浅色底 |
| `Danger/Success/Warning/Purple/Lime*` | 语义色，各带 `Hover` / `Press` / `Soft` |

## 性能约定

- **不使用 `DropShadowEffect` / `BlurEffect`**。实测：登录页大卡片的 56px 阴影 + 两个 70/80px 模糊光斑，会让整页重绘从 6ms 涨到 160ms；在远程桌面 / 无 GPU 环境下就是 7fps。需要"深度感"用**描边 + 底色层级**（`BorderBrush` / `SurfaceSubtle`）表达。
- 装饰性光晕用 **径向渐变**（`RadialGradientBrush`），成本可以忽略。

## 加一个新颜色

1. `Primitives.xaml` 加 `<Color x:Key="Xxx">#FFxxxxxx</Color>`
2. 对应 `Global|Controls|Pages` 文件加 `<SolidColorBrush x:Key="XxxBrush" Color="{StaticResource Xxx}" />`
3. 页面/样式里用 `{StaticResource XxxBrush}`