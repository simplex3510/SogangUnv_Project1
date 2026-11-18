using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Manager
{
    public class GameManager : ManagerBase<GameManager>
    {
        [SerializeField] private InputActionReference _escapeKey;

        private static readonly List<EscapeHandler> EscapeHandlers = new();

        private readonly struct EscapeHandler
        {
            public readonly int Priority;
            public readonly Func<bool> TryHandle;

            public EscapeHandler(int priority, Func<bool> tryHandle)
            {
                Priority = priority;
                TryHandle = tryHandle;
            }
        }

        /// <summary>퇴근 확인창 등으로 게임이 정지된 동안 true. 조작 스크립트가 입력을 무시하는 기준이다.</summary>
        public static bool IsPaused { get; private set; }

        private void OnEnable()
        {
            _escapeKey.action.performed += OnEscape;
        }

        private void OnDisable()
        {
            _escapeKey.action.performed -= OnEscape;

            // 도메인 리로드가 꺼져 있어도 다음 플레이가 정지 상태로 시작하지 않게 한다
            SetPaused(false);
        }

        /// <summary>Esc 핸들러를 등록한다. 정적이라 종료 중에도 안전하게 해제할 수 있다(OnDisable에서 호출). 우선순위가 높은 핸들러부터 호출하고, true를 돌려주면 거기서 멈춘다.</summary>
        public static void AddEscapeHandler(int priority, Func<bool> tryHandle)
        {
            EscapeHandlers.Add(new EscapeHandler(priority, tryHandle));
            EscapeHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public static void RemoveEscapeHandler(Func<bool> tryHandle)
        {
            EscapeHandlers.RemoveAll(handler => handler.TryHandle == tryHandle);
        }

        /// <summary>게임 시간과 물리를 멈추거나 재개한다. 정지 중 입력 무시는 각 조작 스크립트가 IsPaused로 처리한다.</summary>
        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        /// <summary>게임을 끝낸다. 시작 화면의 퇴근 버튼과 퇴근 확인창의 예가 쓴다.</summary>
        public void Quit()
        {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 플레이 중일 때 종료
#else
                Application.Quit(); // 실제 빌드된 앱에서 종료
#endif
        }

        private void OnEscape(InputAction.CallbackContext context)
        {
            // 핸들러가 호출 중 등록·해제될 수 있어 복사본으로 돈다
            foreach (EscapeHandler handler in EscapeHandlers.ToArray())
            {
                if (handler.TryHandle())
                {
                    return;
                }
            }

            // 처리한 UI가 없으면(예: 시작 화면이 꺼진 상태) 기존처럼 바로 종료한다
            Quit();
        }
    }
}
