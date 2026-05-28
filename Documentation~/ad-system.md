# 广告系统

> 源码设计稿：`Runtime/Ad/AdSystem-Design.md`（与本文档同步维护）

## 1. 概述

多平台广告管理采用 **适配器模式 + 工厂模式**，业务层通过 `AdManager` 或 `MiniGameKit` 调用，不直接依赖各平台 SDK。

| 平台 | 宏定义 | SDK |
|------|--------|-----|
| 微信小游戏 | `WEIXINMINIGAME` | WeChatWASM |
| 抖音小游戏 | `DOUYINMINIGAME` | TTSDK |
| Web (H5) | `UNITY_WEBGL` | jslib（需自备 `WebAdPlugin.jslib`） |
| Android | `UNITY_ANDROID` | 原生插件 `adplugin` |
| iOS | `UNITY_IOS` | `__Internal` 原生接口 |
| Editor | `UNITY_EDITOR` | `EditorAdAdapter`（日志模拟） |

支持类型：`Banner`、`Interstitial`、`RewardedVideo`、`Custom`。

---

## 2. 架构

```
业务 (UI / GameControl)
        │
        ▼
  MiniGameKit ──委托──► AdManager (单例 MonoBehaviour)
        │                    │
        │                    ▼
        │              IAdAdapter (工厂创建)
        │         ┌──────┼──────┬─────────┐
        │    WeChat Douyin  Web   Mobile/Editor
        ▼
   各平台 WX.* / TT.* / DllImport / 模拟
```

### 目录

```
Runtime/Ad/
├── AdManager.cs
├── Core/
│   ├── AdPlatform.cs, AdType.cs, AdState.cs
│   ├── IAdUnit.cs, IAdAdapter.cs
│   ├── AdConfig.cs
│   ├── AdPlatformDetector.cs
│   └── AdAdapterFactory.cs
└── Adapter/
    ├── EditorAdAdapter.cs
    ├── WeChatAdAdapter.cs
    ├── DouyinAdAdapter.cs
    ├── WebAdAdapter.cs
    └── MobileAdAdapter.cs
```

---

## 3. 核心类型

### 枚举

- `AdPlatform`：`Editor`, `WeChatMiniGame`, `DouyinMiniGame`, `Web`, `Android`, `iOS`
- `AdType`：`Banner`, `Interstitial`, `RewardedVideo`, `Custom`
- `AdState`：`None`, `Loading`, `Loaded`, `Showing`, `Closed`, `Error`

### IAdUnit 接口族

- `IAdUnit`：基础 `Load` / `Show` / `Hide` / 事件 / `IDisposable`
- `IBannerAdUnit`：`SetPosition`, `SetSize`
- `IRewardedVideoAdUnit`：`OnRewarded(unit, bool isEnded)`
- `ICustomAdUnit`：位置与尺寸

### IAdAdapter

每个平台实现 `CreateAd(AdType, adUnitId)`、`IsAdSupported`、`Initialize`、`Dispose`。

---

## 4. AdManager API

### 初始化

```csharp
// 自动检测平台
AdManager.Instance.Initialize();

// 指定平台
AdManager.Instance.Initialize(AdPlatform.WeChatMiniGame);

// 带配置
var config = new AdConfig { EnableAd = true };
config.SetAdUnitId(AdType.Banner, AdPlatform.WeChatMiniGame, "adunit-xxx");
AdManager.Instance.Initialize(config);
```

### 加载与展示

```csharp
var banner = AdManager.Instance.LoadAd(AdType.Banner, "adunit-banner");
AdManager.Instance.ShowAd(AdType.Interstitial, "adunit-inter");

AdManager.Instance.ShowRewardedVideo("adunit-reward", isRewarded =>
{
    if (isRewarded) { /* 发奖 */ }
});

AdManager.Instance.HideAd(AdType.Banner);
```

### 其它

```csharp
AdManager.Instance.SetEnableAd(false);
AdManager.Instance.PreloadAll();

var banner = AdManager.Instance.GetAdUnit<IBannerAdUnit>(AdType.Banner);
banner?.SetPosition(0, 1620);
banner?.SetSize(1080, 300);

bool ready = AdManager.Instance.IsAdLoaded(AdType.RewardedVideo);
```

### 缓存键

广告实例缓存键为 `{AdType}_{adUnitId}`。重复 `LoadAd` 且已 `Loaded`/`Showing` 时返回已有实例。

---

## 5. 平台检测

`AdPlatformDetector.Detect()` 优先级（简化）：

1. `UNITY_EDITOR` → Editor  
2. `WEIXINMINIGAME` → WeChatMiniGame  
3. `DOUYINMINIGAME` → DouyinMiniGame  
4. `UNITY_WEBGL` → Web  
5. `UNITY_ANDROID` / `UNITY_IOS` → 对应移动端  

---

## 6. Editor 工具

| 菜单 | 功能 |
|------|------|
| `Tools/Minigame/广告/平台/启用\|禁用微信小游戏广告` | 增删 `WEIXINMINIGAME` |
| `Tools/Minigame/广告/平台/启用\|禁用抖音小游戏广告` | 增删 `DOUYINMINIGAME` |
| `Tools/Minigame/广告/平台/查看当前宏定义` | 打印各 BuildTargetGroup 宏 |
| `Tools/Minigame/广告/广告管理器调试` | Play Mode 下测试加载/展示 |

宏会写入 Standalone / WebGL / Android / iOS 等常用 Target Group。

---

## 7. 扩展指南

### 新增平台

1. `AdPlatform` 增加枚举值  
2. 实现 `IAdAdapter` 及各 `IAdUnit` 内部类  
3. `AdAdapterFactory` 注册创建函数  
4. `AdPlatformDetector.Detect()` 增加分支  
5. 如需构建宏，在 `MiniGameBuildPipeline.PlatformDefines` 中登记  

### 新增广告类型

1. `AdType` 与 `IAdUnit` 子接口  
2. 各 `*AdAdapter` 实现 `CreateAd` 分支  
3. `AdManager` 增加便捷方法（可选）  

### 原生 / Web 插件

- **Android**：`[DllImport("adplugin")]`，AAR 放 `Assets/Plugins/Android/`  
- **iOS**：`[DllImport("__Internal")]`，Framework 放 `Assets/Plugins/iOS/`  
- **Web**：`.jslib` 放 `Assets/Plugins/Web/`，函数名与 `WebAdAdapter` 一致  

---

## 8. 生命周期与安全

- `AdManager` 使用 `DontDestroyOnLoad`；`OnDestroy` 释放全部 `IAdUnit` 与适配器。  
- 微信等适配器在异步回调中检查销毁标记，避免操作已释放对象。  
- 与 `MiniGameKit` 关系：新代码优先 `ShowRewardedVideo`；旧 `MiniGameKit` 废弃方法仅作兼容包装。
