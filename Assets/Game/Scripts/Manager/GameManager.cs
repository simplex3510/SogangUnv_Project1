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
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 플레이 중일 때 종료
#else
                Application.Quit(); // 실제 빌드된 앱에서 종료
#endif
        }
    }
}