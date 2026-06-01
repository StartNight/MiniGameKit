# 构建管线

构建逻辑集中在 `MiniGameKit.Editor` 命名空间，由 **`MiniGameBuildPipeline`** 统一实现；Editor 窗口与菜单、CI 均复用同一套代码。

---

## 1. 支持的平台

| `BuildPlatform` | BuildTarget | 专属宏（构建时临时写入） |
|-----------------|-------------|-------------------------|
| `WeChatMiniGame` | WebGL | `WEIXINMINIGAME` |
| `DouyinMiniGame` | WebGL | `DOUYINMINIGAME` |
| `CrazyGames` | WebGL | `CRAZYGAMES` |
| `WebGL` | WebGL | （无平台专属宏） |
| `Android` | Android | （无平台专属宏） |
| `iOS` | iOS | （无平台专属宏） |

构建结束会调用 `RestoreScriptingDefines` **还原**构建前的宏列表。

---

## 2. BuildConfig

```csharp
var config = BuildConfig.Create(BuildPlatform.WeChatMiniGame);
config.BuildAddressables = true;
config.UseWeChatProvider = true;      // 微信默认为 true
config.ApplyWebGLOptimizations = true;
config.SwitchBuildTarget = true;
config.DevelopmentBuild = false;
config.AutoRun = false;
config.OutputPath = "";               // 空则用 DefaultOutputDir
```

默认输出目录（相对项目根）：

| 平台 | 目录 |
|------|------|
| 微信 | `build/WeChatMiniGame` |
| 抖音 | `build/DouyinMiniGame` |
| WebGL | `build/WebGL` |
| Android | `build/Android/{ProductName}.apk` |
| iOS | `build/iOS` |

> **微信注意**：最终导出路径由**微信插件配置**中的「导出路径」决定，菜单里的 `OutputPath` 对微信「生成并转换」不生效。

---

## 3. 通用构建流程（非微信）

`MiniGameBuildPipeline.Run(config)`：

1. 写入/还原平台 Scripting Define Symbols  
2. （可选）切换 Active Build Target  
3. （可选）切换 Addressables Provider 并 `BuildPlayerContent`  
4. （WebGL）`WebGLCiBuild.ApplyReleaseSizeOptimizations()`  
5. `BuildPipeline.BuildPlayer`  
6. 还原宏定义  

事件：

- `OnLog` — 日志字符串  
- `OnProgress` — `(progress, message)`  

---

## 4. 微信小游戏构建

`MiniGameBuildPipeline.RunWeChatBuild(config)`：

1. 临时添加 `WEIXINMINIGAME`  
2. （可选）WebGL 体积优化（关 Debug Symbols、Brotli、IL2CPP Release 等）  
3. 切换 Addressables 为 **微信 Provider** 并构建内容  
4. 调用 `WeChatWASM.WXConvertCore.DoExport(buildWebGL: true)`（等同插件 **「生成并转换」**）  
5. 还原宏  

菜单入口：`Tools/Minigame/构建/构建当前平台 (微信小游戏)`  
结果弹窗：`ShowWeChatBuildResult(bool)`，成功时显示插件配置的 `DST` 路径。

---

## 5. Editor 菜单速查

由于引入了 SDK 物理热插拔隔离，构建菜单会**根据当前激活的平台动态显示**，防止跨平台交叉构建。

| 菜单 (当前平台下可见) | 说明 |
|------|------|
| `Tools/Minigame/构建/构建窗口` | `MiniGameBuildWindow` 图形界面 |
| `Tools/Minigame/构建/构建当前平台 (微信小游戏)` | `RunWeChatBuild` (仅在切换至微信平台后可见) |
| `Tools/Minigame/构建/构建当前平台 (抖音小游戏)` | 抖音构建逻辑 (仅在切换至抖音平台后可见) |
| `Tools/Minigame/构建/构建当前平台 (CrazyGames - WebGL)` | CrazyGames 构建 |
| `Tools/Minigame/构建/构建当前平台 (Android)` | APK 构建 |
| `Tools/Minigame/构建/构建并运行当前平台 (Android)` | APK + AutoRun |
| `Tools/Minigame/构建/构建当前平台 (iOS)` | Xcode 工程目录构建 |
| `Tools/Minigame/构建/诊断当前构建环境` | 平台、场景数、Provider 状态 |
| `Tools/Minigame/构建/一键性能护航构建 (微信小游戏)` | 见下文 |

### 一键性能护航（微信）

`OneClickBuildTool` 顺序执行：

1. 光照贴图加入 Addressables  
2. 批量关闭 UI 贴图 MipMaps  
3. 剥离 TrueShadow（项目相关，默认扫描 `Assets/Prefabs/Rooms`）  
4. MeshCollider → BoxCollider 合规修复  
5. 切换微信 Provider 并构建 Addressables  

---

## 6. WebGL 优化项

`WebGLCiBuild.ApplyReleaseSizeOptimizations()` 主要设置：

- 关闭 WebGL Debug Symbols  
- 模板 `PROJECT:WYMinigame2022`（可按项目修改）  
- Brotli 压缩、Wasm Linker  
- 自定义 `emscriptenArgs`（导出 `_main` 等）  
- IL2CPP Release + High Stripping + OptimizeSize（2022+）  

`ApplyLinkerSafeWebGLSettings()` 为较轻量子集，供其它脚本调用。

---

## 7. CI / 命令行

推荐统一入口 **`MiniGameKit.Editor.CiBuild`**（GitHub Actions 使用）：

| 平台 | executeMethod |
|------|----------------|
| WebGL H5 | `MiniGameKit.Editor.CiBuild.BuildWebGL` |
| 微信小游戏 | `MiniGameKit.Editor.CiBuild.BuildWeChat` |
| 抖音小游戏 | `MiniGameKit.Editor.CiBuild.BuildDouyin` |
| Android | `MiniGameKit.Editor.CiBuild.BuildAndroid` |
| iOS | `MiniGameKit.Editor.CiBuild.BuildIOS` |

公共参数：

```text
-projectPath <项目根>
-buildTarget WebGL|Android|iOS
-customBuildPath <输出目录>
```

### 仅 Addressables（微信 Provider）

```text
-executeMethod MiniGameKit.Editor.AddressablesWeChatBuildMenu.BatchWeChat
-executeMethod MiniGameKit.Editor.AddressablesWeChatBuildMenu.BatchDefault
```

兼容旧入口：`MiniGameKit.Editor.WebGLCiBuild.Build` / `BuildWeChat`。

退出码：`CiBuild` / `WebGLCiBuild` 成功 `0`，失败 `101+`；Addressables Batch 失败 `200`/`201`。

---

## 8. 微信加载页补丁

`Tools/Minigame/构建/微信小游戏/应用加载页配置` — `WeChatLoadingPagePatchTool`，按项目需求 patch 微信加载页资源（详见工具源码注释）。

---

## 9. 故障排查

| 现象 | 建议 |
|------|------|
| 微信构建无 `WXConvertCore` | 确认已安装微信 Unity 转换插件 |
| Addressables 菜单灰色 | 安装 `com.unity.addressables` 并等待 asmdef 刷新 |
| 构建后宏未还原 | 查看 Console 是否在 `BuildPlayer` 前异常退出 |
| emcc 内存错误 | 使用菜单构建（会自动 `ApplyReleaseSizeOptimizations` 关调试符号） |
