using UnityEngine;

using Game.UI;
using Game.Interaction;

namespace Game.Player.Interaction
{
    [RequireComponent(typeof(PlayerInteractionController))]
    /// <summary>상호작용 종류에 맞는 프롬프트만 켜고 나머지는 끈다.</summary>
    public class PlayerUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractionController _interactionController;

        [Header("UIs")]
        [SerializeField] private PlayerObjectUI[] _interactionUIs;

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
        }

        private void Start()
        {
            _interactionController.InteractionContextChanged += RefreshInteractionUI;
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
    }
}
