using System.Collections.Generic;

using UnityEngine;

using Game.Interaction;
using Game.UI;

namespace Game.Logistics
{
    /// <summary>택배를 상차하는 물류창고. 생성 시스템이 넣은 물류를 생성 순서대로 보관하고, 상호작용 시 상차 UI를 연다.</summary>
    public class LogisticsCenter : InteractableObject
    {
        // 배송지 리스트
        private static readonly List<DeliveryTarget> _deliveryTargets = new();
        public static List<DeliveryTarget> DeliveryTargets { get {return _deliveryTargets;} }

        // 배송지에 할당되어 상차를 기다리는 물류. 상차 순서는 생성 순서를 따른다.
        private readonly List<Parcel> _availableParcels = new();

        [Header("Interact Zone")]
        [SerializeField] private Collider _interactableZone;

        [Header("UI")]
        [SerializeField] private LogisticsCenterUI _ui;
        [Tooltip("상호작용 키를 유지해 상차 UI가 열리기까지 걸리는 시간(초)")]
        [SerializeField, Min(0f)] private float _uiOpenDuration = 1f; // TODO(tuning)

        public IReadOnlyList<Parcel> AvailableParcels => _availableParcels;

        public bool IsUIOpen => _ui != null && _ui.IsOpen;

        public float UIOpenDuration => _uiOpenDuration;

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

        /// <summary>생성 시스템이 물류를 상차 대기열에 넣는다.</summary>
        internal void AddAvailableParcel(Parcel parcel)
        {
            _availableParcels.Add(parcel);
        }

        public bool ContainsParcel(Parcel parcel)
        {
            return _availableParcels.Contains(parcel);
        }

        /// <summary>대기열 선두 물류. 없으면 null.</summary>
        public Parcel PeekNextParcel()
        {
            return _availableParcels.Count > 0 ? _availableParcels[0] : null;
        }

        /// <summary>대기열에서 특정 물류를 꺼낸다. 상차 완료 시점에 호출된다.</summary>
        public bool TryTakeParcel(Parcel parcel)
        {
            return _availableParcels.Remove(parcel);
        }

        public void OpenUI()
        {
            if (_ui != null)
            {
                _ui.Open();
            }
        }

        public void CloseUI()
        {
            if (_ui != null)
            {
                _ui.Close();
            }
        }

        public override bool CanInteract()
        {
            return _availableParcels.Count > 0;
        }

        public override void Interact()
        {
            // 상차 처리는 적재 목록을 가진 PlayerInteractionController가 수행한다
            OpenUI();
        }
    }
}
