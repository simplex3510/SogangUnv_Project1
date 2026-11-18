using System;

using UnityEngine;

namespace Game.Logistics
{
    /// <summary>배송 타입 하나의 확률·계수 묶음. 타입별 수치는 enum에 두지 않고 여기서 관리한다.</summary>
    [Serializable]
    public class DeliveryTypeEntry
    {
        [SerializeField] private DeliveryType _type;
        [Tooltip("생성 확률(0~1, 상대 가중치로 정규화해 사용)")]
        [SerializeField, Range(0f, 1f)] private float _spawnProbability = 0.5f; // TODO(tuning)
        [Tooltip("배송 제한 시간 적용 여부. 끄면 제한 시간·시간 추가 보상·시간 초과 손실이 없다(기획서 6.2)")]
        [SerializeField] private bool _hasDeadline = true;
        [Tooltip("배송 제한 시간 계수. Fresh는 신선도 계수 M_fresh로 쓰며 Express 계수보다 작게 둔다(제안: Express 계수 × 0.5)")]
        [SerializeField, Min(0f)] private float _deadlineMultiplier = 1f; // TODO(tuning)
        [Tooltip("거리 항 계수 M_dist. 제한 시간(신선도)의 거리 비례 부분에만 곱한다")]
        [SerializeField, Min(0f)] private float _distanceMultiplier = 1f; // TODO(tuning)
        [Tooltip("제한 시간이 줄기 시작하는 시점. Fresh는 LoadCompletion(상차 완료 시점)")]
        [SerializeField] private DeadlineStart _deadlineStart = DeadlineStart.Creation;
        [Tooltip("시간 초과 손실 계수")]
        [SerializeField, Min(0f)] private float _lateLossMultiplier = 1f; // TODO(tuning)
        [Tooltip("배송 실패 페널티 계수")]
        [SerializeField, Min(0f)] private float _failurePenaltyMultiplier = 1f; // TODO(tuning)

        public DeliveryType Type => _type;
        public float SpawnProbability => _spawnProbability;
        public bool HasDeadline => _hasDeadline;
        public float DeadlineMultiplier => _deadlineMultiplier;
        public float DistanceMultiplier => _distanceMultiplier;
        public DeadlineStart DeadlineStart => _deadlineStart;
        public float LateLossMultiplier => _lateLossMultiplier;
        public float FailurePenaltyMultiplier => _failurePenaltyMultiplier;
    }

    /// <summary>배송 타입별 설정값을 보관하는 ScriptableObject. 현재는 골격만 갖춘다.</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/DeliveryTypeConfig", fileName = "DeliveryTypeConfig")]
    public class DeliveryTypeConfig : ScriptableObject
    {
        [SerializeField] private DeliveryTypeEntry[] _entries = Array.Empty<DeliveryTypeEntry>();

        public DeliveryTypeEntry[] Entries => _entries;

        /// <summary>생성 확률(상대 가중치)로 배송 타입을 무작위 결정한다. 가중치 합이 0이면 Normal.</summary>
        public DeliveryType RollType()
        {
            float totalWeight = 0f;
            for (int i = 0; i < _entries.Length; i++)
            {
                totalWeight += Mathf.Max(_entries[i].SpawnProbability, 0f);
            }

            if (totalWeight <= 0f)
            {
                return DeliveryType.Normal;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            for (int i = 0; i < _entries.Length; i++)
            {
                float weight = Mathf.Max(_entries[i].SpawnProbability, 0f);
                if (roll < weight)
                {
                    return _entries[i].Type;
                }

                roll -= weight;
            }

            return _entries[_entries.Length - 1].Type;
        }

        /// <summary>배송 유형별 제한 시간 적용 여부. 설정에 없는 유형은 기존 동작대로 제한 시간을 둔다.</summary>
        public bool HasDeadline(DeliveryType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i].HasDeadline;
                }
            }

            return true;
        }

        /// <summary>배송 타입별 제한 시간 계수. 설정에 없는 타입은 1로 처리한다.</summary>
        public float GetDeadlineMultiplier(DeliveryType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i].DeadlineMultiplier;
                }
            }

            return 1f;
        }

        /// <summary>배송 타입별 제한 시간 거리 항 계수. 설정에 없는 타입은 1로 처리한다.</summary>
        public float GetDistanceMultiplier(DeliveryType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i].DistanceMultiplier;
                }
            }

            return 1f;
        }

        /// <summary>배송 유형의 제한 시간이 상차 완료 시점부터 줄어드는지. 설정에 없는 유형은 생성 시점부터 줄어든다.</summary>
        public bool StartsDeadlineOnLoad(DeliveryType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i].DeadlineStart == DeadlineStart.LoadCompletion;
                }
            }

            return false;
        }

        /// <summary>배송 타입별 시간 초과 손실 계수. 설정에 없는 타입은 1로 처리한다.</summary>
        public float GetLateLossMultiplier(DeliveryType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i].LateLossMultiplier;
                }
            }

            return 1f;
        }

        /// <summary>배송 타입별 실패 페널티 계수. 설정에 없는 타입은 1로 처리한다.</summary>
        public float GetFailurePenaltyMultiplier(DeliveryType type)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].Type == type)
                {
                    return _entries[i].FailurePenaltyMultiplier;
                }
            }

            return 1f;
        }
    }
}
