# Editor 工具

所有菜单挂在 **`Tools/Minigame/`** 下。路径常量定义于 `MiniGameKit.Editor.MiniGameKitEditorPaths`，可通过 **`Tools/Minigame/项目设置`** 覆盖部分默认值（EditorPrefs，键前缀 `MiniGameKit.`）。

---

## 项目设置

| 菜单 | 说明 |
|------|------|
| `Tools/Minigame/项目设置` | 本地化 / 字体 / Android 签名三页签 |

### 可配置项

| 键 | 默认值 | 用途 |
|----|--------|------|
| I2 CSV | `Assets/Localization/I2UITable.csv` | CSV 导入源 |
| LanguageSource Asset | `Assets/Resources/I2Languages.asset` | I2 语言源 |
| 静态 TTF | `Assets/Fonts/simhei.ttf` | 字符写入目标字体 |
| 字符集输出目录 | `Assets/Fonts/FontSubsetPack` | 导出 txt / TMP 子集 |
| 本地化扫描目录 | `Assets/Localization` | 收集文案字符 |
| Prefab 扫描根 | `Assets` | TMP 分字体导出 |
| 脚本扫描目录 | `Assets/Scripts`（分号分隔多个） | C# 字符串字面量扫描 |
| Android Keystore | 空（可回退 `key/user.keystore`） | 签名路径与密码 |

---

## 本地化

| 菜单 | 类 | 说明 |
|------|-----|------|
| `Tools/Minigame/本地化/导入 I2 CSV` | `I2LocalizationCsvImporter` | 反射调用 I2，将 CSV 写入 LanguageSource；失败写 `I2ImportError.txt` |
| `Tools/Minigame/本地化/自动绑定 Localize 组件` | `I2LocalizeAutoBindWindow` | 扫描场景/Prefab 文本，按内容匹配 I2 词条并挂 `Localize` |

**依赖**：项目需安装 I2 Localization（未安装会弹窗提示）。

---

## UI

| 菜单 | 类 | 说明 |
|------|-----|------|
| `Tools/Minigame/UI/Text 转 TextMeshPro` | `TextToTextMeshProConverterWindow` | 批量将 Prefab 内 `UnityEngine.UI.Text` 换为 `TextMeshProUGUI`，尽量保留布局与引用 |

---

## 字体

| 菜单 | 类 | 说明 |
|------|-----|------|
| `Tools/Minigame/字体/收集全项目字符并写入 TTF` | `FontCharacterCollector` | 扫描脚本、本地化、Prefab 文本 → 去重 → 写入配置的 TTF + `字符.txt` |
| `Tools/Minigame/字体/按 TMP 字体导出字符集` | `FontCharacterCollector` | 按 Prefab 中 `TMP_Text.font` 分组导出字符集；**合并** `Assets/Localization` 下 CSV 与 `I2Languages.asset` 中**全部语言**译文 |

---

## 广告

| 菜单 | 类 | 说明 |
|------|-----|------|
| `Editor 工具栏右上角 / Platform Switcher` | `PlatformSwitchTool` | 下拉切换平台：微信 / 抖音 BGDT 通过增删 `Packages/manifest.json` 的 UPM 依赖（微信 `com.qq.weixin.minigame`；抖音 `com.bytedance.bgdt`，默认 `https://github.com/StartNight/com.bytedance.bgdt.git#v3.0.271`）。`Assets/Plugins/ByteGame` 下 StarkSDK 等非 UPM 内容仍可物理归档；CrazyGames 仍走 Active↔Archive。抖音仅在 UPM 不可用且无本地备份时，才 interactive 导入 MiniGameKit 内置 `.unitypackage`（离线兜底）。同步 Build Target 与宏。 |
| `Tools/Minigame/广告/广告管理器调试` | `AdManagerEditorWindow` | Play Mode 下测试 Banner/插屏/激励视频 |

---

## 构建

详见 [构建管线](build-pipeline.md) 与 [Addressables 微信 Provider](addressables-wechat.md)。

| 菜单 | 说明 |
|------|------|
| `构建/构建窗口` | 图形化 `BuildConfig` |
| `构建/构建当前平台 (...)` | 根据 Platform Switcher 激活的平台动态显示本地构建选项 |
| `构建/构建并运行当前平台 (Android)` | APK + 运行 (仅限 Android 平台可见) |
| `构建/诊断当前构建环境` | 环境诊断 |
| `构建/一键性能护航构建 (微信小游戏)` | 优化 + Addressables |
| `构建/优化/剥离 TrueShadow...` | UI 性能 |
| `构建/优化/批量关闭 UI 贴图 MipMaps` | 微信包体 |
| `构建/光照/将所有烘焙贴图加入 Addressables` | 光照贴图进组 |
| `构建/Addressables/*` | Provider 切换与构建 |

---

## Android

| 菜单 | 类 | 说明 |
|------|-----|------|
| `Tools/Minigame/Android/应用签名配置` | `AndroidKeystoreConfigurator` | 打开项目设置 Android 页 |
| `Tools/Minigame/Android/立即应用签名配置` | | 写入 `PlayerSettings.Android.*` |

`InitializeOnLoad` 会在 Editor 启动时自动 `ApplyIfConfigured()`。

---

## 其它 Editor 脚本

| 脚本 | 说明 |
|------|------|
| `ScriptHeaderProcessor` | 新 C# 文件自动插入标准文件头（公司/作者/版本等） |
| `CsvLineParser` | I2 CSV 行解析 |
| `ReflectionTypeUtility` | 按类型名反射查找 I2 等第三方类型 |
| `WebGLEmscriptenArgsPreprocessor` | WebGL 构建前处理 Emscripten 参数 |

---

## 文件头规范

新脚本默认头格式（与 README 一致）：

```csharp
/****************************************************
 * FileName:        {文件名}
 * CompanyName:     苏州微游科技有限公司
 * Author:          {作者}
 * ...
*****************************************************/
```

由 `ScriptHeaderProcessor` 在创建 `.cs` 时注入。
