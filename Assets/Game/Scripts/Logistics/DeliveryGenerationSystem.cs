using UnityEngine;

using Game.Systems;

namespace Game.Logistics
{
    /// <summary>하루 시작 시 정적 물류를 생성하고, 영업 중 동적 물류를 생성한다. 생성된 물류는 물류센터 상차 대기열에 들어간다.</summary>
    // GameTimeSystem.Start에서 게임 시각이 초기화된 뒤 하루 시작 생성을 수행하도록 실행 순서를 늦춘다
    [DefaultExecutionOrder(100)]
    public class DeliveryGenerationSystem : MonoBehaviour
    {
        public static DeliveryGenerationSystem Instance { get; private set; }

        // 동적 물류 생성 마감은 영업 종료 2시간 전(기획서 3.1, 4.2)
        private const int DynamicSpawnStopLeadSeconds = 2 * 3600;

        [SerializeField] private DeliveryGenerationConfig _config;
        [SerializeField] private DeliveryTypeConfig _typeConfig;
        [SerializeField] private LogisticsCenter _logisticsCenter;

        private int _remainingDynamicCount;
        private float _nextDynamicSpawnSeconds;
        private bool _isDynamicGenerationStopped;

        /// <summary>참조 검증을 마쳐 하루 시작을 받을 수 있는 상태인지.</summary>
        public bool IsReady { get; private set; }

        /// <summary>더 이상 물류가 생성되지 않는 상태(조기 종료 판정에 사용).</summary>
        public bool IsFinished => _isDynamicGenerationStopped || _remainingDynamicCount <= 0;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_config == null || _typeConfig == null || _logisticsCenter == null)
            {
                Debug.LogError($"DeliveryGenerationSystem references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            // 하루 시작은 DailyOperationSystem이 지시한다
            IsReady = true;
        }

        /// <summary>하루 시작 시퀀스 중 생성 단계(기획서 3.4의 4~6). 7단계 DailyOperationSystem이 이 함수를 호출한다.</summary>
        public void StartDay()
        {
            if (IsReady == false)
            {
                return;
            }

            _remainingDynamicCount = Random.Range(_config.DynamicMinCount, _config.DynamicMaxCount + 1);
            _nextDynamicSpawnSeconds = _config.FirstSpawnSeconds;
            _isDynamicGenerationStopped = false;

            Debug.Log($"[물류 생성] 하루 시작. 동적 생성 가능량 {_remainingDynamicCount}");
            GenerateStaticParcels();
        }

        /// <summary>하루 운영 종료 시 더 이상 물류를 만들지 않는다.</summary>
        public void StopDay()
        {
            if (_isDynamicGenerationStopped == false)
            {
                StopDynamicGeneration("하루 운영 종료");
            }
        }

        private void Update()
        {
            if (_isDynamicGenerationStopped || _remainingDynamicCount <= 0)
            {
                return;
            }

            GameTimeSystem gameTime = GameTimeSystem.Instance;
            if (gameTime == null || gameTime.IsBusinessEndReached)
            {
                return;
            }

            if (gameTime.CurrentSeconds >= GetDynamicSpawnStopSeconds())
            {
                StopDynamicGeneration("생성 마감 시각 도달");
                return;
            }

            if (gameTime.CurrentSeconds < _nextDynamicSpawnSeconds)
            {
                return;
            }

            // 할당 가능한 배송지가 없으면 재시도 없이 동적 생성을 종료한다(기획서 4.2)
            if (TryGenerateParcel() == false)
            {
                StopDynamicGeneration("할당 가능한 배송지 없음");
                return;
            }

            // 생성 성공 기준으로 차감하고, 다음 생성 시각은 실제 생성 시점 기준으로 잡는다(기획서 4.2)
            _remainingDynamicCount--;
            _nextDynamicSpawnSeconds = gameTime.CurrentSeconds
                + Random.Range(_config.DynamicIntervalMinSeconds, _config.DynamicIntervalMaxSeconds);
            Debug.Log($"[동적 물류] 남은 생성 가능량 {_remainingDynamicCount}, 다음 생성 {FormatTime(_nextDynamicSpawnSeconds)}");
        }

        private void GenerateStaticParcels()
        {
            int count = Random.Range(_config.StaticMinCount, _config.StaticMaxCount + 1);

            // 정적 생성량이 배송지 잔여 할당량을 넘지 않도록 제한한다(기획서 16.1-2)
            int remainingCapacity = CalculateRemainingCapacity();
            int generateCount = Mathf.Min(count, remainingCapacity);
            if (generateCount < count)
            {
                Debug.Log($"[물류 생성] 정적 생성량을 잔여 할당량으로 제한: {count} → {generateCount}");
            }

            for (int i = 0; i < generateCount; i++)
            {
                if (TryGenerateParcel() == false)
                {
                    Debug.LogWarning($"[물류 생성] 정적 물류 생성 중단: 할당 가능한 배송지 없음 ({i}/{generateCount} 생성)");
                    return;
                }
            }

            Debug.Log($"[물류 생성] 정적 물류 {generateCount}개 생성 완료");
        }

        /// <summary>배송지 전체의 남은 일일 할당 가능량 합계.</summary>
        private static int CalculateRemainingCapacity()
        {
            int capacity = 0;
            var targets = LogisticsCenter.DeliveryTargets;
            if (targets == null)
            {
                return 0;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                DeliveryTarget target = targets[i];
                if (target != null)
                {
                    capacity += target.MaxDailyAssignmentCount - target.DailyAssignedCount;
                }
            }

            return capacity;
        }

        /// <summary>물류 하나를 생성해 배송지에 할당하고 물류센터 대기열에 넣는다. 할당 가능한 배송지가 없으면 false.</summary>
        private bool TryGenerateParcel()
        {
            if (DeliveryAssignmentSystem.TryAssignRandom(out DeliveryTarget target) == false)
            {
                return false;
            }

            DeliveryType type = _typeConfig.RollType();
            DeliveryReward reward = RewardSystem.Instance.CreateReward(
                _logisticsCenter.transform.position,
                target.transform.position,
                type);

            Parcel parcel = DeliveryDeadlineSystem.Instance.CreateParcel(
                _logisticsCenter.transform.position,
                target,
                type,
                _typeConfig.GetDeadlineMultiplier(type),
                reward);

            _logisticsCenter.AddAvailableParcel(parcel);
            return true;
        }

        private float GetDynamicSpawnStopSeconds()
        {
            return GameTimeSystem.Instance.BusinessEndSeconds - DynamicSpawnStopLeadSeconds;
        }

        private void StopDynamicGeneration(string reason)
        {
            _isDynamicGenerationStopped = true;
            Debug.Log($"[동적 물류] 생성 종료: {reason}");
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            return $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}";
        }
    }
}
