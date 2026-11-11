using UnityEngine;

using TMPro;

using Game.UI;
using Game.Interaction;

namespace Game.Player.Interaction
{
    [RequireComponent(typeof(PlayerInteractionController))]
    /// <summary>상호작용 종류에 맞는 프롬프트만 켜고 나머지는 끈다. 막힌 상호작용은 사유 문구로 잠시 알린다.</summary>
    public class PlayerUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractionController _interactionController;

        [Header("UIs")]
        [SerializeField] private PlayerObjectUI[] _interactionUIs;

        [Header("Blocked Message")]
        [SerializeField] private TMP_Text _messageText;
        [Tooltip("사유 문구를 표시하는 시간(초)")]
        [SerializeField, Min(0f)] private float _messageDuration = 1.5f;

        private float _messageTimer;

        private void Awake()
        {
            if (_interactionController == null)
            {
                _interactionController = GetComponent<PlayerInteractionController>();
                Debug.LogWarning($"PlayerInteractionController is not assigned in {gameObject.name}");
                if (_interactionController == null)
                {
                    Debug.LogError($"PlayerInteractionController is missing in {gameObject.name}");
                }
            }

            if (_messageText != null)
            {
                _messageText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            _interactionController.InteractionContextChanged += RefreshInteractionUI;
            _interactionController.InteractionBlocked += ShowMessage;
        }

        private void Update()
        {
            if (_messageTimer <= 0f)
            {
                return;
            }

            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0f && _messageText != null)
            {
                _messageText.gameObject.SetActive(false);
            }
        }

        private void ShowByType(InteractionTypes type)
        {
            bool isActive = false;
            foreach (var ui in _interactionUIs)
            {
                if (ui.LoadType == type)
                {
                    isActive = true;
                }
                else
                {
                    isActive = false;
                }

                ui.SetVisible(isActive);
            }
        }

        // 사용처
        // 1. 상호작용을 한 후
        // 2. 상호작용 대상이 바뀐 후
        public void RefreshInteractionUI(bool isShow, InteractionTypes type = InteractionTypes.None)
        {
            ShowByType(isShow ? type : InteractionTypes.None);
        }

        private void ShowMessage(string message)
        {
            if (_messageText == null)
            {
                return;
            }

            _messageText.text = message;
            _messageText.gameObject.SetActive(true);
            _messageTimer = _messageDuration;
        }
    }
}
