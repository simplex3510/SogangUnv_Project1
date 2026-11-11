using System.Collections.Generic;

using UnityEngine;

using Game.Systems;

namespace Game.Logistics
{
    /// <summary>물류의 배송 제한 시간을 생성 시점에 산정하고, 게임 시간에 따라 감소시키는 시스템.</summary>
    public class DeliveryDeadlineSystem : MonoBehaviour
    {
        public static DeliveryDeadlineSystem Instance { get; private set; }

        [SerializeField] private DeliveryDeadlineConfig _config;

        private readonly List<Parcel> _trackedParcels = new();
        private int _nextParcelId = 1;

        /// <summary>제한 시간이 최초로 초과된 시점에 1회 발생한다.</summary>
        public event System.Action<Parcel> ParcelOverdue;

        /// <summary>배송이 완료되어 추적이 끝난 시점에 발생한다.</summary>
        public event System.Action<Parcel> ParcelDelivered;

        /// <summary>생성됐지만 아직 배송되지 않은 물류. 영업 종료 시 미배송 판정에 사용한다.</summary>
        public IReadOnlyList<Parcel> TrackedParcels => _trackedParcels;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogError($"DeliveryDeadlineConfig is not assigned in {gameObject.name}");
                enabled = false;
            }
        }

        /// <summary>물류를 생성하고 제한 시간을 확정한다. T_limit = T_base(D) * M_type + R</summary>
        public Parcel CreateParcel(Vector3 origin, DeliveryTarget target, DeliveryType type, float deadlineMultiplier, DeliveryReward reward)
        {
            float distance = CalculateXZDistance(origin, target.transform.position);
            float baseDeadline = _config.BaseSeconds + _config.SecondsPerMeter * distance;
            float variation = Random.Range(_config.VariationMinSeconds, _config.VariationMaxSeconds);
            float deadline = Mathf.Max(baseDeadline * deadlineMultiplier + variation, 0f);

            Parcel parcel = new Parcel(
                _nextParcelId++,
                target,
                type,
                GameTimeSystem.Instance.CurrentSeconds,
                deadline,
                reward);

            _trackedParcels.Add(parcel);
            target.AddParcel(parcel);

            Debug.Log($"[Parcel #{parcel.Id}] 배송지 {target.name}, 타입 {type}, 거리 {distance:0.0}m, 제한 시간 {deadline:0}초, 보상 {reward.BaseFee + reward.DistanceFee:0}/{reward.TimeBonus:0}");

            return parcel;
        }

        /// <summary>추적을 중단한다(배송 완료·실패 등).</summary>
        public void Unregister(Parcel parcel)
        {
            _trackedParcels.Remove(parcel);
        }

        /// <summary>배송 완료를 알린다. 추적을 끝내고 정산 집계에 넘긴다.</summary>
        public void NotifyDelivered(Parcel parcel)
        {
            Unregister(parcel);
            ParcelDelivered?.Invoke(parcel);
        }

        private void Update()
        {
            float delta = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.GameDeltaSeconds : 0f;
            if (delta <= 0f)
            {
                return;
            }

            for (int i = 0; i < _trackedParcels.Count; i++)
            {
                Parcel parcel = _trackedParcels[i];
                if (parcel.IsOverdue)
                {
                    continue;
                }

                parcel.RemainingDeadline -= delta;
                if (parcel.RemainingDeadline > 0f)
                {
                    continue;
                }

                parcel.RemainingDeadline = 0f;
                parcel.IsOverdue = true;
                ParcelOverdue?.Invoke(parcel);
                Debug.Log($"[Parcel #{parcel.Id}] 배송 제한 시간 초과 (배송지 {parcel.Target.name})");
            }
        }

        /// <summary>XZ 평면 직선거리. 기획서 7.1의 거리 기준.</summary>
        private static float CalculateXZDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
