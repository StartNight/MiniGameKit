# Addressables 微信 Provider

## 1. 作用

微信小游戏环境下，Unity 默认的 `AssetBundleProvider` 无法直接使用微信 CDN / 缓存能力。MiniGameKit 提供：

- **`WXAssetBundleProvider`** — 替代 `AssetBundleProvider`  
- **`WXBundledAssetProvider`** — 替代 `BundledAssetProvider`  

实现位于 `Runtime/Addressables/WXAssetBundleProvider.cs`，编译条件：

- 包依赖：`com.unity.addressables`（`UNITY_ADDRESSABLES`）  
- 运行时引用 `WeChatWASM`（微信 SDK）  

程序集：`MiniGameKit.Runtime.Addressables`（与主 Runtime 分离，避免未装 Addressables 时编译失败）。

---

## 2. 切换 Provider

Editor 菜单根路径：`Tools/Minigame/构建/Addressables/`

| 菜单 | 行为 |
|------|------|
| 切换到微信 Provider | 所有 Bundled 分组使用 WX 系列 Provider |
| 切换到 Unity 默认 Provider | 恢复 `AssetBundleProvider` + `BundledAssetProvider` |
| 诊断 Provider 状态 | Console 输出各 Group 当前 Provider 类型 |
| 构建内容（不切换 Provider） | 仅 `BuildPlayerContent` |
| 切换为微信并构建内容 | 切换 + 构建 |
| 切换为默认并构建内容 | 切换 + 构建 |

菜单项带勾选状态：当前生效的 Provider 方案会显示 checked。

**说明**：Localization 等只读分组仍会尝试切换其 `BundledAssetGroupSchema` 上的 Bundled Asset Provider。

---

## 3. 与构建管线集成

- `MiniGameBuildPipeline` 在 `BuildAddressables == true` 时调用 `AddressablesWeChatBuildMenu.ApplyProviders` 与 `BuildAddressablesContent()`。  
- 微信平台构建强制 `UseWeChatProvider = true`。  
- `MiniGameBuildWindow` 中微信平台会禁用「使用微信 Provider」开关（始终为 true），并提示完整构建走 `RunWeChatBuild`。

---

## 4. CI

```bash
Unity -batchmode -quit -projectPath "<path>" \
  -executeMethod MiniGameKit.Editor.AddressablesWeChatBuildMenu.BatchWeChat
```

成功 `EditorApplication.Exit(0)`；Apply 失败 `200`，构建失败 `201`。

默认 Provider 批处理：`BatchDefault`。

---

## 5. 实现要点（阅读源码时）

`WXAssetBundleProvider` 基于 Unity ResourceManager 的 Provider 模式，包含：

- 微信环境下的 `UnityWebRequest` 队列（`WebRequestQueue` / `WXWebRequestQueueOperation`）  
- 与 `WXUnityWebRequestUtilities` 协作判断 AssetBundle 是否下载完成  
- 兼容 `UNLOAD_BUNDLE_ASYNC`（Unity 2022.1+）  

文件较长（约 1300+ 行），修改前建议先运行 **诊断 Provider 状态** 确认当前 Group 配置。

---

## 6. 依赖检查清单

- [ ] `manifest.json` 含 `com.unity.addressables`  
- [ ] 已配置 Addressables Settings 与至少一个 Bundled Group  
- [ ] 微信 SDK / 转换插件已导入（Provider 内引用 `WeChatWASM`）  
- [ ] 构建微信包前执行「切换为微信并构建内容」或完整微信构建菜单  
