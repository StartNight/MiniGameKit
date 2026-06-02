using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MGKit.Editor
{
    /// <summary>
    /// 将 Prefab 内 Unity UI Text 批量替换为 TextMeshProUGUI。
    /// </summary>
    public class TextToTextMeshProConverterWindow : EditorWindow
    {
        private readonly List<string> _prefabPaths = new List<string>();
        private readonly List<string> _scriptSearchFolders = new List<string> { "Assets/Scripts" };

        private Vector2 _scroll;
        private TMP_FontAsset _targetFont;
        private string _folderPath = "Assets";
        private bool _autoFixLinkedScripts = true;
        private int _modeIndex;

        [MenuItem(MGKitEditorPaths.UiMenu + "Text 转 TextMeshPro", false, 150)]
        public static void Open() => GetWindow<TextToTextMeshProConverterWindow>("Text → TMP");

        private void OnGUI()
        {
            _targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP 字体", _targetFont, typeof(TMP_FontAsset), false);
            _autoFixLinkedScripts = EditorGUILayout.Toggle("同步修改关联脚本类型", _autoFixLinkedScripts);

            EditorGUILayout.LabelField("关联脚本搜索目录", EditorStyles.boldLabel);
            for (var i = 0; i < _scriptSearchFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _scriptSearchFolders[i] = EditorGUILayout.TextField(_scriptSearchFolders[i]);
                if (GUILayout.Button("-", GUILayout.Width(24)) && _scriptSearchFolders.Count > 1)
                    _scriptSearchFolders.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("添加脚本搜索目录"))
                _scriptSearchFolders.Add("Assets/Scripts");

            _modeIndex = GUILayout.Toolbar(_modeIndex, new[] { "按文件夹批量", "按预制体列表" });

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_modeIndex == 0)
            {
                _folderPath = EditorGUILayout.TextField("Prefab 根目录", _folderPath);
                if (GUILayout.Button("加载目录下全部 Prefab"))
                    LoadPrefabsFromFolder(_folderPath);
            }

            DrawPrefabList();
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("执行替换", GUILayout.Height(32)))
                ConvertAll();
        }

        private void DrawPrefabList()
        {
            for (var i = _prefabPaths.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_prefabPaths[i]);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                    _prefabPaths.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void LoadPrefabsFromFolder(string assetFolder)
        {
            _prefabPaths.Clear();
            var full = MGKitEditorPaths.ToFullPath(assetFolder);
            if (!Directory.Exists(full))
                return;

            foreach (var file in Directory.GetFiles(full, "*.prefab", SearchOption.AllDirectories))
            {
                var idx = file.IndexOf("Assets");
                if (idx >= 0)
                    _prefabPaths.Add(file.Substring(idx).Replace('\\', '/'));
            }
        }

        private void ConvertAll()
        {
            if (_prefabPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先加载或添加 Prefab。", "确定");
                return;
            }

            if (_targetFont == null)
            {
                EditorUtility.DisplayDialog("提示", "请选择 TMP 字体。", "确定");
                return;
            }

            for (var i = 0; i < _prefabPaths.Count; i++)
            {
                ConvertPrefab(_prefabPaths[i]);
                EditorUtility.DisplayProgressBar("替换中", _prefabPaths[i], (float)i / _prefabPaths.Count);
            }

            EditorUtility.ClearProgressBar();

            if (_autoFixLinkedScripts)
                PatchLinkedScripts();

            AssetDatabase.Refresh();
        }

        private void ConvertPrefab(string assetPath)
        {
            var root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null)
                return;

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var rect = text.rectTransform;
                var size = rect.sizeDelta;
                var content = text.text;
                var color = text.color;
                var fontSize = text.fontSize;
                var fontStyle = text.fontStyle;
                var anchor = text.alignment;
                var rich = text.supportRichText;
                var hWrap = text.horizontalOverflow;
                var vWrap = text.verticalOverflow;
                var raycast = text.raycastTarget;

                DestroyImmediate(text);

                var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.rectTransform.sizeDelta = size;
                tmp.text = content;
                tmp.color = color;
                tmp.fontSize = fontSize;
                tmp.font = _targetFont;
                tmp.fontStyle = fontStyle == FontStyle.BoldAndItalic ? FontStyles.Bold : (FontStyles)fontStyle;
                tmp.alignment = MapAlignment(anchor);
                tmp.richText = rich;
                tmp.enableWordWrapping = vWrap != VerticalWrapMode.Overflow
                    ? hWrap != HorizontalWrapMode.Overflow
                    : true;
                if (vWrap == VerticalWrapMode.Overflow)
                    tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.raycastTarget = raycast;
            }

            PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var ok);
            PrefabUtility.UnloadPrefabContents(root);
            if (!ok)
                Debug.LogError("[Text→TMP] 保存失败：" + assetPath);
        }

        private static TextAlignmentOptions MapAlignment(TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Midline,
            TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };

        private void PatchLinkedScripts()
        {
            var guids = new HashSet<string>();
            foreach (var prefabPath in _prefabPaths)
            {
                var name = Path.GetFileNameWithoutExtension(prefabPath);
                var found = AssetDatabase.FindAssets($"{name} t:Script", _scriptSearchFolders.ToArray());
                foreach (var g in found)
                    guids.Add(g);
            }

            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var text = File.ReadAllText(path);
                text = text.Replace("<Text>", "<TMPro.TextMeshProUGUI>");
                text = text.Replace(" Text ", " TMPro.TextMeshProUGUI ");
                File.WriteAllText(path, text, System.Text.Encoding.UTF8);
            }
        }
    }
}