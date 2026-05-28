# 多平台广告管理系统 - 技术设计文档

> 版本: 1.0 | 日期: 2026-05-18 | 作者: Felix/李康康

---

## 1. 概述

### 1.1 目标

在 `Assets/MiniGame` 模块中构建一套**多平台广告管理功能**，通过 Unity 宏定义（Scripting Define Symbols）控制平台编译，实现对以下平台的广告统一接入与管理：

| 平台 | 宏定义 | 广告SDK |
|------|--------|---------|
| 微信小游戏 | `WEIXINMINIGAME` | WeChatWASM SDK |
| 抖音小游戏 | `DOUYINMINIGAME` | TTSDK (字节跳动) |
| Web (H5) | `UNITY_WEBGL` | JS插件 (jslib) |
| Android | `UNITY_ANDROID` | 原生AAR/JAR插件 |
| iOS | `UNITY_IOS` | 原生Framework插件 |
| Editor | `UNITY_EDITOR` | 模拟适配器(调试用) |

### 1.2 设计原则

- **适配器模式**: 每个平台实现 `IAdAdapter` 接口，屏蔽平台差异
- **工厂模式**: `AdAdapterFactory` 根据平台创建对应适配器实例
- **宏隔离**: 平台特有代码通过 `#if` 宏完全隔离，不参与编译
- **接口驱动**: 业务层仅依赖 `IAdUnit` 接口，不依赖具体平台实现
- **生命周期管理**: 广告单元实现 `IDisposable`，统一资源释放
- **全局开关**: 通过 `AdConfig.EnableAd` 控制广告总开关

---

## 2. 系统架构

### 2.1 架构图

```
┌─────────────────────────────────────────────┐
│                 业务调用层                    │
│         (MiniGameKit / 游戏UI等)             │
└──────────────────┬──────────────────────────┘
                   │ 调用
┌──────────────────▼──────────────────────────┐
│              AdManager (单例)                │
│  ┌───────────┐ ┌──────────┐ ┌────────────┐ │
│  │ AdConfig  │ │ AdUnits  │ │ 事件回调    │ │
│  └───────────┘ └──────────┘ └────────────┘ │
└──────────────────┬──────────────────────────┘
                   │ 委托
┌──────────────────▼──────────────────────────┐
│           IAdAdapter (适配器接口)            │
├──────────┬──────────┬──────────┬────────────┤
│ WeChat   │ Douyin   │  Web     │  Mobile    │
│ Adapter  │ Adapter  │ Adapter  │  Adapter   │
└──────────┴──────────┴──────────┴────────────┘
         │         │         │         │
┌────────▼───┬─────▼────┬────▼────┬───▼────────┐
│ WeChatWASM │  TTSDK   │  JSLib  │  Native    │
│   SDK      │          │         │  Plugin    │
└────────────┴──────────┴─────────┴────────────┘
```

### 2.2 目录结构

```
Assets/MiniGame/
├── Runtime/
│   └── Ad/
│       ├── AdManager.cs                    # 广告管理器(核心入口)
│       └── Core/
│           ├── AdPlatform.cs               # 平台枚举
│           ├── AdType.cs                   # 广告类型枚举
│           ├── AdState.cs                  # 广告状态枚举
│           ├── IAdUnit.cs                  # 广告单元接口族
│           ├── IAdAdapter.cs               # 适配器接口
│           ├── AdConfig.cs                 # 配置数据
│           ├── AdPlatformDetector.cs       # 平台自动检测
│           └── AdAdapterFactory.cs         # 适配器工厂
├── Runtime/Ad/Adapter/
│   ├── EditorAdAdapter.cs                 # Editor适配器
│   ├── WeChatAdAdapter.cs                 # 微信适配器
│   ├── DouyinAdAdapter.cs                 # 抖音适配器
│   ├── WebAdAdapter.cs                    # Web适配器
│   └── MobileAdAdapter.cs                 # Android/iOS适配器
└── Editor/Ad/
    ├── AdManagerEditorWindow.cs            # Editor调试窗口
    └── AdDefineSymbols.cs                 # 宏定义管理工具
```

---

## 3. 核心接口设计

### 3.1 枚举定义

```csharp
public enum AdPlatform   // Editor, WeChatMiniGame, DouyinMiniGame, Web, Android, iOS
public enum AdType       // Banner, Interstitial, RewardedVideo, Custom
public enum AdState      // None, Loading, Loaded, Showing, Closed, Error
```

### 3.2 广告单元接口族

