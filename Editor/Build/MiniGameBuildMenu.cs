/****************************************************
 * FileName:		MiniGameBuildMenu
 * CompanyName:		苏州微游科技有限公司
 * Author:			Felix/李康康
 * Email:			kangkang.li@outlook.com
 * CreateTime:		2026-05-18 10:00:00
 * Version:			2.0
 * UnityVersion:	2022.3.43f1c1
 * Description:		小游戏构建快捷菜单入口，复用MiniGameBuildPipeline
 *
*****************************************************/

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniGameKit.Editor
{
    public static class MiniGameBuildMenu
    {
        [MenuItem(MiniGameKitEditorPaths.BuildWeChatMenu + "本地构建", false, 55)]
        public static void BuildWeChatMiniGame()
        {
            var config = BuildConfig.Create(BuildPlatform.WeChatMiniGame);
            var succeeded = MiniGameBuildPipeline.RunWeChatBuild(config);
            MiniGameBuildPipeline.ShowWeChatBuildResult(succeeded);
        }

        [MenuItem(MiniGameKitEditorPaths.BuildMenu + "抖音小游戏/本地构建", false, 60)]
        public static void BuildDouyinMiniGame()
        {
            var config = BuildConfig.Create(BuildPlatform.DouyinMiniGame);
            var succeeded = MiniGameBuildPipeline.RunDouyinBuild(config);
            if (succeeded)
            {
                var path = BuildArtifactPaths.ResolveArtifactDirectory(BuildPlatform.DouyinMiniGame, config);
                EditorUtility.DisplayDialog("抖音小游戏 构建完成", $"导出目录：\n{path}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("抖音小游戏 构建失败", "请查看 Console 日志。", "确定");
            }
        }

        [MenuItem(MiniGameKitEditorPaths.BuildMenu + "WebGL/本地构建", false, 65)]
        public static void BuildWebGL()
        {
            QuickBuild(BuildConfig.Create(BuildPlatform.WebGL));
        }

        [MenuItem(MiniGameKitEditorPaths.BuildMenu + "Android/本地构建", false, 70)]
        public static void BuildAndroid()
        {
            QuickBuild(BuildConfig.Create(BuildPlatform.Android));
        }

        [MenuItem(MiniGameKitEditorPaths.BuildMenu + "Android/构建并运行", false, 71)]
        public static void BuildAndRunAndroid()
        {
            var config = BuildConfig.Create(BuildPlatform.Android);
            config.AutoRun = true;
            QuickBuild(config);
        }

        [MenuItem(MiniGameKitEditorPaths.BuildMenu + "iOS/本地构建", false, 80)]
        public static void BuildIOS()
        {
            QuickBuild(BuildConfig.Create(BuildPlatform.iOS));
        }

        [MenuItem(MiniGameKitEditorPaths.BuildMenu + "诊断当前构建环境", false, 200)]
        public static void DiagnoseBuildEnvironment()
        {
            MiniGameBuildPipeline.DiagnoseEnvironment();
        }

        static void QuickBuild(BuildConfig config)
        {
            var label = config.GetPlatformLabel();
            var report = MiniGameBuildPipeline.Run(config);
            MiniGameBuildPipeline.ShowBuildResult(label, report);
        }
    }
}

#endif
