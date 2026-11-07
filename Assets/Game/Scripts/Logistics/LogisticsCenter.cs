using System.Collections.Generic;

using UnityEngine;

using Game.Building;
using System.Reflection.Metadata.Ecma335;

namespace Game.Logistics
{
    /// <summary>택배를 상차하는 물류창고. 적재 한도까지 1개씩 상차한다.</summary>
    public class LogisticsCenter : BuildingBase
    {
        #region Singleton
        private static LogisticsCenter instance;
        public static LogisticsCenter Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError($"LogisticsCenter Singleton Instance is null");
                    return null;
                }

                return instance;
            }
        }
        #endregion

        // 배송지 리스트
        public List<DeliveryTarget> DeliveryTargets { get; } = new();

        [Header("Parcels Settings")]
        [Tooltip("창고가 보유하고 있는 택배 수")]
        [SerializeField, Min(0)] private int _parcels = 0;
        [Tooltip("창고가 보유할 수 있는 최대 택배 수")]
        [SerializeField, Min(0)] private int _maxParcels = 10;

        [Header("Interact Zone")]
        [SerializeField] private Collider _interactableZone;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError($"Multiple instances of singleton(LogisticsCenter) detected!");
                Destroy(gameObject);
            }
            else
            {
                instance = this;
            }

            _interactableZone = GetComponentInChildren<BoxCollider>();
            if (_interactableZone == null)
            {
                Debug.LogError($"InteractableZone is missing in {gameObject.name}");
                enabled = false;
            }

            LoadType = LoadTypes.Load;
        }

        public void Start()
        {
            _parcels = _maxParcels;
        }

        // private void Update()

        public override bool CanInteract()
        {
            return _parcels > 0;
        }

        public override void Interact()
        {
            _parcels--;
            Debug.Log($"택배 상차 완료");
        }
    }
}
