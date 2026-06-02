using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MGKit.Editor
{
    /// <summary>
    /// 将指定目录下的 .cs 文件批量转换为 UTF-8（无 BOM）。
    /// </summary>
    public class CsUtf8ConverterWindow : EditorWindow
    {
        readonly List<string> _folderPaths = new List<string>();
        readonly List<string> _scannedFiles = new List<string>();
        Vector2 _folderScroll;
        Vector2 _resultScroll;
        bool _includeSubfolders = true;
        int _skippedFolderCount;

        [MenuItem(MGKitEditorPaths.ScriptMenu + "C# 转 UTF-8 (无 BOM)", false, 100)]
        public static void Open() => GetWindow<CsUtf8ConverterWindow>("CS UTF-8").Show();

        void OnGUI()
        {
            EditorGUILayout.LabelField("将文件夹中的 .cs 转为 UTF-8（无 BOM）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("支持 Assets 相对路径或绝对路径，可多文件夹。", MessageType.Info);
            _includeSubfolders = EditorGUILayout.ToggleLeft("递归子文件夹", _includeSubfolders);
            DrawFolderList();
            DrawScanResult();
            DrawActions();
        }

        void DrawFolderList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标文件夹", EditorStyles.boldLabel);
            if (GUILayout.Button("添加", GUILayout.Width(50)))
                _folderPaths.Add(string.Empty);
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
                AddFolderByDialog();
            EditorGUILayout.EndHorizontal();

            _folderScroll = EditorGUILayout.BeginScrollView(_folderScroll, GUILayout.Height(120));
            for (var i = 0; i < _folderPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _folderPaths[i] = EditorGUILayout.TextField(_folderPaths[i]);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                    PickFolder(i);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    _folderPaths.RemoveAt(i);
                    ClearScan();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawScanResult()
        {
            if (_scannedFiles.Count == 0)
            {
                EditorGUILayout.HelpBox("点击「扫描」列出待转换文件。", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox($"共 {_scannedFiles.Count} 个文件，跳过无效目录 {_skippedFolderCount} 个。", MessageType.Info);
            _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.Height(140));
            foreach (var file in _scannedFiles)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(file, GUILayout.Height(18));
                if (GUILayout.Button("定位", GUILayout.Width(44)))
                    PingFile(file);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空"))
            {
                _folderPaths.Clear();
                ClearScan();
            }
            GUI.enabled = _folderPaths.Count > 0;
            if (GUILayout.Button("扫描"))
                Scan();
            GUI.enabled = _scannedFiles.Count > 0;
            if (GUILayout.Button("转换为 UTF-8") && EditorUtility.DisplayDialog("确认", $"转换 {_scannedFiles.Count} 个文件？", "确定", "取消"))
                ConvertAll();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        void AddFolderByDialog()
        {
            var selected = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                _folderPaths.Add(selected.Replace('\\', '/'));
                ClearScan();
            }
        }

        void PickFolder(int index)
        {
            var selected = EditorUtility.OpenFolderPanel("选择文件夹", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                _folderPaths[index] = selected.Replace('\\', '/');
                ClearScan();
            }
        }

        void Scan()
        {
            _scannedFiles.Clear();
            _skippedFolderCount = 0;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var option = _includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var raw in _folderPaths)
            {
                var folder = NormalizeFolder(raw);
                if (string.IsNullOrEmpty(folder))
                    continue;
                if (!Directory.Exists(folder))
                {
                    _skippedFolderCount++;
                    continue;
                }

                foreach (var file in Directory.GetFiles(folder, "*.cs", option))
                {
                    var normalized = file.Replace('\\', '/');
                    if (visited.Add(normalized))
                        _scannedFiles.Add(normalized);
                }
            }

            _scannedFiles.Sort(StringComparer.OrdinalIgnoreCase);
        }

        void ConvertAll()
        {
            var ok = 0;
            var fail = 0;
            foreach (var file in _scannedFiles)
            {
                try
                {
                    var content = ReadWithEncodingDetection(file);
                    File.WriteAllText(file, content, new UTF8Encoding(false));
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    Debug.LogError($"[CsUtf8] 失败: {file}\n{ex}");
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", $"成功 {ok}，失败 {fail}", "确定");
        }

        void ClearScan()
        {
            _scannedFiles.Clear();
            _skippedFolderCount = 0;
        }

        static void PingFile(string absolutePath)
        {
            var root = MGKitEditorPaths.ProjectRoot.Replace('\\', '/');
            var normalized = absolutePath.Replace('\\', '/');
            if (normalized.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                var rel = normalized.Substring(root.Length + 1);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(rel);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                    return;
                }
            }
            EditorUtility.RevealInFinder(absolutePath);
        }

        static string ReadWithEncodingDetection(string path)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0)
                return string.Empty;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                try { return Encoding.GetEncoding("GB18030").GetString(bytes); }
                catch { return Encoding.Default.GetString(bytes); }
            }
        }

        static string NormalizeFolder(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var folder = input.Trim().Replace('\\', '/');
            if (folder.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                return MGKitEditorPaths.ToFullPath(folder);
            return folder;
        }
    }
}
