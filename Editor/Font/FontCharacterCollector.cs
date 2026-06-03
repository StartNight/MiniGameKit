using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MGKit.Editor
{
    /// <summary>
    /// 收集项目内使用字符：用于静态字体子集、TMP 分字体导出等。
    /// </summary>
    public static class FontCharacterCollector
    {
        private const string CommonAscii =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        [MenuItem(MGKitEditorPaths.FontMenu + "收集全项目字符并写入 TTF", false, 200)]
        public static void CollectProjectCharactersToTtf()
        {
            var sb = new StringBuilder();
            foreach (var folder in MGKitEditorPaths.FontScanScriptFolders)
            {
                var full = MGKitEditorPaths.ToFullPath(folder.Trim());
                if (Directory.Exists(full))
                    sb.Append(ScanCSharpScripts(full));
            }

            sb.Append(ScanLocalizationFolder(MGKitEditorPaths.FontScanLocalizationFolder));
            sb.Append(ScanPrefabsUnder(MGKitEditorPaths.FontScanPrefabRoots));

            var characters = Deduplicate(sb.ToString()) + CommonAscii;
            ApplyToTrueTypeFont(MGKitEditorPaths.FontScanTargetTtf, characters);
            WriteTextFile(MGKitEditorPaths.FontSubsetOutputFolder, "字符.txt", characters);
            Debug.Log($"[FontCollector] 共 {characters.Length} 个不重复字符。");
        }

        [MenuItem(MGKitEditorPaths.FontMenu + "按 TMP 字体导出字符集", false, 201)]
        public static void ExportTmpFontCharacterSets()
        {
            var outputFolder = MGKitEditorPaths.FontSubsetOutputFolder;
            if (!Directory.Exists(MGKitEditorPaths.ToFullPath(outputFolder)))
                Directory.CreateDirectory(MGKitEditorPaths.ToFullPath(outputFolder));

            var multiLanguageChars = CollectMultiLanguageCharacterSet();
            var fontToChars = new Dictionary<TMP_FontAsset, HashSet<char>>();
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("扫描 Prefab", path, (float)i / guids.Length);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                foreach (var tmp in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp.font == null)
                        continue;

                    if (!fontToChars.ContainsKey(tmp.font))
                        fontToChars[tmp.font] = new HashSet<char>();

                    AddCharactersFromText(fontToChars[tmp.font], tmp.text);
                }
            }

            EditorUtility.ClearProgressBar();

            foreach (var kvp in fontToChars)
            {
                if (kvp.Value.Count == 0 && multiLanguageChars.Count == 0)
                    continue;

                foreach (var c in multiLanguageChars)
                    kvp.Value.Add(c);

                var sorted = kvp.Value.ToList();
                sorted.Sort();
                var sb = new StringBuilder();
                foreach (var c in sorted)
                    sb.Append(c);

                WriteTextFile(outputFolder, $"{kvp.Key.name}.txt", sb.ToString() + CommonAscii);
            }

            AssetDatabase.Refresh();
            var langCount = multiLanguageChars.Count;
            EditorUtility.DisplayDialog(
                "完成",
                $"字符集已导出到 {outputFolder}\n（已合并 I2/CSV 多语言字符 {langCount} 个）",
                "确定");
            Debug.Log($"[FontCollector] TMP 分字体导出完成，多语言字符 {langCount} 个。");
        }

        /// <summary>
        /// 从 I2 LanguageSource 与本地化 CSV 收集全部语言字符。
        /// </summary>
        public static HashSet<char> CollectMultiLanguageCharacterSet()
        {
            var chars = new HashSet<char>();
            AddCharactersFromText(chars, ScanLocalizationFolder(MGKitEditorPaths.FontScanLocalizationFolder));
            AddCharactersFromI2LanguageSource(chars);
            return chars;
        }

        public static string Deduplicate(string input)
        {
            var seen = new HashSet<char>();
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                if (seen.Add(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private static void AddCharactersFromText(HashSet<char> chars, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            foreach (var c in text)
            {
                if (!char.IsWhiteSpace(c))
                    chars.Add(c);
            }
        }

        private static void AddCharactersFromI2LanguageSource(HashSet<char> chars)
        {
            var path = MGKitEditorPaths.I2LanguageSourceAssetPath;
            if (string.IsNullOrEmpty(path))
                return;

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("[FontCollector] 未找到 I2 LanguageSource：" + path);
                return;
            }

            var sourceData = GetI2LanguageSourceData(asset);
            if (sourceData == null)
            {
                Debug.LogWarning("[FontCollector] 无法读取 I2 mSource：" + path);
                return;
            }

            var terms = GetReflectionMember(sourceData, "mTerms") as IList;
            if (terms == null)
                return;

            foreach (var term in terms)
            {
                if (term == null)
                    continue;

                var languages = GetReflectionMember(term, "Languages") as string[];
                if (languages == null)
                    continue;

                foreach (var translation in languages)
                    AddCharactersFromText(chars, translation);
            }
        }

        private static object GetI2LanguageSourceData(ScriptableObject asset)
        {
            var assetType = asset.GetType();
            if (assetType.FullName != "I2.Loc.LanguageSourceAsset")
                return null;

            return GetReflectionMember(asset, "mSource");
        }

        private static object GetReflectionMember(object target, string memberName)
        {
            if (target == null)
                return null;

            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField(memberName, flags);
            if (field != null)
                return field.GetValue(target);

            var prop = type.GetProperty(memberName, flags);
            return prop?.GetValue(target);
        }

        private static string ScanCSharpScripts(string folder)
        {
            var sb = new StringBuilder();
            var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                EditorUtility.DisplayProgressBar("扫描脚本", files[i], (float)i / files.Length);
                sb.Append(File.ReadAllText(files[i]));
            }

            EditorUtility.ClearProgressBar();
            return sb.ToString();
        }

        private static string ScanLocalizationFolder(string assetFolder)
        {
            var full = MGKitEditorPaths.ToFullPath(assetFolder);
            if (!Directory.Exists(full))
                return string.Empty;

            var sb = new StringBuilder();
            var files = Directory.GetFiles(full, "*.csv", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                EditorUtility.DisplayProgressBar("扫描本地化", files[i], (float)i / files.Length);
                sb.AppendLine(File.ReadAllText(files[i], Encoding.UTF8));
            }

            EditorUtility.ClearProgressBar();
            return sb.ToString();
        }

        private static string ScanPrefabsUnder(string assetRoots)
        {
            var sb = new StringBuilder();
            var roots = assetRoots.Split(';');
            var guids = AssetDatabase.FindAssets("t:Prefab", roots);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("扫描 Prefab 文本", path, (float)i / guids.Length);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                    continue;

                foreach (var t in go.GetComponentsInChildren<Text>(true))
                    sb.Append(t.text);
                foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                    sb.Append(t.text);

                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null)
                        continue;
                    foreach (var field in mb.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (field.FieldType == typeof(string))
                            sb.Append((string)field.GetValue(mb));
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            return sb.ToString();
        }

        private static void ApplyToTrueTypeFont(string ttfAssetPath, string characters)
        {
            var imp = AssetImporter.GetAtPath(ttfAssetPath) as TrueTypeFontImporter;
            if (imp == null)
            {
                Debug.LogWarning("[FontCollector] 无法加载字体：" + ttfAssetPath);
                return;
            }

            imp.customCharacters = characters;
            AssetDatabase.ImportAsset(ttfAssetPath);
        }

        private static void WriteTextFile(string outputAssetFolder, string fileName, string content)
        {
            var dir = MGKitEditorPaths.ToFullPath(outputAssetFolder);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, fileName), content, Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
