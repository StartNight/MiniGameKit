/****************************************************
 * FileName:		ToolbarExtender
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * CreateTime:		2026-06-01 10:00:00
 * Version:			1.2
 * UnityVersion:	2022.3.43f1c1
 * Description:		向 Unity 编辑器工具栏注入自定义 GUI
 *
 *****************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MGKit.Editor
{
    /// <summary>
    /// 通过 Unity 内部 Toolbar 类的 rootVisualElement 属性安全注入自定义 IMGUI 控件。
    /// 兼容 Unity 2021.2+ / 2022.x 系列。
    /// </summary>
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        public static readonly List<Action> RightToolbarGUI = new List<Action>();
        private static bool _initialized;

        static ToolbarExtender()
        {
            _initialized = false;
            EditorApplication.update -= TryAttach;
            EditorApplication.update += TryAttach;
        }

        private static void TryAttach()
        {
            if (_initialized) return;

            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var toolbarType = asm.GetType("UnityEditor.Toolbar");
                if (toolbarType == null)
                {
                    Debug.LogWarning("[ToolbarExtender] 未找到 UnityEditor.Toolbar 类型");
                    return;
                }

                var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
                if (toolbars == null || toolbars.Length == 0) return;

                var toolbar = toolbars[0];

                // 获取 rootVisualElement — 先试属性再试字段
                VisualElement root = null;

                var rootProp = toolbarType.GetProperty("rootVisualElement",
                    BindingFlags.Public | BindingFlags.Instance);
                if (rootProp != null)
                    root = rootProp.GetValue(toolbar) as VisualElement;

                if (root == null)
                {
                    var rootField = toolbarType.GetField("m_Root",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rootField != null)
                        root = rootField.GetValue(toolbar) as VisualElement;
                }

                if (root == null) return;

                if (root.Q("MiniGamePlatformSwitcher") != null)
                {
                    _initialized = true;
                    EditorApplication.update -= TryAttach;
                    return;
                }

                var imguiContainer = new IMGUIContainer(OnGUI)
                {
                    name = "MiniGamePlatformSwitcher",
                    style =
                    {
                        flexGrow = 0,
                        flexShrink = 0,
                        width = 160,
                        height = 22,
                        marginLeft = 8,
                        alignSelf = Align.Center
                    }
                };

                bool injected = false;

                // 优先：ToolbarZonePlayMode 右侧同级
                var playZone = root.Q("ToolbarZonePlayMode");
                if (playZone?.parent != null)
                {
                    int idx = playZone.parent.IndexOf(playZone);
                    playZone.parent.Insert(idx + 1, imguiContainer);
                    injected = true;
                }

                // 备用 A：ToolbarZoneRightAlign 最左侧
                if (!injected)
                {
                    var rightZone = root.Q("ToolbarZoneRightAlign");
                    if (rightZone != null)
                    {
                        rightZone.Insert(0, imguiContainer);
                        injected = true;
                    }
                }

                // 备用 B：根节点末尾
                if (!injected)
                {
                    root.Add(imguiContainer);
                    injected = true;
                }

                if (injected)
                {
                    _initialized = true;
                    EditorApplication.update -= TryAttach;
                    Debug.Log("[ToolbarExtender] 已成功注入 Platform Switcher 到工具栏");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ToolbarExtender] 注入失败: {ex}");
                _initialized = true;
                EditorApplication.update -= TryAttach;
            }
        }

        private static void OnGUI()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in RightToolbarGUI)
            {
                handler?.Invoke();
            }
            GUILayout.EndHorizontal();
        }
    }
}