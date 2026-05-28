# GitHub Actions CI

## 概述

工作流：`.github/workflows/build-unity.yml`

- 使用 [game-ci/unity-builder](https://github.com/game-ci/unity-builder) 调用 `MiniGameKit.Editor.CiBuild.*`
- 构建完成后写入 `build/ci/manifest.json`
- 脚本 `scripts/ci/sync-build-repo.sh` 将产物同步到 **`OutbreakBowling-build`** 仓库（`{productName}-build`）

## 手动触发

GitHub → Actions → **Unity Build** → Run workflow → 选择 flavor：

| flavor | executeMethod | Runner |
|--------|---------------|--------|
| wechat | `CiBuild.BuildWeChat` | ubuntu |
| douyin | `CiBuild.BuildDouyin` | ubuntu |
| webgl | `CiBuild.BuildWebGL` | ubuntu |
| android | `CiBuild.BuildAndroid` | ubuntu |
| ios | `CiBuild.BuildIOS` | **macos** |

## Secrets

| Secret | 说明 |
|--------|------|
| `UNITY_LICENSE` | Unity License（game-ci 激活文件内容） |
| `UNITY_EMAIL` / `UNITY_PASSWORD` | 可选，Personal 账号激活 |
| `BUILD_REPO_TOKEN` | 写 `OutbreakBowling-build` 的 PAT（`repo` 权限）；不设则用 `GITHUB_TOKEN`（需同源 org 权限） |

## 产物仓库结构

```
OutbreakBowling-build/
  wechat/
    latest/           # 最新微信包
    abc1234/          # 按 commit 短 SHA
    manifest-latest.json
  douyin/latest/
  webgl/latest/
  android/latest/
  ios/latest/
```

## 本地模拟同步

```bash
# 构建后（Editor 或 batchmode）会生成 build/ci/manifest.json
./scripts/ci/sync-build-repo.sh \
  --manifest build/ci/manifest.json \
  --dest ../OutbreakBowling-build \
  --flavor wechat \
  --commit $(git rev-parse HEAD)
```

PowerShell：

```powershell
.\scripts\ci\sync-build-repo.ps1 `
  -Manifest build/ci/manifest.json `
  -Dest ..\OutbreakBowling-build `
  -Flavor wechat `
  -Commit (git rev-parse HEAD)
```

## 子模块接入

1. 在 GitHub 创建空仓库 **`OutbreakBowling-build`**
2. 将主仓库 `scripts/ci/OutbreakBowling-build/README.md` 复制为 build 仓根目录 `README.md` 并首次提交
3. 参见根目录 `.gitmodules.build.example` 添加 submodule

## manifest.json 字段

| 字段 | 说明 |
|------|------|
| `productName` | PlayerSettings.productName |
| `flavor` | wechat / douyin / webgl / android / ios |
| `artifactPath` | 本次构建产物目录绝对路径 |
| `buildRepoName` | 如 `OutbreakBowling-build` |
| `gitCommit` | `GITHUB_SHA` 或本地 `git rev-parse` |
