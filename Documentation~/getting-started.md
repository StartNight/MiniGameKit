# 快速开始

## 1. 安装

### 方式 A：Git Submodule（本仓库用法）

```ini
# .gitmodules
[submodule "Packages/MiniGameKit"]
    path = Packages/MiniGameKit
    url = https://gitee.com/Felix_Lee16/MiniGameKit.git
```

克隆主仓库后执行：

```bash
git submodule update --init --recursive
```

### 方式 B：UPM Git URL

在 `Packages/manifest.json` 中添加：

```json
"com.loveminigame.minigamekit": "https://gitee.com/Felix_Lee16/MiniGameKit.git"
```

### 方式 C：本地路径

```json
"com.loveminigame.minigamekit": "file:../MiniGameKit"
```

---

## 2. 依赖

`package.json` 声明的 UPM 依赖：

| 包 | 用途 |
|----|------|
| `com.unity.addressables` ≥ 1.22.3 | Addressables 构建、微信 Provider |
| `com.unity.textmeshpro` ≥ 3.0.9 | 字体/TMP 工具 |
| `com.unity.ugui` ≥ 1.0.0 | UI 转换工具 |

**宿主工程还需自行集成**（包内不包含）：

| 目标平台 | 通常需要的 SDK |
|----------|----------------|
| 微信小游戏 | [微信 Unity 转换插件](https://github.com/wechat-miniprogram/minigame-unity-webgl-transform)（`WeChatWASM`） |
| 抖音小游戏 | 字节 StarkSDK / TTSDK |
| I2 本地化 | I2 Localization 插件（Editor 工具通过反射调用） |

安装 Addressables 后，Editor 程序集会自动获得 `UNITY_ADDRESSABLES` 宏，微信 Provider 与相关菜单才会参与编译。

---

## 3. 平台宏定义

| 宏 | 含义 |
|----|------|
| `WEIXINMINIGAME` | 编译微信 WASM API（`WeChatWASM`） |
| `DOUYINMINIGAME` | 编译抖音 TTSDK API |
| `CRAZYGAMES` | 编译 CrazyGames SDK |

**核心机制：Platform Switcher（SDK 热插拔隔离）**

MiniGameKit 现已接入物理级别的 SDK 隔离。请**永远不要手动修改平台宏**。
- **日常开发与切换**：在 Unity Editor 右上角工具栏的 **Platform Switcher** 下拉框中选择目标平台（如“微信小游戏”、“纯净 WebGL”、“Android”等）。
- 工具会自动将其他平台的不相关 SDK 移动到根目录的 `SDKs/` 存档文件夹中，避免同级编译冲突，同时自动修改 Build Target 和 Scripting Define Symbols。
- **正式构建**：切换平台后，打开 `Tools/Minigame/构建/`，菜单会自动变为对应当前平台的构建选项。

---

## 4. 场景接入

### 4.1 放置 `MiniGameKit`

在首个场景（或启动场景）中放置带 `MiniGameKit` 组件的 GameObject，或依赖 `Singleton<T>` 在首次访问时自动创建。

```csharp
// 任意处首次访问即创建单例
MiniGameKit.Instance.ShareApp("分享标题", "channel=1");

// 订阅前后台
MiniGameKit.OnMiniGameShow += OnShow;
MiniGameKit.OnMiniGameHide += OnHide;
```

### 4.2 广告初始化

在合适时机（如进入主界面后）初始化广告：

```csharp
void Start()
{
    var config = new AdConfig { EnableAd = true };
    config.SetAdUnitId(AdType.RewardedVideo, AdPlatform.WeChatMiniGame, "adunit-xxxx");
    AdManager.Instance.Initialize(config);
}
```

也可仅调用 `AdManager.Instance.Initialize()`，由 `AdPlatformDetector` 根据当前宏与平台自动选择适配器。

### 4.3 激励视频（推荐 API）

```csharp
MiniGameKit.Instance.ShowRewardedVideo("adunit-xxxx", isRewarded =>
{
    if (isRewarded)
    {
        // 发放奖励
    }
});
```

> Editor 下若使用 `EditorAdAdapter`，通常会立即回调成功，便于联调。

### 4.4 可选：跳过 Unity Logo

将 `SkipUnityLogo` 挂到任意场景或通过代码触发其 `RuntimeInitializeOnLoadMethod`（类已自带入口，无需挂脚本）。仅非 Editor 构建生效。

### 4.5 可选：微信上报开局

场景中添加 `MiniGameInit` 组件，会在 `Start` 时调用 `WX.ReportGameStart()`（仅 `WEIXINMINIGAME`）。

---

## 5. Editor 路径配置

打开 **`Tools/Minigame/项目设置`**，配置：

- I2 CSV 与 LanguageSource 资源路径
- 字体扫描目录、TTF 输出目录
- Android Keystore（存于本机 EditorPrefs，**勿提交密码**）

默认值见 `MiniGameKitEditorPaths`（如 `Assets/Localization/I2UITable.csv`）。

---

## 6. 第一次构建（微信小游戏）

1. 通过 Editor 右上角的 **Platform Switcher** 下拉框切换到“微信小游戏”平台（自动隔离其他 SDK 并配置宏）。
2. 安装并配置微信 Unity 转换插件（导出路径等）。
3. `Tools/Minigame/构建/Addressables/切换为微信并构建内容`（或一键性能护航，见 [构建管线](build-pipeline.md)）。
4. `Tools/Minigame/构建/构建当前平台 (微信小游戏)` — 等价于插件面板 **「生成并转换」**。

---

## 7. 下一步

- [运行时 API](runtime-api.md) — `MiniGameKit` 全部公开方法
- [广告系统](ad-system.md) — `AdManager` 与适配器
- [构建管线](build-pipeline.md) — CI 与 `BuildConfig`
- [Editor 工具](editor-tools.md) — 完整菜单表
