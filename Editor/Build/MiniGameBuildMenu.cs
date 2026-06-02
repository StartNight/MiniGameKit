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
using MGKit;

namespace MGKit.Editor
{
    public static class MiniGameBuildMenu
    {
#if WEIXINMINIGAME
        [MenuItem(MGKitEditorPaths.BuildMenu + "构建当前平台 (微信小游戏)", false, 55)]
        public static void BuildWeChatMiniGame()
        {
            var config = BuildConfig.Create(MiniGamePlatform.WeChatMiniGame);
            var succeeded = MiniGameBuildPipeline.RunWeChatBuild(config);
            MiniGameBuildPipeline.ShowWeChatBuildResult(succeeded);
        }
#elif DOUYINMINIGAME
        [MenuItem(MGKitEditorPaths.BuildMenu + "构建当前平台 (抖音小游戏)", false, 60)]
        public static void BuildDouyinMiniGame()
        {
            var config = BuildConfig.Create(MiniGamePlatform.DouyinMiniGame);
            var succeeded = MiniGameBuildPipeline.RunDouyinBuild(config);
            if (succeeded)
            {
                var path = BuildArtifactPaths.ResolveArtifactDirectory(MiniGamePlatform.DouyinMiniGame, config);
                EditorUtility.DisplayDialog("抖音小游戏 构建完成", $"导出目录：\n{path}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("抖音小游戏 构建失败", "请查看 Console 日志。", "确定");
            }
        }
#elif CRAZYGAMES
        [MenuItem(MGKitEditorPaths.BuildMenu + "构建当前平台 (CrazyGames - WebGL)", false, 65)]
        public static void BuildCrazyGames()
        {
            QuickBuild(BuildConfig.Create(MiniGamePlatform.WebGL));
        }
#elif UNITY_ANDROID
        [MenuItem(MGKitEditorPaths.BuildMenu + "构建当前平台 (Android)", false, 70)]
        public static void BuildAndroid()
        {
            QuickBuild(BuildConfig.Create(MiniGamePlatform.Android));
        }

        [MenuItem(MGKitEditorPaths.BuildMenu + "构建并运行当前平台 (Android)", false, 71)]
        public static void BuildAndRunAndroid()
        {
            var config = BuildConfig.Create(MiniGamePlatform.Android);
            config.AutoRun = true;
            QuickBuild(config);
        }
#elif UNITY_IOS
        [MenuItem(MGKitEditorPaths.BuildMenu + "构建当前平台 (iOS)", false, 80)]
        public static void BuildIOS()
        {
            QuickBuild(BuildConfig.Create(MiniGamePlatform.iOS));
        }
#else
        [MenuItem(MGKitEditorPaths.BuildMenu + "构建当前平台 (纯净 WebGL)", false, 65)]
        public static void BuildWebGL()
        {
            QuickBuild(BuildConfig.Create(MiniGamePlatform.WebGL));
        }
#endif

        [MenuItem(MGKitEditorPaths.BuildMenu + "诊断当前构建环境", false, 200)]
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
