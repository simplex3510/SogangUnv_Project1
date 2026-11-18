using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Manager
{
    public class GameManager : ManagerBase<GameManager>
    {
        [SerializeField] private InputActionReference _escapeKey;

        private void OnEnable()
        {
            _escapeKey.action.performed += Terminate;
        }

        private void OnDisable()
        {
            _escapeKey.action.performed -= Terminate;
        }

        private void Terminate(InputAction.CallbackContext context)
        {
            Quit();
        }

        /// <summary>게임을 끝낸다. Esc와 시작 화면의 종료 버튼이 같이 쓴다.</summary>
        public void Quit()
        {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 플레이 중일 때 종료
#else
                Application.Quit(); // 실제 빌드된 앱에서 종료
#endif
        }
    }
}