# MiniGame - 小游戏功能组件库

> 版本: 2.1 | 更新日期: 2026-05-26 | 作者: Felix/李康康

**完整文档**：见包内 [`Documentation~/index.md`](Documentation~/index.md)（快速开始、运行时 API、广告、构建、Editor 菜单等）。

---

## 1. 功能定位

MiniGame 文件夹是一个**通用的小游戏功能组件库**，为 Unity 项目提供跨平台小游戏开发所需的核心能力封装，包括广告变现、平台适配、构建发布等。本模块独立于具体业务逻辑，可被任何 Unity 小游戏项目复用。

---

## 2. 包含内容

### 2.1 多平台广告管理系统

基于**适配器模式 + 工厂模式**构建的统一广告管理框架，支持以下平台：

| 平台       | 宏定义           | 广告SDK            |
| ---------- | ---------------- | ------------------ |
| 微信小游戏 | `WEIXINMINIGAME` | WeChatWASM SDK     |
| 抖音小游戏 | `DOUYINMINIGAME` | TTSDK (字节跳动)   |
| Web (H5)   | `UNITY_WEBGL`    | JS插件 (jslib)     |
| Android    | `UNITY_ANDROID`  | 原生AAR/JAR插件    |
| iOS        | `UNITY_IOS`      | 原生Framework插件  |
| Editor     | `UNITY_EDITOR`   | 模拟适配器(调试用) |

支持的广告类型：Banner、插屏、激励视频、自定义广告

> 💡 **重要变更 (v2.1)**: 激励视频广告与时间缩步长 (`Time.timeScale`) 完全解耦。框架不再强制将 `Time.timeScale` 设为 1，将时间控制权完全移交给具体的游戏业务层。

### 2.2 多平台构建工具

统一的构建菜单入口，覆盖所有小游戏目标平台：

- **物理级 SDK 热插拔隔离 (Platform Switcher)**: 提供右上角的 Platform Switcher 下拉框。一键切换平台时，自动将其他平台不相关的 SDK 移动到存档目录，彻底解决不同 SDK 间的同级编译冲突，同时自动为您配置 `Build Target` 和 `Scripting Define Symbols`。
- **智能动态构建菜单**: 构建菜单会根据当前通过 Platform Switcher 激活的平台进行“智能隐身”，只显示当前平台的构建选项，防止跨平台交叉构建导致的严重污染。
- **微信小游戏**：微信 Provider + WebGL 构建
- **抖音小游戏**：默认 Provider + WebGL 构建
- **WebGL**：发布设置优化 + Addressables + Player 构建
- **Android**：APK 构建 & 构建并运行
- **iOS**：Xcode 工程构建

### 2.3 小游戏工具包 (MiniGameKit)

微信/抖音平台通用功能封装，具备极高的鲁棒性：

- **生命周期订阅**: 提供 `MiniGameKit.OnMiniGameShow` 和 `MiniGameKit.OnMiniGameHide` 事件，使业务层可轻松监听前后台切换。
- **参数化分享 (ShareApp)**: 支持动态注入 `query` 参数进行渠道跟踪。
- **自定义 Banner (CreateBannerAd)**: 支持动态指定 Banner 广告的位置（left, top）和尺寸（width, height），避免硬编码。
- **参数化客服与场景 (OpenBusinessView)**: 统一且高度参数化的特定微信业务场景打开接口。
- **对称式振动接口**:
  - `VibrateShort()`: 短震动反馈（触觉轻微反馈）。
  - `VibrateLong()`: 长震动反馈（触觉强烈反馈）。
  - 完美适配微信、抖音、原生 Web、Android 和 iOS。
- **现代化单例模型**: `Singleton<T>` 使用全新 `FindFirstObjectByType` 并引入线程锁保护，提供极佳的单例安全保障。

### 2.4 Editor 工具

- **项目设置** (`Tools/Minigame/项目设置`)：I2/字体/Android 路径与签名
- **本地化** (`Tools/Minigame/本地化/`)：导入 I2 CSV、自动绑定 Localize
- **UI** (`Tools/Minigame/UI/`)：Text 转 TextMeshPro
- **字体** (`Tools/Minigame/字体/`)：字符集收集与导出
- **广告** (`Tools/Minigame/广告/`)：管理器调试
- **平台切换** (Editor 工具栏右上角)：Platform Switcher 下拉框
- **构建** (`Tools/Minigame/构建/`)：动态多平台构建、Addressables、优化
- **Android** (`Tools/Minigame/Android/`)：签名配置
- **脚本** (`Tools/Minigame/脚本/`)：C# 转 UTF-8
- **工具** (`Tools/Minigame/工具/`)：清理 PlayerPrefs 等调试工具
- **UI** 扩展：关闭无用 RaycastTarget、批量添加 Prefab 组件
- **构建/优化** 扩展：WebGL ASTC 贴图、Shader Variant Collection

---

## 3. 目录结构

