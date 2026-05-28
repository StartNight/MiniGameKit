# MiniGameKit 文档

> 包名：`com.loveminigame.minigamekit` | Unity 2022.3+ | 作者 Felix/李康康

MiniGameKit 是面向微信、抖音等小游戏平台的 Unity UPM 组件库，提供**多平台广告管理**、**平台 API 封装**、**多平台构建管线**与**常用 Editor 工具**。

---

## 文档目录

| 文档 | 说明 |
|------|------|
| [快速开始](getting-started.md) | 安装、依赖、宏定义、场景接入 |
| [运行时 API](runtime-api.md) | `MiniGameKit`、`Singleton`、生命周期与平台能力 |
| [广告系统](ad-system.md) | `AdManager` 架构、API、适配器与扩展 |
| [构建管线](build-pipeline.md) | 多平台构建、微信「生成并转换」、CI 入口 |
| [**多端打包架构方案（草案）**](build-architecture-proposal.md) | 微信/抖音/WebGL/Android/iOS 架构设计，**待评审** |
| [Addressables 微信 Provider](addressables-wechat.md) | `WXAssetBundleProvider` 与菜单/CI |
| [Editor 工具](editor-tools.md) | 全部 `Tools/Minigame/` 菜单说明 |
| [GitHub Actions CI](ci-github-actions.md) | 自动构建与 `OutbreakBowling-build` 产物同步 |

---

## 包结构概览

```
Packages/MiniGameKit/
├── Runtime/                    # MiniGameKit.Runtime
│   ├── MiniGameKit.cs          # 平台 API 门面（广告委托 AdManager）
│   ├── Utils/                  # Singleton、MiniGameInit、SkipUnityLogo
│   ├── Ad/                     # 广告系统（见 ad-system.md）
│   └── Addressables/           # 微信 AB Provider（独立 asmdef）
├── Editor/                     # MiniGameKit.Editor（仅 Editor）
│   ├── Core/                   # 路径配置、I2 反射工具、文件头处理
│   ├── Ad/                     # 宏定义、广告调试窗口
│   ├── Build/                  # 构建管线、WebGL CI、优化工具
│   ├── Localization/           # I2 CSV 导入、Localize 自动绑定
│   ├── Font/                   # 字符集收集
│   ├── UI/                     # Text → TMP 转换
│   └── Android/                # Keystore 配置
├── Documentation~/             # 本目录（不随包体发布）
├── README.md                   # 功能总览
└── package.json
```

---

## 程序集与命名空间

| 程序集 | 根命名空间 | 说明 |
|--------|------------|------|
| `MiniGameKit.Runtime` | （全局，无根命名空间） | 运行时；`MiniGameKit`、`AdManager` 等 |
| `MiniGameKit.Runtime.Addressables` | `UnityEngine.ResourceManagement` 等 | 可选；需 `com.unity.addressables` |
| `MiniGameKit.Editor` | **`MiniGameKit.Editor`** | Editor 工具、构建管线、菜单 |

Editor 配置项 EditorPrefs 键前缀：`MiniGameKit.`（见 `MiniGameKitEditorPaths`）。

CI 示例：`MiniGameKit.Editor.CiBuild.BuildWeChat`、`MiniGameKit.Editor.AddressablesWeChatBuildMenu.BatchWeChat`。

---

## 设计原则（摘要）

- **适配器 + 工厂**：广告按平台实现 `IAdAdapter`，业务只依赖 `AdManager` / `MiniGameKit`。
- **宏隔离**：`WEIXINMINIGAME`、`DOUYINMINIGAME` 等通过 `#if` 编译期剔除未用平台代码。
- **构建期宏还原**：`MiniGameBuildPipeline` 在构建前后自动写入/还原平台宏，避免污染日常 Editor 环境。
- **激励视频与时间解耦**：v2.1 起不修改 `Time.timeScale`，由游戏业务自行处理暂停逻辑。

更详细的设计说明见各专题文档与 `Runtime/Ad/AdSystem-Design.md`（源码内设计稿）。
