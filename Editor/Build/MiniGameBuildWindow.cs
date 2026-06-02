/****************************************************
 * FileName:		MiniGameBuildWindow
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			1.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		小游戏统一构建窗口，参数配置+构建+自动化复用
 *
*****************************************************/

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using MGKit;

namespace MGKit.Editor
{
    public class MiniGameBuildWindow : EditorWindow
    {
        private MiniGamePlatform _platform = MiniGamePlatform.WebGL;
        private bool _useWeChatProvider = false;
        private bool _applyWebGLOptimizations = true;
        private bool _buildAddressables = false;
        private bool _switchBuildTarget = true;
        private bool _developmentBuild = false;
        private bool _autoRun = false;
        private string _outputPath = "";
        private Vector2 _scrollPos;
        private bool _isBuilding;
        private string _buildLog = "";
        private Vector2 _logScrollPos;

        private static readonly Dictionary<MiniGamePlatform, string> DefaultOutputDirs = new Dictionary<MiniGamePlatform, string>()
        {
            { MiniGamePlatform.WeChatMiniGame, "build/WeChatMiniGame" },
            { MiniGamePlatform.DouyinMiniGame, "build/DouyinMiniGame" },
            { MiniGamePlatform.WebGL, "build/WebGL" },
            { MiniGamePlatform.Android, "build/Android" },
            { MiniGamePlatform.iOS, "build/iOS" }
        };

