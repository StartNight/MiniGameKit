/****************************************************
 * FileName:		DouyinSdkBootstrap
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * CreateTime:		2026-07-27
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		抖音 BGDT 离线兜底（Git UPM 不可用时 ImportPackage）
 *
 *****************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 抖音 BGDT 离线兜底：无 UPM 且无 Active/Archive 时引导导入 MiniGameKit 内置 .unitypackage。
    /// </summary>
    public static class DouyinSdkBootstrap
    {
        public const string ActiveRelPath = "Assets/Plugins/ByteGame";
        public const string ArchiveRelPath = "SDKs/Douyin/ByteGame";

        public static bool ExistsActiveOrArchive(string projectRoot)
        {
            return ExistsPath(Path.Combine(projectRoot, ActiveRelPath))
                || ExistsPath(Path.Combine(projectRoot, ArchiveRelPath));
        }

        static bool ExistsPath(string abs)
        {
            abs = Path.GetFullPath(abs).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Directory.Exists(abs) || File.Exists(abs);
        }

        /// <summary>
        /// 离线兜底：弹出 ImportPackage 对话框。返回 true 表示已发起导入；
        /// false 表示包文件缺失（已 Dialog）。
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
    }
}
