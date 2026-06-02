using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MGKit.Editor
{
    /// <summary>
    /// 根据 CSV 中英译文自动为 Text / TMP 挂载 I2 Localize 并绑定 Term。
    /// </summary>
    public class I2LocalizeAutoBindWindow : EditorWindow
    {
        private string _csvPath;

        [MenuItem(MGKitEditorPaths.LocalizationMenu + "自动绑定 Localize 组件", false, 101)]
        public static void Open()
        {
            var w = GetWindow<I2LocalizeAutoBindWindow>("I2 自动绑定");
            w._csvPath = MGKitEditorPaths.I2CsvAssetPath;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CSV 路径", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _csvPath = EditorGUILayout.TextField(_csvPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50)))
            {
                var p = EditorUtility.OpenFilePanel("选择 I2 CSV", "Assets", "csv");
                if (!string.IsNullOrEmpty(p))
                    _csvPath = "Assets" + p.Replace(Application.dataPath, "").Replace('\\', '/');
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "扫描全部 Scene 与 Prefab：若 Text/TMP 内容与 CSV 中/英译文一致，则自动添加 I2.Loc.Localize 并设置 Term。",
                MessageType.Info);

            if (GUILayout.Button("开始自动绑定", GUILayout.Height(36)))
                RunAutoBind(_csvPath);
        }

        public static void RunAutoBind(string csvAssetPath)
        {
            var localizeType = ReflectionTypeUtility.FindType("I2.Loc.Localize");
            if (localizeType == null)
            {
                Debug.LogError("[I2AutoBind] 找不到 I2.Loc.Localize。");
                return;
            }

            var textToKey = LoadTextToKeyMap(csvAssetPath);
            if (textToKey == null)
                return;

            var bindCount = 0;
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar("扫描 Prefab", path, (float)i / prefabGuids.Length);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && ProcessGameObject(prefab, textToKey, localizeType))
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                    bindCount++;
                }
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (var i = 0; i < sceneGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar("扫描 Scene", path, (float)i / sceneGuids.Length);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var changed = false;
                foreach (var root in scene.GetRootGameObjects())
                    changed |= ProcessGameObject(root, textToKey, localizeType);

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    bindCount++;
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("完成", $"共修改 {bindCount} 个预制体/场景。", "确定");
        }

        private static Dictionary<string, string> LoadTextToKeyMap(string csvAssetPath)
        {
            var fullPath = MGKitEditorPaths.ToFullPath(csvAssetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError("[I2AutoBind] CSV 不存在：" + fullPath);
                return null;
            }

            var map = new Dictionary<string, string>();
            var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var cols = CsvLineParser.Parse(line);
                if (cols.Length < 4)
                    continue;

                var key = cols[0].Trim();
                var zh = cols[3].Replace("\\n", "\n").Trim();
                var en = cols.Length > 4 ? cols[4].Replace("\\n", "\n").Trim() : "";

                if (string.IsNullOrEmpty(key))
                    continue;

                if (!string.IsNullOrEmpty(zh) && !map.ContainsKey(zh))
                    map[zh] = key;
                if (!string.IsNullOrEmpty(en) && !map.ContainsKey(en))
                    map[en] = key;
            }

            Debug.Log($"[I2AutoBind] 加载 {map.Count} 条文本映射。");
            return map;
        }

        private static bool ProcessGameObject(GameObject go, Dictionary<string, string> textToKey, Type localizeType)
        {
            var changed = false;
            foreach (var tr in go.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject) > 0)
                    changed = true;
            }

            foreach (var t in go.GetComponentsInChildren<Text>(true))
                changed |= TryBind(t.gameObject, t.text, textToKey, localizeType);

            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
                changed |= TryBind(t.gameObject, t.text, textToKey, localizeType);

            return changed;
        }

        private static bool TryBind(GameObject go, string text, Dictionary<string, string> textToKey, Type localizeType)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            if (!textToKey.TryGetValue(text, out var key))
                return false;

            var comp = go.GetComponent(localizeType) ?? go.AddComponent(localizeType);
            var termField = localizeType.GetField("mTerm",
                BindingFlags.Public | BindingFlags.Instance);
            if (termField == null)
                return false;

            var current = termField.GetValue(comp) as string;
            if (current == key)
                return false;

            termField.SetValue(comp, key);
            EditorUtility.SetDirty(go);
            return true;
        }
    }
}