        [MenuItem(MGKitEditorPaths.BuildMenu + "构建窗口", false, 0)]
        public static void ShowWindow()
        {
            GetWindow<MiniGameBuildWindow>("小游戏构建");
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("小游戏构建工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawPlatformSection();
            EditorGUILayout.Space(8);
#if UNITY_ADDRESSABLES
            DrawProviderSection();
            EditorGUILayout.Space(8);
#else
            EditorGUILayout.HelpBox("未安装 com.unity.addressables，Addressables 相关选项已隐藏。", MessageType.Info);
            EditorGUILayout.Space(8);
#endif
            DrawOutputSection();
            EditorGUILayout.Space(8);
            DrawBuildOptionsSection();
            EditorGUILayout.Space(8);
            DrawBuildButton();
            EditorGUILayout.Space(8);
            DrawEnvironmentInfo();
            EditorGUILayout.Space(8);
            DrawLogSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPlatformSection()
        {
            EditorGUILayout.LabelField("目标平台", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var newPlatform = (MiniGamePlatform)EditorGUILayout.EnumPopup("构建平台", _platform);
            if (newPlatform != _platform)
            {
                _platform = newPlatform;
                ApplyPlatformDefaults();
            }

            var currentTarget = EditorUserBuildSettings.activeBuildTarget;
            var expectedTarget = BuildConfig.Create(_platform).GetBuildTarget();
            if (currentTarget != expectedTarget)
            {
                EditorGUILayout.HelpBox($"当前激活平台为 {currentTarget}，需要切换到 {expectedTarget}", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawProviderSection()
        {
            EditorGUILayout.LabelField("Addressables Provider", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _buildAddressables = EditorGUILayout.Toggle("构建 Addressables", _buildAddressables);

            if (_platform == MiniGamePlatform.WeChatMiniGame)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("使用微信 Provider", true);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox(
                    "微信小游戏「开始构建」等同于插件「生成并转换」，将自动强制使用 WXAssetBundleProvider + WXBundledAssetProvider。\n" +
                    "「使用微信 Provider」选项仅影响「仅构建 Addressables」和「仅切换 Provider」操作。",
                    MessageType.Info);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(!_buildAddressables);
                _useWeChatProvider = EditorGUILayout.Toggle("使用微信 Provider", _useWeChatProvider);
                EditorGUI.EndDisabledGroup();

                if (_useWeChatProvider)
                {
                    EditorGUILayout.HelpBox("将使用 WXAssetBundleProvider + WXBundledAssetProvider", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("将使用 Unity 默认 AssetBundleProvider + BundledAssetProvider", MessageType.Info);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawOutputSection()
        {
            EditorGUILayout.LabelField("输出路径", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (_platform == MiniGamePlatform.WeChatMiniGame)
            {
                EditorGUILayout.HelpBox(
                    "微信小游戏的导出路径由微信插件配置决定（「微信小游戏 → 转换小游戏」面板中的「导出路径」），此处路径设置不生效。",
                    MessageType.Warning);
            }
            else
            {
                var defaultDir = DefaultOutputDirs.ContainsKey(_platform)
                    ? DefaultOutputDirs[_platform]
                    : "build";

                EditorGUILayout.LabelField("默认目录", defaultDir);

                EditorGUILayout.BeginHorizontal();
                _outputPath = EditorGUILayout.TextField("自定义路径", _outputPath);
                if (GUILayout.Button("浏览", GUILayout.Width(50)))
                {
                    var projectRoot = Path.GetDirectoryName(Application.dataPath);
                    var selected = EditorUtility.OpenFolderPanel(
                        "选择构建输出目录",
                        string.IsNullOrEmpty(_outputPath) ? Path.Combine(projectRoot, defaultDir) : _outputPath,
                        "");
                    if (!string.IsNullOrEmpty(selected))
                        _outputPath = selected;
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("重置为默认路径", GUILayout.Height(20)))
                {
                    _outputPath = "";
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBuildOptionsSection()
        {
            EditorGUILayout.LabelField("构建选项", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _switchBuildTarget = EditorGUILayout.Toggle("自动切换平台", _switchBuildTarget);
            _developmentBuild = EditorGUILayout.Toggle("Development Build", _developmentBuild);

            if (_platform == MiniGamePlatform.Android)
            {
                _autoRun = EditorGUILayout.Toggle("构建并运行", _autoRun);
            }

            var isWebGL = _platform == MiniGamePlatform.WeChatMiniGame
                || _platform == MiniGamePlatform.DouyinMiniGame
                || _platform == MiniGamePlatform.WebGL;

            if (isWebGL)
            {
                if (_platform == MiniGamePlatform.WeChatMiniGame)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Toggle("应用 WebGL 发布优化", false);
                    EditorGUI.EndDisabledGroup();
                    _applyWebGLOptimizations = false;
                    EditorGUILayout.HelpBox(
                        "微信构建不使用「WebGL 发布优化」（会关闭 Debug Symbols，导致 preprocessSymbols 失败）。\n" +
                        "浏览器 H5 包请在「WebGL」平台下单独构建并开启该选项。",
                        MessageType.Info);
                }
                else
                {
                    _applyWebGLOptimizations = EditorGUILayout.Toggle("应用 WebGL 发布优化", _applyWebGLOptimizations);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBuildButton()
        {
            EditorGUI.BeginDisabledGroup(_isBuilding);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("开始构建", GUILayout.Height(35)))
            {
                StartBuild();
            }

#if UNITY_ADDRESSABLES
            if (GUILayout.Button("仅构建 Addressables", GUILayout.Height(35)))
            {
                BuildAddressablesOnly();
            }
#endif

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

#if UNITY_ADDRESSABLES
            if (GUILayout.Button("仅切换 Provider", GUILayout.Height(25)))
            {
                SwitchProviderOnly();
            }
#endif

            if (GUILayout.Button("诊断环境", GUILayout.Height(25)))
            {
                _buildLog = "";
                MiniGameBuildPipeline.DiagnoseEnvironment();
                AppendLog("已输出诊断信息到Console");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawEnvironmentInfo()
        {
            EditorGUILayout.LabelField("环境信息", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("当前平台", EditorUserBuildSettings.activeBuildTarget.ToString());
            EditorGUILayout.LabelField("产品名", PlayerSettings.productName);
            EditorGUILayout.LabelField("包名", PlayerSettings.applicationIdentifier);
            EditorGUILayout.LabelField("构建场景数", EditorBuildSettings.scenes.Length.ToString());

            var enabledScenes = 0;
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) enabledScenes++;
            EditorGUILayout.LabelField("启用场景数", enabledScenes.ToString());

            EditorGUI.indentLevel--;
        }

        private void DrawLogSection()
        {
            EditorGUILayout.LabelField("构建日志", EditorStyles.boldLabel);

            if (GUILayout.Button("清空日志", GUILayout.Height(20)))
            {
                _buildLog = "";
            }

            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(150));
            EditorGUILayout.TextArea(_buildLog, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private void ApplyPlatformDefaults()
        {
            var config = BuildConfig.Create(_platform);
            _useWeChatProvider = config.UseWeChatProvider;
            _applyWebGLOptimizations = config.ApplyWebGLOptimizations;
            _outputPath = "";
        }

        private BuildConfig CreateBuildConfig()
        {
            var config = BuildConfig.Create(_platform);
            config.UseWeChatProvider = _useWeChatProvider;
            config.ApplyWebGLOptimizations = _applyWebGLOptimizations;
            config.BuildAddressables = _buildAddressables;
            config.SwitchBuildTarget = _switchBuildTarget;
            config.DevelopmentBuild = _developmentBuild;
            config.AutoRun = _autoRun;
            config.OutputPath = _outputPath;
            return config;
        }

        private void StartBuild()
        {
            var config = CreateBuildConfig();
            var label = config.GetPlatformLabel();

            _buildLog = "";
            _isBuilding = true;
            AppendLog($"===== 开始构建: {label} =====");

            MiniGameBuildPipeline.OnLog += OnBuildLog;
            MiniGameBuildPipeline.OnProgress += OnBuildProgress;

            try
            {
                if (config.Platform == MiniGamePlatform.WeChatMiniGame)
                {
                    // 微信小游戏：等同于插件「生成并转换」
                    var succeeded = MiniGameBuildPipeline.RunWeChatBuild(config);
                    MiniGameBuildPipeline.ShowWeChatBuildResult(succeeded);
                }
                else
                {
                    var report = MiniGameBuildPipeline.Run(config);
                    MiniGameBuildPipeline.ShowBuildResult(label, report);
                }
            }
            finally
            {
                MiniGameBuildPipeline.OnLog -= OnBuildLog;
                MiniGameBuildPipeline.OnProgress -= OnBuildProgress;
                _isBuilding = false;
            }
        }

#if UNITY_ADDRESSABLES
        private void BuildAddressablesOnly()
        {
            _buildLog = "";
            AppendLog("===== 仅构建 Addressables =====");

            if (!AddressablesWeChatBuildMenu.ApplyProviders(weChat: _useWeChatProvider))
            {
                AppendLog("[ERROR] 切换Provider失败");
                return;
            }

            if (!AddressablesWeChatBuildMenu.BuildAddressablesContent())
            {
                AppendLog("[ERROR] Addressables构建失败");
                return;
            }

            AppendLog("Addressables构建成功");
        }
#endif

#if UNITY_ADDRESSABLES
        private void SwitchProviderOnly()
        {
            _buildLog = "";
            var mode = _useWeChatProvider ? "微信" : "默认";
            AppendLog($"切换到{mode}Provider...");

            if (AddressablesWeChatBuildMenu.ApplyProviders(weChat: _useWeChatProvider))
            {
                AppendLog($"已切换为{mode}Provider");
            }
            else
            {
                AppendLog("[ERROR] 切换Provider失败");
            }
        }
#endif

        private void OnBuildLog(string message)
        {
            AppendLog(message);
        }

        private void OnBuildProgress(float progress, string step)
        {
            AppendLog($"[{progress:P0}] {step}");
            if (EditorUtility.DisplayCancelableProgressBar("构建中...", step, progress))
            {
                EditorUtility.ClearProgressBar();
                AppendLog("构建已被用户取消");
            }
        }

        private void AppendLog(string message)
        {
            _buildLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            _logScrollPos = new Vector2(0, float.MaxValue);
            Repaint();
        }
    }
}

#endif