```csharp
// 基础接口 - 所有广告类型
public interface IAdUnit : IDisposable
{
    string AdUnitId { get; }
    AdType Type { get; }
    AdState State { get; }
    void Load();
    void Show();
    void Hide();
    event Action<IAdUnit> OnLoaded;
    event Action<IAdUnit, string> OnError;
    event Action<IAdUnit> OnClosed;
    event Action<IAdUnit> OnClicked;
}

// Banner广告 - 支持位置和尺寸设置
public interface IBannerAdUnit : IAdUnit
{
    void SetPosition(int left, int top);
    void SetSize(int width, int height);
}

// 插屏广告
public interface IInterstitialAdUnit : IAdUnit { }

// 激励视频广告 - 增加奖励回调
public interface IRewardedVideoAdUnit : IAdUnit
{
    event Action<IRewardedVideoAdUnit, bool> OnRewarded;  // bool=是否看完
}

// 自定义广告
public interface ICustomAdUnit : IAdUnit
{
    void SetPosition(int left, int top);
    void SetSize(int width, int height);
}
```

### 3.3 平台适配器接口

```csharp
public interface IAdAdapter
{
    AdPlatform Platform { get; }
    string PlatformName { get; }
    bool IsInitialized { get; }
    void Initialize();
    void Dispose();
    IAdUnit CreateAd(AdType type, string adUnitId);
    bool IsAdSupported(AdType type);
}
```

---

## 4. 平台适配器实现

### 4.1 微信小游戏 (WeChatAdAdapter)

| 广告类型 | 实现类 | SDK API |
|---------|--------|---------|
| Banner | WeChatBannerAdUnit | `WX.CreateBannerAd` |
| 插屏 | WeChatInterstitialAdUnit | `WX.CreateInterstitialAd` |
| 激励视频 | WeChatRewardedVideoAdUnit | `WX.CreateRewardedVideoAd` |
| 自定义 | WeChatCustomAdUnit | `WX.CreateCustomAd` |

- 宏控制: `#if WEIXINMINIGAME`
- 命名空间: `WeChatWASM`
- 生命周期: 实现了 `Dispose()` 中的 `Destroy()` 调用和异常保护

### 4.2 抖音小游戏 (DouyinAdAdapter)

| 广告类型 | 实现类 | SDK API |
|---------|--------|---------|
| Banner | DouyinBannerAdUnit | `TT.CreateBannerAd` |
| 插屏 | DouyinInterstitialAdUnit | `TT.CreateInterstitialAd` |
| 激励视频 | DouyinRewardedVideoAdUnit | `TT.CreateRewardedVideoAd` |
| 自定义 | DouyinCustomAdUnit | 占位实现 |

- 宏控制: `#if DOUYINMINIGAME`
- 命名空间: `TTSDK`

### 4.3 Web平台 (WebAdAdapter)

- 宏控制: `#if UNITY_WEBGL`
- 调用方式: 通过 `[DllImport("__Internal")]` 调用 `.jslib` 插件
- 需要配套 `Assets/Plugins/Web/WebAdPlugin.jslib` 文件（需自行实现Web广告SDK对接）

### 4.4 移动端 (MobileAdAdapter)

- 宏控制: `#if UNITY_ANDROID` / `#if UNITY_IOS`
- Android: 通过 `[DllImport("adplugin")]` 调用AAR/JAR原生插件
- iOS: 通过 `[DllImport("__Internal")]` 调用Framework原生插件
- 需要配套原生插件工程（需自行实现Android AdMob/穿山甲等、iOS AdMob/穿山甲等对接）

### 4.5 Editor模拟 (EditorAdAdapter)

- 宏控制: `#if UNITY_EDITOR`
- 仅输出日志，不调用任何真实SDK
- 用于开发阶段调试广告流程

---

## 5. AdManager API

### 5.1 初始化

```csharp
// 自动检测平台初始化
AdManager.Instance.Initialize();

// 指定平台初始化
AdManager.Instance.Initialize(AdPlatform.WeChatMiniGame);

// 带配置初始化
var config = new AdConfig { EnableAd = true, CurrentPlatform = AdPlatform.WeChatMiniGame };
config.SetAdUnitId(AdType.Banner, AdPlatform.WeChatMiniGame, "adunit-xxx");
AdManager.Instance.Initialize(config);
```

### 5.2 加载广告

```csharp
// 加载广告(自动从Config获取adUnitId)
var banner = AdManager.Instance.LoadAd(AdType.Banner);

// 加载广告(手动指定adUnitId)
var rewarded = AdManager.Instance.LoadAd(AdType.RewardedVideo, "adunit-xxx");
```

### 5.3 展示广告

```csharp
// 直接展示(未加载则自动加载)
AdManager.Instance.ShowAd(AdType.Banner);

// 展示激励视频并监听奖励
AdManager.Instance.ShowRewardedVideo("adunit-xxx", (isRewarded) => {
    if (isRewarded) { /* 发放奖励 */ }
});
```

