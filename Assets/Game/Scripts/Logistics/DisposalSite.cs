using UnityEngine;

using Game.Interaction;

namespace Game.Logistics
{
    /// <summary>
    /// 폐기 물류의 목적지(기획서 11.6). 맵에 고정 배치하며 배송지 할당·일일 할당 한도의 대상이 아니다.
    /// 처분은 하차와 같은 방식(키 유지, 물류 1개당 설정 시간)이며 PlayerInteractionController가 수행한다.
    /// </summary>
    public class DisposalSite : InteractableObject
    {
        /// <summary>씬의 처분 장소(1곳 배치 가정). 폐기 전환 시 목적지로 쓴다.</summary>
        public static DisposalSite Instance { get; private set; }

        [SerializeField] private DisposalConfig _config;

        [Header("Interact Zone")]
        [SerializeField] private Collider _interactableZone;

        private void Awake()
        {
            Instance = this;

            // 프롬프트는 하차와 같은 UI를 쓰므로 타입을 고정해 씬 배선 실수를 막는다
            _interactionType = InteractionTypes.Unload;

            if (_config == null)
            {
                Debug.LogError($"DisposalConfig is not assigned in {gameObject.name}");
            }
            else
            {
                _interactionDuration = _config.SecondsPerParcel;
            }

            if (_interactableZone == null)
            {
                _interactableZone = GetComponentInChildren<BoxCollider>();
                if (_interactableZone == null)
                {
                    Debug.LogError($"InteractableZone is missing in {gameObject.name}");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>처분할 물류 유무는 적재 목록을 가진 PlayerInteractionController가 판정한다.</summary>
        public override bool CanInteract()
        {
            return true;
        }

        public override void Interact()
        {
        }
    }
}
