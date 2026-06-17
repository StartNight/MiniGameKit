//#if UNITY_EDITOR

//using UnityEditor;
//using UnityEditor.Build;
//using UnityEditor.Build.Reporting;
//using UnityEngine;

//namespace MGKit.Editor
//{
//    public sealed class WebGLEmscriptenArgsPreprocessor : IPreprocessBuildWithReport
//    {
//        public int callbackOrder => -1000;

//        public void OnPreprocessBuild(BuildReport report)
//        {
//            if (!IsWebGLLikeTarget(report.summary.platform))
//                return;

//            var before = PlayerSettings.WebGL.emscriptenArgs;
//            WebGLCiBuild.ApplyLinkerSafeWebGLSettings();

//            if (before != PlayerSettings.WebGL.emscriptenArgs)
//                Debug.Log("[Build] WebGL emscriptenArgs patched to export _main before BuildPlayer.");
//        }

//        static bool IsWebGLLikeTarget(BuildTarget target)
//        {
//            return target == BuildTarget.WebGL || target.ToString() == "WeixinMiniGame";
//        }
//    }
//}

//#endif