### 5.4 隐藏/控制

```csharp
AdManager.Instance.HideAd(AdType.Banner);
AdManager.Instance.SetEnableAd(false);  // 全局开关
AdManager.Instance.PreloadAll();         // 预加载全部
```

### 5.5 获取广告单元(类型安全)

```csharp
var banner = AdManager.Instance.GetAdUnit<IBannerAdUnit>(AdType.Banner);
banner?.SetPosition(0, 1620);
banner?.SetSize(1080, 300);
```

---

## 6. 宏定义管理

### 6.1 Editor菜单工具

| 菜单路径 | 功能 |
|---------|------|
| `Tools/MiniGame/广告平台/启用微信小游戏广告` | 添加 `WEIXINMINIGAME` 宏 |
| `Tools/MiniGame/广告平台/禁用微信小游戏广告` | 移除 `WEIXINMINIGAME` 宏 |
| `Tools/MiniGame/广告平台/启用抖音小游戏广告` | 添加 `DOUYINMINIGAME` 宏 |
| `Tools/MiniGame/广告平台/禁用抖音小游戏广告` | 移除 `DOUYINMINIGAME` 宏 |
| `Tools/MiniGame/广告平台/查看当前宏定义` | 打印所有BuildTarget的宏定义 |
| `Tools/MiniGame/广告管理器调试` | 打开调试窗口 |

### 6.2 平台自动检测逻辑

```csharp
AdPlatformDetector.Detect():
  UNITY_EDITOR       → Editor
  WEIXINMINIGAME     → WeChatMiniGame
  DOUYINMINIGAME     → DouyinMiniGame
  UNITY_WEBGL        → Web
  UNITY_ANDROID      → Android
  UNITY_IOS          → iOS
```

---

## 7. Editor调试窗口

通过 `Tools/MiniGame/广告管理器调试` 打开，功能：

- 选择目标平台(模拟不同平台适配器)
- 配置各类型广告位ID
- 一键初始化 / 预加载
- 单独加载/展示/隐藏 Banner、插屏、激励视频
- 实时查看状态信息

---

## 8. 扩展指南

### 8.1 新增平台

1. 在 `AdPlatform` 枚举中新增平台值
2. 实现 `IAdAdapter` 接口，创建新适配器类
3. 在适配器中实现各广告类型的 `IAdUnit` 内部类
4. 在 `AdAdapterFactory._creators` 中注册平台创建函数
5. 在 `AdPlatformDetector.Detect()` 中添加宏检测分支

### 8.2 新增广告类型

1. 在 `AdType` 枚举中新增类型值
2. 在 `IAdUnit.cs` 中定义新接口(如 `ISplashAdUnit`)
3. 在各平台适配器中实现新广告类型
4. 在 `AdManager` 中添加对应的快捷方法

### 8.3 对接原生插件

**Android:**
1. 创建Android Library工程，实现AAR包
2. 命名JNI方法与 `MobileAdAdapter` 中的 `[DllImport("adplugin")]` 对应
3. 将AAR放入 `Assets/Plugins/Android/`

**iOS:**
1. 创建iOS Framework，实现C接口
2. 方法名与 `[DllImport("__Internal")]` 对应
3. 将Framework放入 `Assets/Plugins/iOS/`

**Web:**
1. 创建 `.jslib` 文件，实现JS函数
2. 方法名与 `[DllImport("__Internal")]` 对应
3. 将jslib放入 `Assets/Plugins/Web/`

---

## 9. 线程安全与生命周期

- `AdManager` 继承 `MonoBehaviour`，使用 `DontDestroyOnLoad` 保证场景切换不销毁
- 广告单元缓存使用 `Dictionary<string, IAdUnit>`，键为 `{AdType}_{AdUnitId}`
- `OnDestroy` 中遍历释放所有广告单元并调用适配器 `Dispose()`
- 所有平台适配器的广告单元均实现 `Dispose()` 模式，包含 try-catch 异常保护
- 微信广告特别处理了 `isDestroyed` 标记，防止异步回调时操作已销毁对象

---

## 10. 与现有 MiniGameKit 的兼容

现有 `MiniGameKit.cs` 中的广告代码可以逐步迁移至 `AdManager`：

```csharp
// 旧代码 (MiniGameKit)
MiniGameKit.Instance.CreateRewardedVideoAd(adId, onSuccess);

// 新代码 (AdManager)
AdManager.Instance.ShowRewardedVideo(adId, (isRewarded) => {
    if (isRewarded) onSuccess("");
});
```

建议迁移完成后，将 `MiniGameKit` 中的广告方法标记为 `[Obsolete]`，最终移除。
