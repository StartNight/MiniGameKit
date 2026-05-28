# 运行时 API

## MiniGameKit

`MiniGameKit` 继承 `Singleton<MiniGameKit>`，是面向业务的**平台能力门面**。广告相关方法统一委托给 `AdManager`。

### 生命周期事件

```csharp
public static event Action OnMiniGameShow;
public static event Action OnMiniGameHide;
```

微信/抖音在 `Start` 中注册 `WX.OnShow` / `TT.OnShow` 等并转发到上述事件。旧 API `WXOnShow()` 已废弃，请订阅事件。

### 广告（委托 AdManager）

| 方法 | 说明 |
|------|------|
| `ShowInterstitialAd(string adId)` | 展示插屏 |
| `ShowRewardedVideo(string adId, Action<bool> onRewardResult)` | 激励视频；`true` 表示完整观看 |
| `CreateBannerAd(adId, left, top, width, height)` | 创建 Banner 并设置位置/尺寸后加载 |
| `BannerAdShow()` / `BannerAdHide()` | 显示/隐藏当前 Banner |
| `ShowCustomAd()` | 自定义广告 |

**已废弃**（内部转调 `ShowRewardedVideo`）：

- `ShowRewardedVideoAd`
- `CreateRewardedVideoAd`

**v2.1 行为**：激励视频**不会**修改 `Time.timeScale`；暂停游戏请在回调中自行处理。

### 分享与微信能力

| 方法 | 平台 | 说明 |
|------|------|------|
| `ShareApp(title, query)` | 微信/抖音 | `query` 默认 `"key1=val1&key2=val2"`，用于渠道参数 |
| `OpenCustomerService()` | 微信 | 打开客服会话 |
| `OpenBusinessView(businessType, fail, success)` | 微信 | 打开业务场景，默认 `servicecommentpage` |
| `WXReportGameStart()` | 微信 | 上报游戏开始 |

抖音 `Start` 时会调用 `TT.InitSDK()`；微信会 `ShowShareMenu`。

### 振动

| 方法 | 说明 |
|------|------|
| `VibrateShort()` | 短震；微信/抖音/WebGL/原生各有实现 |
| `VibrateLong()` | 长震 |

WebGL 非 Editor 通过 `__Internal` 的 `Vibrate(ms)`；Android/iOS 使用 `Handheld.Vibrate()`。

### 使用示例

```csharp
// 分享带渠道参数
MiniGameKit.Instance.ShareApp("来一局！", "from=lobby&uid=123");

// 激励视频
MiniGameKit.Instance.ShowRewardedVideo(rewardAdId, ok =>
{
    if (ok) GrantCoins(100);
});

// 前后台
void OnEnable()
{
    MiniGameKit.OnMiniGameHide += PauseGame;
    MiniGameKit.OnMiniGameShow += ResumeGame;
}
```

---

## AdManager

完整广告 API 见 [广告系统](ad-system.md)。常用入口：

```csharp
AdManager.Instance.Initialize();
AdManager.Instance.ShowRewardedVideo(adId, isRewarded => { });
AdManager.Instance.ShowAd(AdType.Banner);
AdManager.Instance.SetEnableAd(false); // 全局关闭
```

`SetEnableAd(false)` 时，`ShowRewardedVideo` 会直接回调 `true`（便于无广告环境测试）。

---

## Singleton&lt;T&gt;

```csharp
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
```

- 首次访问 `Instance` 时 `FindFirstObjectByType`（Unity 2022+），找不到则新建 GameObject。
- `Awake` 中去重、`DontDestroyOnLoad`（可通过 `protected virtual bool DontDestroy` 关闭）。
- 子类重写 `AwakeOf()` 做初始化（`MiniGameKit` 在 `AwakeOf` 中调用 `TT.InitSDK()`）。

---

## MiniGameInit

简单 `MonoBehaviour`：在 `Start` 中于微信平台调用 `WX.ReportGameStart()`。可挂在启动场景任意物体上。

---

## SkipUnityLogo

非 Editor 构建时通过 `RuntimeInitializeOnLoadMethod(BeforeSplashScreen)` 尽快停止 Unity Splash：

- **WebGL**：监听 `Application.focusChanged` 后 `SplashScreen.Stop`
- **其它平台**：后台线程调用 `SplashScreen.Stop`

无需在场景中挂载组件。
