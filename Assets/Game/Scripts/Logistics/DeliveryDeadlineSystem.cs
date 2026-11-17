using System.Collections.Generic;

using UnityEngine;

using Game.Systems;

namespace Game.Logistics
{
    /// <summary>물류의 배송 제한 시간을 생성 시점에 산정하고, 게임 시간에 따라 감소시키는 시스템.</summary>
    // 물류 생성 뒤, 상·하차 완료 판정 전에 제한 시간을 감소·판정한다(기획서 3.7)
    [DefaultExecutionOrder(150)]
    public class DeliveryDeadlineSystem : MonoBehaviour
    {
        public static DeliveryDeadlineSystem Instance { get; private set; }

        [SerializeField] private DeliveryDeadlineConfig _config;

        private readonly List<Parcel> _trackedParcels = new();

        // 폐기 전환이 추적 목록에서 물류를 빼므로, 순회가 끝난 뒤 알리려고 모아 둔다
        private readonly List<Parcel> _depletedParcels = new();
        private int _nextParcelId = 1;

        /// <summary>제한 시간이 최초로 초과된 시점에 1회 발생한다.</summary>
        public event System.Action<Parcel> ParcelOverdue;

        /// <summary>배송이 완료되어 추적이 끝난 시점에 발생한다.</summary>
        public event System.Action<Parcel> ParcelDelivered;

        /// <summary>물류가 폐기로 전환되어 추적이 끝난 시점에 발생한다. 정산이 실패로 집계한다.</summary>
        public event System.Action<Parcel> ParcelSpoiled;

        /// <summary>적재한 Fresh의 신선도가 0이 된 시점에 발생한다. 차량 쪽이 폐기 전환(Story 005 공통 경로)을 수행한다.</summary>
        public event System.Action<Parcel> FreshnessDepleted;

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

        /// <summary>물류를 생성하고 제한 시간을 확정한다. T_limit = T_base(D) * M_type + R (제한 시간이 있는 유형만)</summary>
        public Parcel CreateParcel(
            Vector3 origin,
            DeliveryTarget target,
            DeliveryType type,
            DeliveryGenerationType generationType,
            CargoSpec cargo,
            bool hasDeadline,
            bool deadlineStartsOnLoad,
            float deadlineMultiplier,
            DeliveryReward reward)
        {
            float distance = CalculateXZDistance(origin, target.transform.position);
            float deadline = 0f;
            if (hasDeadline)
            {
                float baseDeadline = _config.BaseSeconds + _config.SecondsPerMeter * distance;
                float variation = Random.Range(_config.VariationMinSeconds, _config.VariationMaxSeconds);
                deadline = Mathf.Max(baseDeadline * deadlineMultiplier + variation, 0f);
            }

            Parcel parcel = new Parcel(
                _nextParcelId++,
                target,
                type,
                generationType,
                cargo,
                GameTimeSystem.Instance.CurrentSeconds,
                hasDeadline,
                deadlineStartsOnLoad,
                deadline,
                reward);

            // 제한 시간이 없어도 영업 종료 시 미배송 판정을 위해 추적한다
            _trackedParcels.Add(parcel);
            target.AddParcel(parcel);

            string deadlineText = hasDeadline ? (type == DeliveryType.Fresh ? $"신선도 {deadline:0}초(상차 후 감소)" : $"{deadline:0}초") : "없음";
            Debug.Log($"[Parcel #{parcel.Id}] 배송지 {target.name}, 타입 {type}, 생성 {generationType}, 특징 {cargo.Traits}, 내구도 {cargo.Durability}, 용량 {cargo.CapacityUsage}, 무게 {cargo.WeightUsage}, 거리 {distance:0.0}m, 제한 시간 {deadlineText}, 보상 {reward.BaseFee + reward.DistanceFee:0}/{reward.TimeBonus:0}/특징 {reward.TraitBonus:0}/신선 {reward.FreshBonusMax:0}");

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

        /// <summary>폐기 전환을 알린다. 제한 시간 추적을 끝내(시간 초과 손실 없음) 정산 집계에 넘긴다.</summary>
        public void NotifySpoiled(Parcel parcel)
        {
            Unregister(parcel);
            ParcelSpoiled?.Invoke(parcel);
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
                if (parcel.HasDeadline == false || parcel.IsOverdue)
                {
                    continue;
                }

                // 상차 완료 시점부터 줄어드는 유형은 물류센터 대기 중에는 줄지 않는다
                if (parcel.DeadlineStartsOnLoad && parcel.Status == ParcelStatus.Pending)
                {
                    continue;
                }

                // 상차 완료 시점부터 줄어드는 유형은 저장한 상차 시각으로 남은 값을 다시 계산한다
                parcel.RemainingDeadline = parcel.DeadlineStartsOnLoad
                    ? parcel.Deadline - (GameTimeSystem.Instance.CurrentSeconds - parcel.LoadCompletionTime)
                    : parcel.RemainingDeadline - delta;
                if (parcel.RemainingDeadline > 0f)
                {
                    continue;
                }

                parcel.RemainingDeadline = 0f;

                // Fresh는 시간 초과가 아니라 폐기 전환이다. 시간 초과 손실은 매기지 않는다
                if (parcel.IsFresh)
                {
                    _depletedParcels.Add(parcel);
                    continue;
                }

                parcel.IsOverdue = true;
                ParcelOverdue?.Invoke(parcel);
                Debug.Log($"[Parcel #{parcel.Id}] 배송 제한 시간 초과 (배송지 {parcel.Target.name})");
            }

            for (int i = 0; i < _depletedParcels.Count; i++)
            {
                Debug.Log($"[Parcel #{_depletedParcels[i].Id}] 신선도 소진 (배송지 {_depletedParcels[i].Target.name})");
                FreshnessDepleted?.Invoke(_depletedParcels[i]);
            }

            _depletedParcels.Clear();
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
