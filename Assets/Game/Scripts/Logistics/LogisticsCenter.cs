using System.Collections.Generic;

using UnityEngine;

using Game.Interaction;

namespace Game.Logistics
{
    /// <summary>택배를 상차하는 물류창고. 적재 한도까지 1개씩 상차한다.</summary>
    public class LogisticsCenter : InteractableObject
    {
        // 배송지 리스트
        private static readonly List<DeliveryTarget> _deliveryTargets = new();
        public static List<DeliveryTarget> DeliveryTargets { get {return _deliveryTargets;} }

        [Header("Parcels Settings")]
        [Tooltip("창고가 보유하고 있는 택배 수")]
        [SerializeField, Min(0)] private int _currentParcelCount = 0;
        [Tooltip("창고가 보유할 수 있는 최대 택배 수")]
        [SerializeField, Min(0)] private int _maxParcelCount = 5;

        [Header("Production Settings")]
        [Tooltip("물류 하나를 생산하는데 걸리는 시간")]
        [SerializeField, Min(0)] private float _productionDuration = 1f;
        [Tooltip("생산한 물류 수")]
        [SerializeField, Min(0)] private int _currentProductionCount = 0;
        [Tooltip("최대 생산 물류 수")]
        [SerializeField, Min(0)] private int _maxProductionCount = 10;

        private float _productionTime = 0f;

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

        public void Start()
        {
            
        }

        private void Update()
        {
            _productionTime += Time.deltaTime;
            if (_productionTime >= _productionDuration)
            {
                if (_currentParcelCount >= _maxParcelCount)
                {
                    return;
                }

                if (_currentProductionCount >= _maxProductionCount)
                {
                    return;
                }
                
                ProductParcel();
                _productionTime = 0f;  
            }
        }

        public override bool CanInteract()
        {
            return _currentParcelCount > 0;
        }

        public override void Interact()
        {
            _currentParcelCount--;
            Debug.Log($"택배 상차 완료");
        }

        private void ProductParcel()
        {
            while (true)
            {
                int randomIndex = Random.Range(0, _deliveryTargets.Count);
                DeliveryTarget target = _deliveryTargets[randomIndex];

                if (target.CanAllocate() == true)
                {
                    target.AllocateParcel();
                    break;
                }
            }

            _currentParcelCount++;
            _currentProductionCount++;
        }
    }
}
