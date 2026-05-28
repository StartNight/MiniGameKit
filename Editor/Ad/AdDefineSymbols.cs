/****************************************************
 * FileName:		AdDefineSymbols
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.1
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告平台宏定义管理工具（互斥）
 *
*****************************************************/

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using MiniGameKit.Editor;
using UnityEditor;
using UnityEngine;

public static class AdDefineSymbols
{
    private const string WECHAT_SYMBOL = "WEIXINMINIGAME";
    private const string DOUYIN_SYMBOL = "DOUYINMINIGAME";

    private static readonly Dictionary<AdPlatform, string[]> PlatformSymbols = new Dictionary<AdPlatform, string[]>()
    {
        { AdPlatform.WeChatMiniGame, new[] { WECHAT_SYMBOL } },
        { AdPlatform.DouyinMiniGame, new[] { DOUYIN_SYMBOL } },
    };

    private static readonly BuildTargetGroup[] SymbolTargets =
    {
        BuildTargetGroup.Standalone,
        BuildTargetGroup.WebGL,
        BuildTargetGroup.Android,
        BuildTargetGroup.iOS,
    };

    [MenuItem(MiniGameKitEditorPaths.AdPlatformMenu + "启用微信小游戏广告", false, 200)]
    public static void EnableWeChat() => SetExclusivePlatform(AdPlatform.WeChatMiniGame);

    [MenuItem(MiniGameKitEditorPaths.AdPlatformMenu + "禁用微信小游戏广告", false, 201)]
    public static void DisableWeChat() => TogglePlatformSymbol(AdPlatform.WeChatMiniGame, false);

    [MenuItem(MiniGameKitEditorPaths.AdPlatformMenu + "启用抖音小游戏广告", false, 210)]
    public static void EnableDouyin() => SetExclusivePlatform(AdPlatform.DouyinMiniGame);

    [MenuItem(MiniGameKitEditorPaths.AdPlatformMenu + "禁用抖音小游戏广告", false, 211)]
    public static void DisableDouyin() => TogglePlatformSymbol(AdPlatform.DouyinMiniGame, false);

    [MenuItem(MiniGameKitEditorPaths.AdPlatformMenu + "清除所有小游戏宏", false, 250)]
    public static void ClearAllMiniGameSymbols()
    {
        foreach (var target in SymbolTargets)
            RemoveAllMiniGameSymbols(target);

        Debug.Log("[AdDefineSymbols] 已清除所有小游戏平台宏（WEIXINMINIGAME / DOUYINMINIGAME）");
    }

    [MenuItem(MiniGameKitEditorPaths.AdPlatformMenu + "查看当前宏定义", false, 300)]
    public static void ShowCurrentSymbols()
    {
        var targets = Enum.GetValues(typeof(BuildTargetGroup))
            .Cast<BuildTargetGroup>()
            .Where(t => t != BuildTargetGroup.Unknown && !Attribute.IsDefined(t.GetType().GetField(t.ToString()), typeof(ObsoleteAttribute)));

        foreach (var target in targets)
        {
            try
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
                Debug.Log($"[{target}] {symbols}");
            }
            catch { }
        }
    }

    static void SetExclusivePlatform(AdPlatform platform)
    {
        foreach (var target in SymbolTargets)
            RemoveAllMiniGameSymbols(target);

        TogglePlatformSymbol(platform, true);
        Debug.Log($"[AdDefineSymbols] 已互斥启用 {platform}（其它小游戏宏已清除）");
    }

    static void RemoveAllMiniGameSymbols(BuildTargetGroup target)
    {
        try
        {
            var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            var list = new HashSet<string>(current.Split(';'));
            list.Remove(WECHAT_SYMBOL);
            list.Remove(DOUYIN_SYMBOL);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", list));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdDefineSymbols] 清除 {target} 宏失败: {e.Message}");
        }
    }

    private static void TogglePlatformSymbol(AdPlatform platform, bool enable)
    {
        if (!PlatformSymbols.TryGetValue(platform, out var symbols)) return;

        foreach (var target in SymbolTargets)
        {
            try
            {
                var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
                var currentList = new HashSet<string>(current.Split(';'));

                if (enable)
                {
                    foreach (var other in PlatformSymbols.Values.SelectMany(s => s))
                        currentList.Remove(other);
                }

                foreach (var symbol in symbols)
                {
                    if (enable)
                        currentList.Add(symbol);
                    else
                        currentList.Remove(symbol);
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, string.Join(";", currentList));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AdDefineSymbols] 设置 {target} 宏失败: {e.Message}");
            }
        }

        var action = enable ? "启用" : "禁用";
        Debug.Log($"[AdDefineSymbols] {action} {platform} 平台宏定义完成");
    }
}

#endif
