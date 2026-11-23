#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Hackaton.Multiplayer
{
    [InitializeOnLoad]
    internal static class MenuStartScene
    {
        static MenuStartScene() => EditorApplication.delayCall += () =>
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MenuPrincipal.unity");
            if (menu) EditorSceneManager.playModeStartScene = menu;
        };
    }
}
#endif
