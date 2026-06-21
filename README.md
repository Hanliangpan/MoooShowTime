# MoooShowTime

复古点阵桌面时钟 — Windows Phone 8.1 WinRT 全屏时钟应用

## 应用简介

MoooShowTime 是一款横屏全屏显示的复古风格桌面时钟，采用 7 段数码管点阵像素渲染时间数字，搭配 FC 吃豆人边框巡逻动画和经典幽灵碰撞机制，兼具实用性与趣味性。

## 核心功能

- **数码管时钟** — 7 段点阵像素风格显示 HH:MM，冒号闪烁
- **10 色切换** — 点击屏幕任意位置循环切换：明亮绿 / 白 / 红 / 蓝 / 绿 / 青 / 琥珀 / 紫 / 粉 / 金
- **吃豆人动画** — 金色吃豆人沿屏幕边框巡逻，嘴部开合动画，自动吃掉路径上的豆子
- **幽灵系统** — 4 个经典颜色幽灵（红/粉/青/橙）在路径上随机移动，碰撞后吃豆人反转方向，幽灵 60 秒后重生
- **常亮模式** — 支持「始终常亮」和「仅充电常亮」两种模式，长按/双击切换
- **电量状态** — 左下角显示电池百分比和充电状态
- **主题色装饰** — 角框和边框线自动跟随系统主题色

## 技术架构

```
MoooShowTime/
├── Controls/                    # 自定义控件
│   ├── DotMatrixDigit.xaml/.cs  # 7 段数码管数字控件（核心渲染）
│   ├── ColonSeparator.xaml/.cs  # 冒号闪烁分隔符
│   ├── PixelDigit.xaml/.cs      # 像素字体控件（备用）
│   └── PixelColon.xaml/.cs      # 像素冒号控件（备用）
├── Services/
│   ├── DisplayService.cs        # 屏幕常亮管理
│   └── BatteryService.cs        # 电池状态监控
├── MainPage.xaml/.cs            # 主页面（时钟 + 动画 + 交互）
├── App.xaml/.cs                 # 应用入口
├── Assets/                      # 应用图标和启动画面
├── Package.appxmanifest         # WinRT 应用清单
└── MoooShowTime.sln             # 解决方案文件
```

### 技术栈

| 组件 | 技术 |
|------|------|
| 平台 | Windows Phone 8.1 WinRT |
| UI 框架 | XAML + C# |
| 动画 | DispatcherTimer (25ms / 40FPS) |
| 渲染 | Canvas + Rectangle / Ellipse / Polygon |
| 构建 | MSBuild 14.0 + MakeAppx |

## 交互说明

| 操作 | 功能 |
|------|------|
| 单击屏幕 | 切换数字颜色（10 色循环） |
| 长按屏幕 | 切换常亮模式 |
| 双击屏幕 | 切换常亮模式（替代长按） |
| 按返回键 | 退出应用 |

## 构建与部署

### 环境要求

- Visual Studio 2015 或 MSBuild 14.0
- Windows Phone 8.1 SDK
- 已启用开发者模式的 WP8.1 真机

### 命令行构建

```powershell
& "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" MoooShowTime.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
```

### 部署到真机

使用 `AppDeploy.exe`（位于 Windows Phone SDK 目录）或 Visual Studio 直接部署。

## 性能优化

动画热路径（40FPS）已做以下优化：

- **RotateTransform 缓存** — 避免每帧 new 对象，只更新 `.Angle`
- **屏幕尺寸缓存** — `Window.Current.Bounds` 只读取一次
- **路径常量预计算** — `_pathLen`、`_totalPath`、`_pathW`、`_pathH` 初始化后不变
- **事件订阅去重** — 配合 `NavigationCacheMode.Required` 防止重复注册

## 开发过程中遇到的关键问题

| 问题 | 根因 | 解决方案 |
|------|------|----------|
| 5×7 像素字体不可读 | 低分辨率下数字辨识度不足 | 改用 7 段数码管点阵风格 |
| `ThemeResource SystemAccentColor` 崩溃 | WP8.1 WinRT 无此资源键 | 代码容错读取，回退默认蓝 |
| `UISettings.GetColorValue` 编译失败 | WP8.1 WinRT 阉割了 UWP API | 从 Application.Resources 容错获取 |
| 动画卡顿 / GC 抖动 | 每帧 new RotateTransform | 缓存变换对象，热路径零分配 |
| 事件重复触发 | NavigationCacheMode.Required 下重复订阅 | 移至构造函数，Cleanup 中移除 |

## License

MIT
