/****************************************************
 * FileName:		DouyinSdkBootstrap
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * CreateTime:		2026-07-27
 * Version:			1.1
 * UnityVersion:	2022.3.43f1c1
 * Description:		抖音 StarkSDK 自动恢复/安装引导（归档 → BGDT 缓存 → 离线种子 → 手动 BGDT）
 *
 *****************************************************/

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 抖音 SDK 就绪检测与自动安装：优先归档恢复结果，其次 BGDT 本机缓存，再次离线 unitypackage，最后引导手动安装。
    /// </summary>
    public static class DouyinSdkBootstrap
    {
        public const string ActiveRelPath = "Assets/Plugins/ByteGame";
        public const string ArchiveRelPath = "SDKs/Douyin/ByteGame";
        public const string StarkSdkFolderName = "com.bytedance.starksdk";
        public const string BgdtMenuPath = "ByteGame/ByteGame Develop Tools";

        public static string StarkSdkActiveRelPath =>
            Path.Combine(ActiveRelPath, StarkSdkFolderName).Replace('\\', '/');

        public static bool ExistsActiveOrArchive(string projectRoot)
        {
            return ExistsPath(Path.Combine(projectRoot, ActiveRelPath))
                || ExistsPath(Path.Combine(projectRoot, ArchiveRelPath));
        }

        /// <summary>StarkSDK 是否可被编译引用（以 ttsdk.dll 为准）。</summary>
        public static bool IsStarkSdkReady(string projectRoot)
        {
            string dll = Path.Combine(projectRoot, ActiveRelPath, StarkSdkFolderName, "ttsdk.dll");
            return File.Exists(Path.GetFullPath(dll));
        }

        /// <summary>
        /// BGDT 安装 StarkSDK 时要求目标父目录存在，否则复制会失败。
        /// </summary>
        public static void EnsureActiveDirectory(string projectRoot)
        {
            string abs = Path.GetFullPath(Path.Combine(projectRoot, ActiveRelPath));
            if (!Directory.Exists(abs))
            {
                Directory.CreateDirectory(abs);
                Debug.Log($"[DouyinSdkBootstrap] 已创建目录: {ActiveRelPath}");
            }
        }

        /// <summary>
        /// 尝试自动确保 StarkSDK 就绪。成功返回 true。
        /// 顺序：已存在 → BGDT Temp 缓存目录 → BGDT Temp zip → 离线 BGDT unitypackage（仅壳，通常不够）→ 失败。
        /// </summary>
        public static bool TryEnsureStarkSdk(string projectRoot, out string sourceUsed)
        {
            sourceUsed = null;
            EnsureActiveDirectory(projectRoot);

            if (IsStarkSdkReady(projectRoot))
            {
                sourceUsed = "already-present";
                return true;
            }

            string dest = Path.GetFullPath(Path.Combine(projectRoot, ActiveRelPath, StarkSdkFolderName));

            // 1) 归档下的 StarkSDK 子目录（若整包 ByteGame 未恢复到 Active，但子路径在 Archive）
            string archiveStark = Path.GetFullPath(Path.Combine(projectRoot, ArchiveRelPath, StarkSdkFolderName));
            if (Directory.Exists(archiveStark) && File.Exists(Path.Combine(archiveStark, "ttsdk.dll")))
            {
                if (TryCopyDirectory(archiveStark, dest))
                {
                    sourceUsed = "archive";
                    AssetDatabase.Refresh();
                    return IsStarkSdkReady(projectRoot);
                }
            }

            // 2) BGDT 本机解压缓存
            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp", "bgdt", StarkSdkFolderName);
            if (Directory.Exists(cacheDir) && File.Exists(Path.Combine(cacheDir, "ttsdk.dll")))
            {
                if (TryCopyDirectory(cacheDir, dest))
                {
                    sourceUsed = "bgdt-temp-cache";
                    AssetDatabase.Refresh();
                    return IsStarkSdkReady(projectRoot);
                }
            }

            // 3) BGDT 本机 zip 缓存
            string zip = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Temp", "bgdt", "com.bytedance.starksdk-6.8.0.zip");
            if (!File.Exists(zip))
            {
                // 任选最新 starksdk zip
                string bgdtTemp = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Temp", "bgdt");
                if (Directory.Exists(bgdtTemp))
                {
                    var zips = Directory.GetFiles(bgdtTemp, "com.bytedance.starksdk-*.zip");
                    if (zips.Length > 0)
                    {
                        Array.Sort(zips);
                        zip = zips[zips.Length - 1];
                    }
                }
            }

            if (File.Exists(zip))
            {
                string extractTo = Path.Combine(Path.GetTempPath(), "mgkit-starksdk-extract-" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(extractTo);
                    System.IO.Compression.ZipFile.ExtractToDirectory(zip, extractTo);
                    string extractedRoot = FindStarkSdkRoot(extractTo);
                    if (extractedRoot != null && TryCopyDirectory(extractedRoot, dest))
                    {
                        sourceUsed = "bgdt-temp-zip:" + Path.GetFileName(zip);
                        AssetDatabase.Refresh();
                        return IsStarkSdkReady(projectRoot);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DouyinSdkBootstrap] 解压 BGDT zip 失败: {ex.Message}");
                }
                finally
                {
                    try { if (Directory.Exists(extractTo)) Directory.Delete(extractTo, true); } catch { /* ignore */ }
                }
            }

            return false;
        }

        /// <summary>
        /// StarkSDK 仍不可用时：提示并尝试打开 BGDT。返回是否打开了菜单。
        /// </summary>
        public static bool PromptManualInstallAndOpenBgdt()
        {
            bool open = EditorUtility.DisplayDialog(
                "需要安装抖音 StarkSDK",
                "已安装/准备 BGDT，但尚未找到可自动恢复的 StarkSDK。\n\n" +
                "请在 ByteGame Develop Tools 中安装 StarkSDK，完成后再次切换到「抖音小游戏」。\n\n" +
                "（首次安装成功后，切走平台会归档到 SDKs/Douyin，之后可自动恢复。）",
                "打开 BGDT",
                "稍后");

            if (!open)
                return false;

            if (!EditorApplication.ExecuteMenuItem(BgdtMenuPath))
            {
                EditorUtility.DisplayDialog(
                    "无法打开 BGDT",
                    "未找到菜单「ByteGame/ByteGame Develop Tools」。\n请确认 com.bytedance.bgdt 已通过 UPM 安装并完成编译。",
                    "确定");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 离线兜底：弹出 ImportPackage 对话框（仅 BGDT 壳，一般仍需再装 StarkSDK）。
        /// </summary>
        public static bool TryImportSeedPackageInteractive(string projectRoot)
        {
            string rel = MGKitEditorPaths.DouyinSeedUnityPackageRelPath;
            string abs = Path.GetFullPath(Path.Combine(projectRoot, rel));
            if (!File.Exists(abs))
            {
                EditorUtility.DisplayDialog(
                    "抖音 SDK 缺失",
                    $"未找到离线兜底包：\n{abs}\n\n请将 com.bytedance.bgdt-cp-*.unitypackage 放到 MiniGameKit 包根目录。",
                    "确定");
                return false;
            }

            Debug.Log($"[DouyinSdkBootstrap] 离线兜底导入抖音 BGDT: {abs}");
            AssetDatabase.ImportPackage(abs, true);
            return true;
        }

        static string FindStarkSdkRoot(string extractRoot)
        {
            if (File.Exists(Path.Combine(extractRoot, "ttsdk.dll")))
                return extractRoot;

            string nested = Path.Combine(extractRoot, StarkSdkFolderName);
            if (File.Exists(Path.Combine(nested, "ttsdk.dll")))
                return nested;

            foreach (var dir in Directory.GetDirectories(extractRoot, "*", SearchOption.AllDirectories))
            {
                if (File.Exists(Path.Combine(dir, "ttsdk.dll")))
                    return dir;
            }

            return null;
        }

        static bool TryCopyDirectory(string source, string dest)
        {
            try
            {
                source = Path.GetFullPath(source);
                dest = Path.GetFullPath(dest);
                if (Directory.Exists(dest))
                    Directory.Delete(dest, true);

                CopyDirectoryRecursive(source, dest);
                Debug.Log($"[DouyinSdkBootstrap] 已复制 StarkSDK:\n  {source}\n→ {dest}");
                return File.Exists(Path.Combine(dest, "ttsdk.dll"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DouyinSdkBootstrap] 复制 StarkSDK 失败: {ex.Message}");
                return false;
            }
        }

        static void CopyDirectoryRecursive(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
            {
                string name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(dest, name), true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                string name = Path.GetFileName(dir);
                CopyDirectoryRecursive(dir, Path.Combine(dest, name));
            }
        }

        static bool ExistsPath(string abs)
        {
            abs = Path.GetFullPath(abs).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Directory.Exists(abs) || File.Exists(abs);
        }
    }
}
