using System.Collections.Generic;

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
        private static readonly List<DisposalSite> Sites = new();

        /// <summary>활성화된 모든 처분 장소. 맵에 여러 곳을 둘 수 있다.</summary>
        public static IReadOnlyList<DisposalSite> All => Sites;

        [SerializeField] private DisposalConfig _config;

        [Header("Interact Zone")]
        [SerializeField] private Collider _interactableZone;

        private void Awake()
        {
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

        private void OnEnable()
        {
            Sites.Add(this);
        }

        private void OnDisable()
        {
            Sites.Remove(this);
        }

        /// <summary>수평(XZ) 거리로 가장 가까운 처분 장소. 하나도 없으면 null.</summary>
        public static DisposalSite FindNearest(Vector3 position)
        {
            DisposalSite nearest = null;
            float best = float.MaxValue;
            foreach (DisposalSite site in Sites)
            {
                Vector3 offset = site.transform.position - position;
                float sqr = offset.x * offset.x + offset.z * offset.z;
                if (sqr < best)
                {
                    best = sqr;
                    nearest = site;
                }
            }

            return nearest;
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
