using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MGKit.Editor
{
    public static class TrueShadowStripper
    {
        [MenuItem(MGKitEditorPaths.BuildOptimizeMenu + "剥离 TrueShadow 并应用原生阴影 (微信)", false, 10)]
        public static void StripTrueShadows()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });
            int removedCount = 0;
            int fallbackAddedCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // 我们需要修改预制体，所以使用 PrefabUtility
                GameObject instance = PrefabUtility.LoadPrefabContents(path);
                var shadows = instance.GetComponentsInChildren<Component>(true);
                bool modified = false;

                foreach (var s in shadows)
                {
                    if (s == null) continue;
                    if (s.GetType().Name.Contains("TrueShadow"))
                    {
                        GameObject go = s.gameObject;
                        Object.DestroyImmediate(s);
                        removedCount++;
                        modified = true;

                        // 添加 Unity 原生 Shadow 作为轻量级回退
                        if (go.GetComponent<Shadow>() == null)
                        {
                            var shadow = go.AddComponent<Shadow>();
                            shadow.effectColor = new Color(0, 0, 0, 0.5f);
                            shadow.effectDistance = new Vector2(2, -2);
                            fallbackAddedCount++;
                        }
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }
                PrefabUtility.UnloadPrefabContents(instance);
            }

            Debug.Log($"[TrueShadowStripper] 已剥离 {removedCount} 个 TrueShadow 组件，并添加了 {fallbackAddedCount} 个原生 Shadow 回退。");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}