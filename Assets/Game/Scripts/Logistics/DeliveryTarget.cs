using UnityEngine;

using Game.Interaction;

namespace Game.Logistics
{
    /// <summary>지정된 배달지. 필요한 수량을 모두 받으면 더 이상 상호작용할 수 없다.</summary>
    public class DeliveryTarget : InteractableObject
    {
        public bool IsInitialized { get; private set; }

        [Header("Parcels Settings")]
        [Tooltip("배송지가 배송받은 택배 수")]
        [SerializeField, Min(0)] private int _deliveredParcels = 0;
        [Tooltip("배송지가 배송받아야 할 택배 수")]
        [SerializeField, Min(0)] private int _requiredParcels = 0;
        [Tooltip("배송지가 배송받아야 할 택배 수")]
        [SerializeField, Min(0)] private int _allocatableParcels = 5;


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
            return _requiredParcels < _allocatableParcels;
        }

        public void AllocateParcel(int amount = 1)
        {
            _requiredParcels += amount;
        }

        public override bool CanInteract()
        {
            return _deliveredParcels < _requiredParcels;
        }

        public override void Interact()
        {
            _deliveredParcels++;
            Debug.Log($"택배 하차 완료");
        }
    }
}
