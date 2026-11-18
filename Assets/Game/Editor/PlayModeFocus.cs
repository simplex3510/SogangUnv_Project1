using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>플레이 모드에 들어가면 Game 뷰에 포커스를 줘서 클릭 없이 바로 입력을 받게 한다.</summary>
    [InitializeOnLoad]
    internal static class PlayModeFocus
    {
        static PlayModeFocus()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    // GameView는 internal 타입이라 이름으로 찾는다
                    EditorWindow.FocusWindowIfItsOpen(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                }
            };
        }
    }
}
