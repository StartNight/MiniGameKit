/****************************************************
 * FileName:		ManifestPackageSwitcher
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * CreateTime:		2026-07-27
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		装卸平台 UPM 依赖（微信 / 抖音 BGDT 等）
 *
 *****************************************************/

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 通过编辑 Packages/manifest.json 装卸平台 UPM 依赖（微信 / 抖音 BGDT 等）。
    /// </summary>
    public static class ManifestPackageSwitcher
    {
        public static string ManifestPath =>
            Path.GetFullPath(Path.Combine(MGKitEditorPaths.ProjectRoot, "Packages", "manifest.json"));

        public static bool HasPackage(string packageId) =>
            HasPackageInText(File.ReadAllText(ManifestPath), packageId);

        /// <summary>若已存在则保留原 URL，返回 false；新写入返回 true。</summary>
        public static bool EnsurePackage(string packageId, string gitUrl)
        {
            string text = File.ReadAllText(ManifestPath);
            if (HasPackageInText(text, packageId))
                return false;

            string newText = InsertDependency(text, packageId, gitUrl);
            if (newText == null)
                throw new InvalidOperationException("无法在 manifest.json 的 dependencies 中插入依赖。");

            File.WriteAllText(ManifestPath, newText, new UTF8Encoding(false));
            return true;
        }

        /// <summary>移除依赖；若不存在返回 false。</summary>
        public static bool RemovePackage(string packageId)
        {
            string text = File.ReadAllText(ManifestPath);
            if (!HasPackageInText(text, packageId))
                return false;

            string newText = RemoveDependency(text, packageId);
            if (newText == null)
                throw new InvalidOperationException("无法从 manifest.json 移除依赖。");

            File.WriteAllText(ManifestPath, newText, new UTF8Encoding(false));
            return true;
        }

        public static bool HasPackageInText(string manifestJson, string packageId)
        {
            var pattern = "\"" + Regex.Escape(packageId) + "\"\\s*:";
            return Regex.IsMatch(manifestJson, pattern);
        }

        public static string InsertDependency(string manifestJson, string packageId, string gitUrl)
        {
            var depsOpen = Regex.Match(manifestJson, "\"dependencies\"\\s*:\\s*\\{");
            if (!depsOpen.Success)
                return null;

            int insertAt = depsOpen.Index + depsOpen.Length;
            string line = "\n    \"" + packageId + "\": \"" + gitUrl + "\",";
            return manifestJson.Insert(insertAt, line);
        }

        public static string RemoveDependency(string manifestJson, string packageId)
        {
            // 匹配整行键值（含前导换行/空白与可选尾逗号）
            var pattern = new Regex(
                "\\r?\\n[ \\t]*\"" + Regex.Escape(packageId) + "\"\\s*:\\s*\"[^\"]*\"\\s*,?",
                RegexOptions.Multiline);
            if (!pattern.IsMatch(manifestJson))
                return null;

            string result = pattern.Replace(manifestJson, "", 1);
            // 若删的是块内最后一项且留下尾逗号： "url",\n  } → "url"\n  }
            result = Regex.Replace(result, ",(\\s*})", "$1");
            return result;
        }
    }
}
