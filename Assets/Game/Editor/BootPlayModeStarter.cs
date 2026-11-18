using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>열린 씬과 같은 폴더에 Boot.unity가 있으면 Play를 Boot부터 시작한다.</summary>
    [InitializeOnLoad]
    internal static class BootPlayModeStarter
    {
        static BootPlayModeStarter()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    EditorSceneManager.playModeStartScene = FindBootScene();
                }
            };
        }

        private static SceneAsset FindBootScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string path = SceneManager.GetSceneAt(i).path;
                string bootPath = Path.GetDirectoryName(path)?.Replace('\\', '/') + "/Boot.unity";
                if (path != bootPath && AssetDatabase.LoadAssetAtPath<SceneAsset>(bootPath) is SceneAsset boot)
                {
                    return boot;
                }
            }

            return null;
        }
    }
}
