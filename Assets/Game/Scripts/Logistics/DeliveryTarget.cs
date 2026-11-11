using System;
using System.Collections.Generic;

using UnityEngine;

using Game.Interaction;

namespace Game.Logistics
{
    /// <summary>지정된 배달지. 필요한 수량을 모두 받으면 더 이상 상호작용할 수 없다.</summary>
    public class DeliveryTarget : InteractableObject
    {
        public bool IsInitialized { get; private set; }

        [Header("Parcels Settings")]
        [Tooltip("배송지가 하루 동안 할당받을 수 있는 최대 택배 수")]
        [SerializeField, Min(0)] private int _allocatableParcels = 5;

        /// <summary>당일 누적 할당 물류 수. 배송 완료 여부와 무관하게 하루 동안 감소하지 않는다.</summary>
        public int DailyAssignedCount { get; private set; }

        public int MaxDailyAssignmentCount => _allocatableParcels;

        /// <summary>이 배송지에 할당된 물류 목록(배송 완료·실패 포함).</summary>
        public IReadOnlyList<Parcel> AllocatedParcels => _allocatedParcels;

        /// <summary>할당 추가 또는 물류 상태 전이 시 발생한다. UI는 이 이벤트로 아이콘 구성을 갱신한다.</summary>
        public event Action<Parcel> ParcelsChanged;

        private readonly List<Parcel> _allocatedParcels = new();


        [Header("Interact Zone")]
        [SerializeField] private Collider _interactableZone;

        private void Awake()
        {
            if (_interactableZone == null)
            {
                _interactableZone = GetComponentInChildren<BoxCollider>();
                Debug.LogWarning($"InteractableZone is not assigned in {gameObject.name}");
                if (_interactableZone == null)
                {
                    Debug.LogError($"InteractableZone is missing in {gameObject.name}");
                }
            }
        }

        private void Start()
        {
            if (LogisticsCenter.DeliveryTargets == null)
            {
                Debug.LogError($"LogisticsCenter.deliveryTargets is null in {gameObject.name}");
                return;
            }

            LogisticsCenter.DeliveryTargets.Add(this);
        }

        public bool CanAllocate()
        {
            return DailyAssignedCount < MaxDailyAssignmentCount;
        }

        /// <summary>할당은 DeliveryAssignmentSystem만 수행한다.</summary>
        internal void AssignParcel()
        {
            DailyAssignedCount++;
        }

        internal void ResetDailyAssignment()
        {
            DailyAssignedCount = 0;
        }

        /// <summary>할당된 물류를 이 배송지의 추적 목록에 추가한다. 생성 시스템만 호출한다.</summary>
        internal void AddParcel(Parcel parcel)
        {
            _allocatedParcels.Add(parcel);
            ParcelsChanged?.Invoke(parcel);
        }

        /// <summary>물류 상태를 바꾼 쪽에서 호출한다. 상태 전이를 UI에 알린다.</summary>
        internal void NotifyParcelChanged(Parcel parcel)
        {
            ParcelsChanged?.Invoke(parcel);
        }

        public override bool CanInteract()
        {
            // 카운터가 아니라 실제 물류 상태로 판정한다. 배송이 끝난 물류만 있으면 상호작용할 수 없다.
            for (int i = 0; i < _allocatedParcels.Count; i++)
            {
                Parcel parcel = _allocatedParcels[i];
                if (parcel.Status != ParcelStatus.Delivered && parcel.Status != ParcelStatus.Failed)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Interact()
        {
            // 배송 처리는 적재 목록을 가진 PlayerInteractionController가 수행한다
        }
    }
}
