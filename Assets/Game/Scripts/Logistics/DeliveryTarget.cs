using UnityEngine;
using Game.Building;
using UnityEditor.EditorTools;

namespace Game.Logistics
{
    /// <summary>지정된 배달지. 필요한 수량을 모두 받으면 더 이상 상호작용할 수 없다.</summary>
    public class DeliveryTarget : BuildingBase
    {
        public bool IsInitialized { get; private set; }

        [Header("Parcels Settings")]
        [Tooltip("배송지가 배송받은 택배 수")]
        [SerializeField, Min(0)] private int _deliveredParcels;
        [Tooltip("배송지가 배송받아야 할 택배 수")]
        [SerializeField, Min(0)] private int _requiredParcels;

        [Header("Interact Zone")]
        [SerializeField] private Collider _interactableZone;

        private void Awake()
        {
            _interactableZone = GetComponentInChildren<BoxCollider>();
            if (_interactableZone == null)
            {
                Debug.LogError($"InteractableZone is missing in {gameObject.name}");
                enabled = false;
            }

            LoadType = LoadTypes.Load;
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Start()
        {
            if (LogisticsCenter.Instance.DeliveryTargets == null)
            {
                Debug.LogError($"LogisticsCenter.deliveryTargets is null in {gameObject.name}");
                return;
            }

            LogisticsCenter.Instance.DeliveryTargets.Add(this);
        }

        private void Initialize()
        {
            IsInitialized = true;
            _requiredParcels = 0;
            _deliveredParcels = 0;
        }

        public void RequireParcels(int count)
        {
            _requiredParcels = count;
        }

        public override bool CanInteract()
        {
            return _deliveredParcels < _requiredParcels;
        }

        public override void Interact()
        {
            _deliveredParcels++;

            if (_requiredParcels <= 0)
            {
                Initialize();
            }

            Debug.Log($"택배 하차 완료");
        }
    }
}
