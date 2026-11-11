using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>포인터를 누르고 있는 동안만 상차를 진행하도록 UI 행에 홀드 상태를 전달한다.</summary>
    public class HoldInteractButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Button _button;
        private Action _onHoldStarted;
        private Action _onHoldEnded;
        private bool _isHolding;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public bool IsHolding => _isHolding;

        public void Initialize(Action onHoldStarted, Action onHoldEnded)
        {
            _onHoldStarted = onHoldStarted;
            _onHoldEnded = onHoldEnded;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isHolding || (_button != null && _button.interactable == false))
            {
                return;
            }

            _isHolding = true;
            _onHoldStarted?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EndHold();
        }

        // 누른 채 행 밖으로 나가면 진행도를 잃는다(기획서 16.2-3과 같은 규칙)
        public void OnPointerExit(PointerEventData eventData)
        {
            EndHold();
        }

        private void OnDisable()
        {
            EndHold();
        }

        /// <summary>상호작용이 끝났을 때 행 쪽에서도 홀드를 종료한다.</summary>
        public void EndHold()
        {
            if (_isHolding == false)
            {
                return;
            }

            _isHolding = false;
            _onHoldEnded?.Invoke();
        }
    }
}
