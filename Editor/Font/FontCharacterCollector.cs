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
        const string CommonAscii =
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
                    if (tmp.font == null || string.IsNullOrEmpty(tmp.text))
                        continue;

                    if (!fontToChars.ContainsKey(tmp.font))
                        fontToChars[tmp.font] = new HashSet<char>();

                    foreach (var c in tmp.text)
                    {
                        if (!char.IsWhiteSpace(c))
                            fontToChars[tmp.font].Add(c);
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            foreach (var kvp in fontToChars)
            {
                if (kvp.Value.Count == 0)
                    continue;

                var sorted = kvp.Value.ToList();
                sorted.Sort();
                var sb = new StringBuilder();
                foreach (var c in sorted)
                    sb.Append(c);

                WriteTextFile(outputFolder, $"{kvp.Key.name}.txt", sb.ToString() + CommonAscii);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", $"字符集已导出到 {outputFolder}", "确定");
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

        static string ScanCSharpScripts(string folder)
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

        static string ScanLocalizationFolder(string assetFolder)
        {
            var full = MGKitEditorPaths.ToFullPath(assetFolder);
            if (!Directory.Exists(full))
                return string.Empty;

            var sb = new StringBuilder();
            var files = Directory.GetFiles(full, "*.csv", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                EditorUtility.DisplayProgressBar("扫描本地化", files[i], (float)i / files.Length);
                sb.AppendLine(File.ReadAllText(files[i]));
            }

            EditorUtility.ClearProgressBar();
            return sb.ToString();
        }

        static string ScanPrefabsUnder(string assetRoots)
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

        static void ApplyToTrueTypeFont(string ttfAssetPath, string characters)
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

        static void WriteTextFile(string outputAssetFolder, string fileName, string content)
        {
            var dir = MGKitEditorPaths.ToFullPath(outputAssetFolder);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, fileName), content, Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
