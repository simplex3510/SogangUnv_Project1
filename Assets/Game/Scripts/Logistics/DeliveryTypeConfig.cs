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
        [Tooltip("배송 제한 시간 계수")]
        [SerializeField, Min(0f)] private float _deadlineMultiplier = 1f; // TODO(tuning)
        [Tooltip("시간 초과 손실 계수")]
        [SerializeField, Min(0f)] private float _lateLossMultiplier = 1f; // TODO(tuning)
        [Tooltip("배송 실패 페널티 계수")]
        [SerializeField, Min(0f)] private float _failurePenaltyMultiplier = 1f; // TODO(tuning)

        public DeliveryType Type => _type;
        public float SpawnProbability => _spawnProbability;
        public float DeadlineMultiplier => _deadlineMultiplier;
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