```
Packages/MiniGameKit/
├── Documentation~/                   # 开发者文档（不随包体发布）
├── Runtime/                          # 运行时程序集 (MiniGameKit.Runtime)
│   ├── MiniGameKit.cs               # 小游戏工具包(分享/客服/振动等)
│   ├── Utils/                       # 通用工具
│   │   ├── Singleton.cs             # 单例基类
│   │   ├── MiniGameInit.cs          # 小游戏初始化
│   │   └── SkipUnityLogo.cs        # 跳过Unity启动Logo
│   ├── Addressables/                # 微信小游戏 Addressables Provider
│   │   └── WXAssetBundleProvider.cs # WXAssetBundleProvider + WXBundledAssetProvider
│   └── Ad/                          # 广告管理系统
│       ├── AdManager.cs             # 广告管理器(统一入口)
│       ├── AdSystem-Design.md       # 广告系统技术设计文档
│       ├── Core/                    # 核心接口与定义
│       │   ├── AdPlatform.cs        # 平台枚举
│       │   ├── AdType.cs            # 广告类型枚举
│       │   ├── AdState.cs           # 广告状态枚举
│       │   ├── IAdUnit.cs           # 广告单元接口族
│       │   ├── IAdAdapter.cs        # 适配器接口
│       │   ├── AdConfig.cs          # 配置数据
│       │   ├── AdPlatformDetector.cs# 平台自动检测
│       │   └── AdAdapterFactory.cs  # 适配器工厂
│       └── Adapter/                 # 平台适配器实现
│           ├── EditorAdAdapter.cs   # Editor模拟
│           ├── WeChatAdAdapter.cs   # 微信小游戏
│           ├── DouyinAdAdapter.cs   # 抖音小游戏
│           ├── WebAdAdapter.cs      # Web/H5
│           └── MobileAdAdapter.cs   # Android/iOS
└── Editor/                          # Editor程序集 (MiniGameKit.Editor)
    ├── PlatformSwitcher/            # 物理级平台隔离
    │   └── PlatformSwitchTool.cs    # 核心热插拔逻辑
    ├── Ad/                          # 广告Editor工具
    │   └── AdManagerEditorWindow.cs # 调试窗口
    └── Build/                       # 构建工具
        ├── MiniGameBuildMenu.cs     # 动态多平台构建菜单
        ├── MiniGameBuildPipeline.cs # 构建管线核心逻辑
        ├── MiniGameBuildWindow.cs   # 统一构建配置窗口
        ├── WebGLCiBuild.cs          # WebGL构建流程
        └── AddressablesWeChatBuildMenu.cs  # Addressables Provider管理
```

---

## 4. 设计原则

### 4.1 高内聚、低耦合

- **模块边界清晰**：Runtime 和 Editor 通过程序集定义（asmdef）严格隔离
- **接口驱动**：业务层仅依赖 `IAdUnit` / `IAdAdapter` 接口，不依赖具体平台实现
- **适配器模式**：每个平台独立实现适配器，新增平台无需修改已有代码
- **工厂模式**：`AdAdapterFactory` 根据平台创建适配器，业务层无需关心平台细节

### 4.2 物理级 SDK 隔离与宏定义

平台特有代码不仅通过 Unity Scripting Define Symbols 隔离，更通过 **Platform Switcher** 实现了物理文件的热插拔隔离（未选中的 SDK 将被移至项目根目录的 `SDKs/` 存档文件夹）：

```csharp
#if WEIXINMINIGAME    // 微信小游戏
#if DOUYINMINIGAME    // 抖音小游戏
#if UNITY_WEBGL       // Web平台
#if UNITY_ANDROID     // Android
#if UNITY_IOS         // iOS
#if UNITY_EDITOR      // Editor
```

配合物理隔离，未激活的平台 SDK 代码根本不会出现在 `Assets` 下，彻底解决底层插件冲突与编译时报错，实现真正的“零时开销”。

### 4.3 生命周期安全

- 所有广告单元实现 `IDisposable`，统一资源释放
- `AdManager` 和 `MiniGameKit` 基于 `Singleton<T>` 自动进行 `DontDestroyOnLoad` 托管，场景切换不销毁
- 异步回调中检查 `isDestroyed` 标记，防止操作已销毁对象
- 平台适配器包含 try-catch 异常保护

---

## 5. 开发规范

### 5.1 程序集定义

- `MiniGameKit.Runtime`：运行时代码，所有平台可用
- `MiniGameKit.Editor`：Editor工具，仅Editor平台编译
- 模块间引用通过 GUID 而非名称，确保重命名安全

### 5.2 命名规范

| 类别     | 规范                 | 示例                                      |
| -------- | -------------------- | ----------------------------------------- |
| 枚举     | Ad前缀 + 语义        | `AdPlatform`, `AdType`, `AdState`         |
| 接口     | I前缀 + 语义         | `IAdUnit`, `IAdAdapter`, `IBannerAdUnit`  |
| 适配器   | 平台名 + AdAdapter   | `WeChatAdAdapter`, `DouyinAdAdapter`      |
| MenuItem | Tools/Minigame/分类/ | `Tools/Minigame/构建/微信小游戏/本地构建` |

### 5.3 文件头注释

所有代码文件必须包含标准文件头：

```csharp
/****************************************************
 * FileName:        {文件名}
 * CreateTime:      {创建时间}
 * Version:         {版本}
 * UnityVersion:    {Unity版本}
 * Description:     {描述}
 *
*****************************************************/
```

### 5.4 扩展规范

新增平台适配器：

1. 在 `AdPlatform` 枚举新增值
2. 实现 `IAdAdapter` 接口和各广告类型 `IAdUnit` 内部类
3. 在 `AdAdapterFactory` 注册创建函数
4. 在 `AdPlatformDetector.Detect()` 添加宏检测分支
5. 在 `MiniGameKit.Editor.asmdef` 添加必要引用

新增构建平台：

1. 在 `MiniGameBuildMenu` 添加带有条件编译 (`#if MACRO`) 的动态 MenuItem 入口
2. 遵循 统一流程：切换Provider → 构建Addressables → BuildPlayer → 结果弹窗

### 5.5 版本控制

- 所有代码变更需通过编译验证
- Editor工具在非Play模式下不可访问运行时实例
- 构建脚本需同时支持 CI 命令行和 Editor 菜单两种入口
