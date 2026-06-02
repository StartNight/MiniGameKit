/****************************************************
 * FileName:		AdManagerEditorWindow
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.1
 * UnityVersion:	2022.3.43f1c1
 * Description:		广告管理器Editor调试窗口
 *
*****************************************************/


#if UNITY_EDITOR

using MGKit.Editor;
using UnityEditor;
using UnityEngine;
using MGKit;

public class AdManagerEditorWindow : EditorWindow
{
    private MiniGamePlatform _selectedPlatform = MiniGamePlatform.Editor;
    private bool _enableAd = true;
    private Vector2 _scrollPos;

    private string _bannerAdId = "";
    private string _interstitialAdId = "";
    private string _rewardedVideoAdId = "";
    private string _customAdId = "";

    private static AdManager _runtimeInstance;

    [MenuItem(MGKitEditorPaths.AdMenu + "广告管理器调试", false, 100)]
    public static void ShowWindow()
    {
        GetWindow<AdManagerEditorWindow>("广告管理器");
    }

    private void OnGUI()
    {
        _runtimeInstance = EditorGUIUtility.isProSkin != EditorGUIUtility.isProSkin
            ? null : FindRuntimeInstance();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("广告管理器 - Editor调试工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        DrawPlatformSection();
        EditorGUILayout.Space(10);
        DrawAdIdSection();
        EditorGUILayout.Space(10);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("请进入Play Mode后使用广告操作", MessageType.Info);
        }
        else
        {
            DrawControlSection();
            EditorGUILayout.Space(10);
            DrawStatusSection();
        }

        EditorGUILayout.EndScrollView();
    }

    private static AdManager FindRuntimeInstance()
    {
        if (!EditorApplication.isPlaying) return null;
        var found = FindObjectOfType<AdManager>();
        return found;
    }

    private void DrawPlatformSection()
    {
        EditorGUILayout.LabelField("平台设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        _selectedPlatform = (MiniGamePlatform)EditorGUILayout.EnumPopup("目标平台", _selectedPlatform);
        _enableAd = EditorGUILayout.Toggle("启用广告", _enableAd);

        EditorGUI.indentLevel--;
    }

    private void DrawAdIdSection()
    {
        EditorGUILayout.LabelField("广告位ID配置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        _bannerAdId = EditorGUILayout.TextField("Banner 广告位ID", _bannerAdId);
        _interstitialAdId = EditorGUILayout.TextField("插屏广告位ID", _interstitialAdId);
        _rewardedVideoAdId = EditorGUILayout.TextField("激励视频广告位ID", _rewardedVideoAdId);
        _customAdId = EditorGUILayout.TextField("自定义广告位ID", _customAdId);

        EditorGUI.indentLevel--;
    }

    private void DrawControlSection()
    {
        if (!EditorApplication.isPlaying) return;

        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var inst = AdManager.Instance;

        if (GUILayout.Button("初始化广告管理器", GUILayout.Height(30)))
        {
            var config = new AdConfig
            {
                CurrentPlatform = _selectedPlatform,
                EnableAd = _enableAd
            };

            if (!string.IsNullOrEmpty(_bannerAdId))
                config.SetAdUnitId(AdType.Banner, _selectedPlatform, _bannerAdId);
            if (!string.IsNullOrEmpty(_interstitialAdId))
                config.SetAdUnitId(AdType.Interstitial, _selectedPlatform, _interstitialAdId);
            if (!string.IsNullOrEmpty(_rewardedVideoAdId))
                config.SetAdUnitId(AdType.RewardedVideo, _selectedPlatform, _rewardedVideoAdId);
            if (!string.IsNullOrEmpty(_customAdId))
                config.SetAdUnitId(AdType.Custom, _selectedPlatform, _customAdId);

            inst.Initialize(config);
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("预加载全部广告", GUILayout.Height(25)))
        {
            inst.PreloadAll();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载Banner", GUILayout.Height(25)))
        {
            inst.LoadAd(AdType.Banner, _bannerAdId);
        }
        if (GUILayout.Button("展示Banner", GUILayout.Height(25)))
        {
            inst.ShowAd(AdType.Banner, _bannerAdId);
        }
        if (GUILayout.Button("隐藏Banner", GUILayout.Height(25)))
        {
            inst.HideAd(AdType.Banner, _bannerAdId);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载插屏", GUILayout.Height(25)))
        {
            inst.LoadAd(AdType.Interstitial, _interstitialAdId);
        }
        if (GUILayout.Button("展示插屏", GUILayout.Height(25)))
        {
            inst.ShowAd(AdType.Interstitial, _interstitialAdId);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载激励视频", GUILayout.Height(25)))
        {
            inst.LoadAd(AdType.RewardedVideo, _rewardedVideoAdId);
        }
        if (GUILayout.Button("展示激励视频", GUILayout.Height(25)))
        {
            inst.ShowRewardedVideo(_rewardedVideoAdId, (rewarded) =>
            {
                Debug.Log($"[Ad-Editor] 激励视频结果: {rewarded}");
            });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawStatusSection()
    {
        if (!EditorApplication.isPlaying) return;

        var inst = FindRuntimeInstance();

        EditorGUILayout.LabelField("状态信息", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (inst != null)
        {
            EditorGUILayout.LabelField("当前平台", inst.CurrentPlatform.ToString());
            EditorGUILayout.LabelField("已初始化", inst.IsInitialized ? "是" : "否");
            EditorGUILayout.LabelField("广告开关", inst.Config?.EnableAd == true ? "开启" : "关闭");
        }
        else
        {
            EditorGUILayout.LabelField("当前平台", "未创建");
            EditorGUILayout.LabelField("已初始化", "否");
        }

        EditorGUI.indentLevel--;
    }
}

#endif
