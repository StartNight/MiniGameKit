# 自动构建工作流规范 (AutoBuild Specification)

本文档描述了 MiniGameKit 集成的 GitHub Actions 自动构建方案的标准工作流和使用规范。

## 1. 触发机制设计原则

在工业级游戏开发中，构建打包通常是一项极其耗时且消耗服务器资源的任务。因此，我们**不推荐**在主分支的每一次 `push` 上触发全量打包。相反，我们采用以下两种主要触发机制：

### 1.1 临时测试包 / 联调打包 (API 触发)
**适用场景**：日常开发测试、给 QA 打一个非正式验证包。
- **不留痕迹**：通过 GitHub Actions 的 `workflow_dispatch` API 触发，**完全不污染 Git 提交历史**。
- **操作方式**：
  1. 打开 Unity 顶部菜单：`Tools -> 自动构建 -> 构建配置面板`。
  2. 填写正确的目标 GitHub Repo 和 Personal Access Token (PAT)。
  3. 选择目标平台，点击 **"API 触发"** 按钮。

### 1.2 正式发布包 / 里程碑版本 (Tag 触发)
**适用场景**：向测试部门提审、向运营交付正式版本、里程碑归档。
- **精准溯源**：通过打下 Git Tag 触发，Tag 相当于当前代码的一个快照书签，方便未来追溯打包时确切的代码版本，且不产生垃圾 commit 记录。
- **操作方式**：
  1. 打开 Unity 顶部菜单：`Tools -> 自动构建 -> 构建配置面板`。
  2. 选择目标平台，点击 **"创建 Tag 触发"** 按钮。
  3. 脚本会自动在本地打下一个类似 `Build_WeChat_20260602_153000` 的 Tag，并推送到远端从而触发 Actions。

---

## 2. GitHub Actions 工作流解析

我们的自动打包管道位于项目根目录的 `.github/workflows/minigame-build.yml`：

- **监听事件 (`on`)**：
  - `workflow_dispatch`: 接受来自编辑器面板发出的带参数 API 调用的打包请求。
  - `push.tags`: 监听格式为 `Build_*` 的轻量级或附注标签推送。

- **构建阶段 (`jobs.build`)**：
  - **环境解析**：脚本会通过传入的输入参数或 Tag 名称 (`github.ref_name`) 自动解析出打包的平台枚举变量 (PLATFORM)。
  - **检出源码**：拉取包含 Submodule 的主仓库源码。
  - **云端构建**：使用 `game-ci/unity-builder` 容器启动无头 Unity，并运行 `MiniGameKit.Editor.CiBuild.BuildFromAction` 入口点执行编译。
  - **产物归档**：构建结束后，生成的 WebGL/Android 等产物会立刻被拷贝进对应的产物存放子模块目录中（如 `Builds/WeChatMiniGame`）。
  - **产物推送**：机器人账自动提交产物子模块的变化并 Push 到子仓库的 master 分支，完成闭环发布。

---

## 3. 常见问题排查 (Troubleshooting)

- **点击 API 触发报错无权限 / Not Found**：
  检查编辑器面板中填写的 GitHub PAT 是否过期，且是否至少包含 `repo` 和 `workflow` 权限。
  
- **构建工作流中 Unity 报错 License 无效**：
  检查项目的 GitHub Secrets 中 `UNITY_LICENSE` 的 `.ulf` 内容是否完整，或检查你当前的 Unity 大版本是否匹配此 License。

- **构建产物未能推送到子仓库**：
  如果在 **Commit and Push to Submodule** 阶段失败，可能是 `PAT_FOR_SUBMODULE` 环境变量没有配置到 GitHub 的 Repository Secrets 里。请前往仓库 Settings 补充具有目标 Submodule 写入权限的 PAT。
