/****************************************************
 * FileName:		PlatformSwitchTool
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * CreateTime:		2026-06-01 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		平台快速切换核心逻辑
 *
 *****************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MGKit;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MGKit.Editor
{
    [InitializeOnLoad]
    public static class PlatformSwitchTool
    {
        private static readonly string PROJ_ROOT = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
        private static readonly string SDK_ROOT = Path.Combine(PROJ_ROOT, "SDKs");

        private class SDKPath
        {
            public string Active;
            public string Archive;
        }

        private class PlatformConfig
        {
            public MiniGamePlatform Platform;
            public string DisplayName;
            public BuildTargetGroup BuildGroup;
            public BuildTarget BuildTarget;
            public string Macro;
            public SDKPath[] SDKs;
        }

        private static List<PlatformConfig> _configs;
        private static string[] _options;
        private static int _currentIndex = -1;

        private static AddressablesProviderMode? _pendingProviderMode;
        private static bool _compilationFinishedSubscribed;
        private static bool _delayCallScheduled;

        const string TTAssetBundleProviderTypeName =
            "UnityEngine.ResourceManagement.ResourceProviders.TTAssetBundleProvider, MiniGameKit.Runtime.Addressables.Douyin";
        const string TTBundledAssetProviderTypeName =
            "UnityEngine.ResourceManagement.ResourceProviders.TTBundledAssetProvider, MiniGameKit.Runtime.Addressables.Douyin";

        static PlatformSwitchTool()
        {
            InitConfigs();
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
            EditorApplication.delayCall += EnsureDouyinMacroConsistentWithSdk;
        }

        /// <summary>
        /// 若已启用 DOUYINMINIGAME 但 StarkSDK 被删：先尝试自动恢复，失败则清宏，避免 Runtime CS0246。
        /// </summary>
        static void EnsureDouyinMacroConsistentWithSdk()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!defines.Contains(MGKitScriptingDefines.Douyin))
                return;

            if (DouyinSdkBootstrap.IsStarkSdkReady(PROJ_ROOT))
                return;

            if (DouyinSdkBootstrap.TryEnsureStarkSdk(PROJ_ROOT, out string source))
            {
                Debug.Log($"[PlatformSwitchTool] 自动恢复 StarkSDK 成功（{source}），保留 DOUYINMINIGAME。");
                return;
            }

            Debug.LogWarning(
                "[PlatformSwitchTool] 检测到 DOUYINMINIGAME 但 StarkSDK 缺失，已清除该宏以避免编译错误。请重新切换到「抖音小游戏」。");
            UpdateMacros(group, "");
        }

        private static void InitConfigs()
        {
            _configs = new List<PlatformConfig>()
            {
                new PlatformConfig()
                {
                    Platform = MiniGamePlatform.Editor,
                    DisplayName = "纯净 WebGL",
                    BuildGroup = BuildTargetGroup.WebGL,
                    BuildTarget = BuildTarget.WebGL,
                    Macro = "",
                    SDKs = new SDKPath[0]
                },
                new PlatformConfig()
                {
                    Platform = MiniGamePlatform.WeChatMiniGame,
                    DisplayName = "微信小游戏",
                    BuildGroup = BuildTargetGroup.WebGL,
                    BuildTarget = BuildTarget.WebGL,
                    Macro = MGKitScriptingDefines.WeChat,
                    // 微信 SDK 经 UPM（manifest.json）装卸，不再物理移动 Assets/WX-WASM-SDK-V2
                    SDKs = new SDKPath[0]
                },
                new PlatformConfig()
                {
                    Platform = MiniGamePlatform.DouyinMiniGame,
                    DisplayName = "抖音小游戏",
                    BuildGroup = BuildTargetGroup.WebGL,
                    BuildTarget = BuildTarget.WebGL,
                    Macro = MGKitScriptingDefines.Douyin,
                    SDKs = new SDKPath[]
                    {
                        new SDKPath
                        {
                            Active = DouyinSdkBootstrap.ActiveRelPath,
                            Archive = DouyinSdkBootstrap.ArchiveRelPath
                        }
                    }
                },
                new PlatformConfig()
                {
                    Platform = MiniGamePlatform.CrazyGames,
                    DisplayName = "CrazyGames",
                    BuildGroup = BuildTargetGroup.WebGL,
                    BuildTarget = BuildTarget.WebGL,
                    Macro = "CRAZYGAMES",
                    SDKs = new SDKPath[]
                    {
                        new SDKPath { Active = "Assets/Thirdparty/CrazySDK", Archive = "SDKs/CrazyGames/CrazySDK" },
                        new SDKPath { Active = "Assets/Plugins/crazySDK.jslib", Archive = "SDKs/CrazyGames/crazySDK.jslib" }
                    }
                },
                new PlatformConfig()
                {
                    Platform = MiniGamePlatform.Android,
                    DisplayName = "Android (原生)",
                    BuildGroup = BuildTargetGroup.Android,
                    BuildTarget = BuildTarget.Android,
                    Macro = "",
                    SDKs = new SDKPath[0]
                },
                new PlatformConfig()
                {
                    Platform = MiniGamePlatform.iOS,
                    DisplayName = "iOS (原生)",
                    BuildGroup = BuildTargetGroup.iOS,
                    BuildTarget = BuildTarget.iOS,
                    Macro = "",
                    SDKs = new SDKPath[0]
                }
            };

            _options = _configs.Select(c => c.DisplayName).ToArray();
        }

        private static void OnToolbarGUI()
        {
            if (_currentIndex == -1)
            {
                DetectCurrentIndex();
            }

            EditorGUI.BeginChangeCheck();
            GUI.contentColor = Color.green;
            int newIndex = EditorGUILayout.Popup(_currentIndex, _options, GUILayout.Width(130));
            GUI.contentColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                if (EditorUtility.DisplayDialog(
                        "切换平台",
                        $"确认切换到 {(_configs[newIndex].DisplayName)} 平台？\n\n" +
                        "将修改 Build Target、宏定义，并隔离不相关的 SDK（复制到 SDKs 后删除 Active）。\n" +
                        "微信 / 抖音 BGDT：增删 Packages/manifest.json 中的 UPM 依赖。\n" +
                        "抖音：从 SDKs 复制恢复 StarkSDK；若无缓存则引导打开 BGDT 安装。",
                        "确认",
                        "取消"))
                {
                    int previousIndex = _currentIndex;
                    _currentIndex = newIndex;
                    if (!SwitchToPlatform(_configs[newIndex]))
                        _currentIndex = previousIndex;
                }
            }
        }

        private static void DetectCurrentIndex()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Match based on macros first
            for (int i = 0; i < _configs.Count; i++)
            {
                if (!string.IsNullOrEmpty(_configs[i].Macro) && defines.Contains(_configs[i].Macro))
                {
                    _currentIndex = i;
                    return;
                }
            }

            // Fallback to build target
            for (int i = 0; i < _configs.Count; i++)
            {
                if (_configs[i].BuildTarget == target && string.IsNullOrEmpty(_configs[i].Macro))
                {
                    _currentIndex = i;
                    return;
                }
            }

            _currentIndex = 0; // fallback
        }

        private static bool SwitchToPlatform(PlatformConfig config)
        {
            bool completed = false;
            try
            {
                // 关闭潜在的第三方 SDK 面板（如微信小游戏配置面板），防止它们在目录被移走后触发 OnDisable/OnFocus 导致 Crash
                var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                foreach (var w in windows)
                {
                    if (w != null && (w.GetType().Name.Contains("WXEditorWin") || w.GetType().Name.Contains("ByteGame")))
                    {
                        try { w.Close(); } catch { /* ignore */ }
                    }
                }

                EditorUtility.DisplayProgressBar("Platform Switcher", "正在隔离不相关的 SDK 文件...", 0.2f);

                // 1. 隔离其他平台的 SDK 到 SDKs 目录
                foreach (var otherConfig in _configs)
                {
                    if (otherConfig == config) continue;

                    foreach (var sdk in otherConfig.SDKs)
                    {
                        MoveToArchive(sdk.Active, sdk.Archive);
                    }
                }

                // 2. UPM：微信 / 抖音 BGDT 与当前平台对齐
                EditorUtility.DisplayProgressBar("Platform Switcher", "正在同步平台 UPM 依赖...", 0.35f);
                try
                {
                    if (config.Platform == MiniGamePlatform.WeChatMiniGame)
                    {
                        bool added = ManifestPackageSwitcher.EnsurePackage(
                            MGKitEditorPaths.WeChatUpmPackageId,
                            MGKitEditorPaths.WeChatPackageGitUrl);
                        Debug.Log(added
                            ? "[PlatformSwitchTool] 已写入微信 UPM 依赖到 manifest.json"
                            : "[PlatformSwitchTool] manifest 已含微信 UPM，保留现有 URL");
                    }
                    else if (ManifestPackageSwitcher.RemovePackage(MGKitEditorPaths.WeChatUpmPackageId))
                    {
                        Debug.Log("[PlatformSwitchTool] 已从 manifest.json 移除微信 UPM");
                    }

                    if (config.Platform == MiniGamePlatform.DouyinMiniGame)
                    {
                        DouyinSdkBootstrap.EnsureActiveDirectory(PROJ_ROOT);
                        bool added = ManifestPackageSwitcher.EnsurePackage(
                            MGKitEditorPaths.DouyinUpmPackageId,
                            MGKitEditorPaths.DouyinPackageGitUrl);
                        Debug.Log(added
                            ? "[PlatformSwitchTool] 已写入抖音 BGDT UPM 到 manifest.json"
                            : "[PlatformSwitchTool] manifest 已含抖音 BGDT UPM，保留现有 URL");
                    }
                    else if (ManifestPackageSwitcher.RemovePackage(MGKitEditorPaths.DouyinUpmPackageId))
                    {
                        Debug.Log("[PlatformSwitchTool] 已从 manifest.json 移除抖音 BGDT UPM");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("切换失败", "同步平台 UPM 失败：\n" + ex.Message, "确定");
                    Debug.LogError(ex);
                    return false;
                }

                EditorUtility.DisplayProgressBar("Platform Switcher", "正在加载目标平台 SDK...", 0.5f);

                // 3. 将目标平台的 SDK 从 SDKs 目录恢复到 Assets
                foreach (var sdk in config.SDKs)
                {
                    RestoreFromArchive(sdk.Archive, sdk.Active);
                }

                // 4. 抖音：自动确保 StarkSDK；就绪前不写 DOUYINMINIGAME，避免 CS0246
                if (config.Platform == MiniGamePlatform.DouyinMiniGame)
                {
                    bool hasUpm = ManifestPackageSwitcher.HasPackage(MGKitEditorPaths.DouyinUpmPackageId);
                    if (!hasUpm && !DouyinSdkBootstrap.ExistsActiveOrArchive(PROJ_ROOT))
                    {
                        if (!DouyinSdkBootstrap.TryImportSeedPackageInteractive(PROJ_ROOT))
                        {
                            EditorUtility.DisplayDialog("切换中止",
                                "无法写入抖音 BGDT UPM，且离线 unitypackage 不可用。", "确定");
                            return false;
                        }
                        Debug.LogWarning("[PlatformSwitchTool] 已回退为 Interactive ImportPackage（离线兜底）。");
                    }

                    EditorUtility.DisplayProgressBar("Platform Switcher", "正在自动安装/恢复 StarkSDK...", 0.55f);
                    if (!DouyinSdkBootstrap.TryEnsureStarkSdk(PROJ_ROOT, out string starkSource))
                    {
                        // 切 WebGL 但清除抖音宏，保持可编译；引导手动装 StarkSDK
                        if (EditorUserBuildSettings.activeBuildTarget != config.BuildTarget)
                            EditorUserBuildSettings.SwitchActiveBuildTarget(config.BuildGroup, config.BuildTarget);
                        UpdateMacros(config.BuildGroup, "");
                        AssetDatabase.Refresh();
                        ScheduleApplyAddressablesProviders(AddressablesProviderMode.Default);

                        DouyinSdkBootstrap.PromptManualInstallAndOpenBgdt();
                        Debug.LogWarning(
                            "[PlatformSwitchTool] 抖音环境已准备（BGDT），但 StarkSDK 未就绪；未启用 DOUYINMINIGAME。安装后再切一次即可。");
                        return false;
                    }

                    Debug.Log($"[PlatformSwitchTool] StarkSDK 已就绪（来源: {starkSource}）");
                }

                EditorUtility.DisplayProgressBar("Platform Switcher", "正在更新宏定义与 Build Target...", 0.7f);

                // 5. 更新 Build Target
                if (EditorUserBuildSettings.activeBuildTarget != config.BuildTarget)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(config.BuildGroup, config.BuildTarget);
                }

                // 6. 更新宏定义
                UpdateMacros(config.BuildGroup, config.Macro);

                EditorUtility.DisplayProgressBar("Platform Switcher", "正在刷新 AssetDatabase...", 0.9f);
                AssetDatabase.Refresh();

                ScheduleApplyAddressablesProviders(ResolveProviderMode(config));

                completed = true;
                return true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (completed)
                    Debug.Log($"[PlatformSwitchTool] 已成功切换至 {config.DisplayName}");
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// 隔离：先复制 Active→Archive（覆盖归档），再删除 Active，保证 SDKs 备份常在。
        /// </summary>
        private static void MoveToArchive(string activeRelPath, string archiveRelPath)
        {
            string activeAbs = NormalizePath(Path.Combine(PROJ_ROOT, activeRelPath));
            string archiveAbs = NormalizePath(Path.Combine(PROJ_ROOT, archiveRelPath));

            bool isDir = Directory.Exists(activeAbs);
            bool isFile = File.Exists(activeAbs);
            Debug.Log($"[PlatformSwitchTool] MoveToArchive 检测: {activeAbs} | 是目录={isDir} | 是文件={isFile}");

            if (isDir || isFile)
            {
                if (!SafeCopy(activeAbs, archiveAbs))
                    return;

                if (isDir)
                {
                    if (!TryDeleteDirectory(activeAbs))
                    {
                        Debug.LogWarning(
                            $"[PlatformSwitchTool] 已复制到归档，但 Active 目录被占用无法删除，请关闭 Unity 后手动删除: {activeAbs}");
                    }
                }
                else
                {
                    try
                    {
                        File.SetAttributes(activeAbs, FileAttributes.Normal);
                        File.Delete(activeAbs);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"[PlatformSwitchTool] 已复制到归档，但 Active 文件被占用无法删除: {activeAbs}\n{ex.Message}");
                    }
                }
            }

            // .meta 文件：同样复制后删除 Active 侧
            string metaSrc = activeAbs + ".meta";
            if (File.Exists(metaSrc))
            {
                if (SafeCopy(metaSrc, archiveAbs + ".meta"))
                {
                    try
                    {
                        File.SetAttributes(metaSrc, FileAttributes.Normal);
                        File.Delete(metaSrc);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"[PlatformSwitchTool] Active .meta 已复制到归档，但无法删除源: {metaSrc}\n{ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 恢复：复制 Archive→Active（覆盖），保留 SDKs 归档不删。
        /// </summary>
        private static void RestoreFromArchive(string archiveRelPath, string activeRelPath)
        {
            string archiveAbs = NormalizePath(Path.Combine(PROJ_ROOT, archiveRelPath));
            string activeAbs = NormalizePath(Path.Combine(PROJ_ROOT, activeRelPath));

            bool isDir = Directory.Exists(archiveAbs);
            bool isFile = File.Exists(archiveAbs);
            Debug.Log($"[PlatformSwitchTool] RestoreFromArchive 检测: {archiveAbs} | 是目录={isDir} | 是文件={isFile}");

            if (isDir || isFile)
                SafeCopy(archiveAbs, activeAbs);

            string metaSrc = archiveAbs + ".meta";
            if (File.Exists(metaSrc))
                SafeCopy(metaSrc, activeAbs + ".meta");
        }

        /// <summary>
        /// 复制 source→dest（覆盖），不删除源。目录用递归复制；文件用 File.Copy。
        /// </summary>
        private static bool SafeCopy(string source, string dest)
        {
            source = NormalizePath(source);
            dest = NormalizePath(dest);

            try
            {
                if (Directory.Exists(source))
                {
                    string destParent = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destParent) && !Directory.Exists(destParent))
                        Directory.CreateDirectory(destParent);

                    if (Directory.Exists(dest))
                    {
                        if (!TryDeleteDirectory(dest))
                        {
                            Debug.LogError(
                                $"[PlatformSwitchTool] 目标目录无法清空，复制中止: {dest}");
                            return false;
                        }
                    }

                    CopyDirectoryRecursive(source, dest);
                    Debug.Log($"[PlatformSwitchTool] 目录已复制: {source} -> {dest}");
                    return true;
                }

                if (File.Exists(source))
                {
                    string destParent = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destParent) && !Directory.Exists(destParent))
                        Directory.CreateDirectory(destParent);
                    File.Copy(source, dest, true);
                    Debug.Log($"[PlatformSwitchTool] 文件已复制: {source} -> {dest}");
                    return true;
                }

                Debug.LogWarning($"[PlatformSwitchTool] 源路径不存在，跳过: {source}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlatformSwitchTool] 复制失败: {source} -> {dest}\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// 递归复制目录，逐文件操作，跳过被占用的单个文件不影响整体
        /// </summary>
        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlatformSwitchTool] 复制文件失败(跳过): {file}\n{ex.Message}");
                }
            }

            // 递归处理子目录
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, destSubDir);
            }
        }

        /// <summary>
        /// 尝试删除目录，带重试机制处理文件占用
        /// </summary>
        private static bool TryDeleteDirectory(string dirPath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (!Directory.Exists(dirPath)) return true;

                    // 逐文件删除，记录失败的
                    var failedFiles = new List<string>();
                    foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                        catch
                        {
                            failedFiles.Add(file);
                        }
                    }

                    if (failedFiles.Count > 0 && i < maxRetries - 1)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        System.Threading.Thread.Sleep(200);
                        continue;
                    }

                    // 尝试删除空目录树
                    try
                    {
                        Directory.Delete(dirPath, true);
                        return true;
                    }
                    catch
                    {
                        if (failedFiles.Count > 0)
                        {
                            Debug.LogWarning($"[PlatformSwitchTool] {failedFiles.Count} 个文件被占用无法删除:\n" +
                                string.Join("\n", failedFiles.Take(5)));
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlatformSwitchTool] 删除重试 {i + 1}/{maxRetries}: {ex.Message}");
                    if (i < maxRetries - 1)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        System.Threading.Thread.Sleep(300);
                    }
                }
            }
            return false;
        }

        static AddressablesProviderMode ResolveProviderMode(PlatformConfig config)
        {
            if (config.Platform == MiniGamePlatform.WeChatMiniGame)
                return AddressablesProviderMode.WeChat;
            if (config.Platform == MiniGamePlatform.DouyinMiniGame
                && DouyinSdkBootstrap.IsStarkSdkReady(PROJ_ROOT))
                return AddressablesProviderMode.Douyin;
            return AddressablesProviderMode.Default;
        }

        static void ScheduleApplyAddressablesProviders(AddressablesProviderMode mode)
        {
            _pendingProviderMode = mode;

            if (EditorApplication.isCompiling)
            {
                EnsureCompilationFinishedSubscription();
                return;
            }

            EnsureDelayCallScheduled();
        }

        static void EnsureCompilationFinishedSubscription()
        {
            if (_compilationFinishedSubscribed)
                return;
            _compilationFinishedSubscribed = true;
            CompilationPipeline.compilationFinished += OnCompilationFinishedApplyProviders;
        }

        static void EnsureDelayCallScheduled()
        {
            if (_delayCallScheduled)
                return;
            _delayCallScheduled = true;
            EditorApplication.delayCall += OnDelayedApplyAddressablesProviders;
        }

        static void CancelProviderApplySubscriptions()
        {
            if (_compilationFinishedSubscribed)
            {
                CompilationPipeline.compilationFinished -= OnCompilationFinishedApplyProviders;
                _compilationFinishedSubscribed = false;
            }

            if (_delayCallScheduled)
            {
                EditorApplication.delayCall -= OnDelayedApplyAddressablesProviders;
                _delayCallScheduled = false;
            }
        }

        static void OnCompilationFinishedApplyProviders(object _)
        {
            if (_compilationFinishedSubscribed)
            {
                CompilationPipeline.compilationFinished -= OnCompilationFinishedApplyProviders;
                _compilationFinishedSubscribed = false;
            }

            if (!_pendingProviderMode.HasValue)
                return;

            if (EditorApplication.isCompiling)
            {
                EnsureCompilationFinishedSubscription();
                return;
            }

            EnsureDelayCallScheduled();
        }

        static void OnDelayedApplyAddressablesProviders()
        {
            _delayCallScheduled = false;
            EditorApplication.delayCall -= OnDelayedApplyAddressablesProviders;

            if (!_pendingProviderMode.HasValue)
                return;

            if (EditorApplication.isCompiling)
            {
                EnsureCompilationFinishedSubscription();
                return;
            }

            CancelProviderApplySubscriptions();

            var mode = _pendingProviderMode.Value;
            _pendingProviderMode = null;

            if (mode == AddressablesProviderMode.Douyin && !AreDouyinProviderTypesReady())
            {
                Debug.LogWarning(
                    "[PlatformSwitchTool] Addressables Provider 自动切换失败（抖音 TT 类型编译后仍不可用，mode=Douyin）。");
                return;
            }

            SafeApplyAddressablesProviders(mode);
        }

        static bool AreDouyinProviderTypesReady()
        {
            return Type.GetType(TTAssetBundleProviderTypeName) != null
                && Type.GetType(TTBundledAssetProviderTypeName) != null;
        }

        static void SafeApplyAddressablesProviders(AddressablesProviderMode mode)
        {
            try
            {
                if (!AddressablesWeChatBuildMenu.ApplyProviders(mode))
                {
                    Debug.LogWarning(
                        $"[PlatformSwitchTool] Addressables Provider 自动切换失败（mode={mode}，不影响平台切换）。");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[PlatformSwitchTool] Addressables Provider 自动切换失败（mode={mode}，不影响平台切换）: {ex.Message}");
            }
        }

        private static void UpdateMacros(BuildTargetGroup group, string targetMacro)
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var defines = new List<string>(currentDefines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            // 移除其他所有管理中的宏
            var allManagedMacros = _configs.Where(c => !string.IsNullOrEmpty(c.Macro)).Select(c => c.Macro).ToList();
            allManagedMacros.Add(MGKitScriptingDefines.LegacyWeChatPluginMacro);
            defines.RemoveAll(d => allManagedMacros.Contains(d));

            // 添加当前目标宏
            if (!string.IsNullOrEmpty(targetMacro) && !defines.Contains(targetMacro))
            {
                defines.Add(targetMacro);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }
    }
}