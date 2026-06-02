using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MGKit.Editor
{
    /// <summary>
    /// 关闭 UI 预制体上无交互需求 Graphic 的 RaycastTarget，减少射线检测开销。
    /// </summary>
    public static class UIRaycastOptimizeTool
    {
        [MenuItem(MGKitEditorPaths.UiMenu + "关闭无用 RaycastTarget", false, 20)]
        public static void DisableUselessRaycastTargets()
        {
            var root = MGKitEditorPaths.UiPrefabSearchRoot;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { root });
            var changedCount = 0;
            var modifiedPrefabs = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                var modified = false;
                foreach (var g in prefab.GetComponentsInChildren<Graphic>(true))
                {
                    if (g.raycastTarget && !NeedsRaycast(g.gameObject))
                    {
                        g.raycastTarget = false;
                        modified = true;
                        changedCount++;
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                    modifiedPrefabs++;
                }
            }

            Debug.Log($"[UIRaycastOptimize] 已处理 {modifiedPrefabs} 个预制体，关闭 {changedCount} 个 RaycastTarget。");
        }

        static bool NeedsRaycast(GameObject go) =>
            go.GetComponent<Button>() != null ||
            go.GetComponent<Toggle>() != null ||
            go.GetComponent<ScrollRect>() != null ||
            go.GetComponent<InputField>() != null ||
            go.GetComponent<TMP_InputField>() != null ||
            go.GetComponent<EventTrigger>() != null;
    }
}
