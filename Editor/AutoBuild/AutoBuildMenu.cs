using UnityEditor;

namespace MGKit.Editor.AutoBuild
{
    public static class AutoBuildMenu
    {
        [MenuItem("Tools/自动构建/更新产物子仓库 (Submodule Update)", priority = 200)]
        public static void UpdateSubmoduleMenu()
        {
            SubRepoUpdater.UpdateSubmodule();
        }
    }
}
