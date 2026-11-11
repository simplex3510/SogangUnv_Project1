using UnityEngine;

namespace Game.Logistics
{
    /// <summary>정적·동적 물류의 생성량과 동적 생성 시각 설정. 기획서 14.2의 DeliveryGenerationConfig.</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/DeliveryGenerationConfig", fileName = "DeliveryGenerationConfig")]
    public class DeliveryGenerationConfig : ScriptableObject
    {
        [Header("정적 물류")]
        [Tooltip("하루 시작 시 생성할 정적 물류 수의 최소값")]
        [SerializeField, Min(0)] private int _staticMinCount = 6; // TODO(tuning)
        [Tooltip("하루 시작 시 생성할 정적 물류 수의 최대값")]
        [SerializeField, Min(0)] private int _staticMaxCount = 10; // TODO(tuning)

        [Header("동적 물류")]
        [Tooltip("하루 동안 생성할 수 있는 동적 물류 수의 최소값")]
        [SerializeField, Min(0)] private int _dynamicMinCount = 3; // TODO(tuning)
        [Tooltip("하루 동안 생성할 수 있는 동적 물류 수의 최대값")]
        [SerializeField, Min(0)] private int _dynamicMaxCount = 6; // TODO(tuning)
        [Tooltip("첫 동적 물류가 생성될 수 있는 시각(시)")]
        [SerializeField, Range(0, 23)] private int _firstSpawnHour = 9;
        [Tooltip("첫 동적 물류가 생성될 수 있는 시각(분)")]
        [SerializeField, Range(0, 59)] private int _firstSpawnMinute = 0;
        [Tooltip("동적 물류 생성 간격의 최소값(게임 시간 초)")]
        [SerializeField, Min(0f)] private float _dynamicIntervalMinSeconds = 600f; // TODO(tuning)
        [Tooltip("동적 물류 생성 간격의 최대값(게임 시간 초)")]
        [SerializeField, Min(0f)] private float _dynamicIntervalMaxSeconds = 1500f; // TODO(tuning)

        // 최소·최대가 뒤집혀 저장되어도 난수 범위가 깨지지 않도록 정렬해 노출한다
        public int StaticMinCount => Mathf.Min(_staticMinCount, _staticMaxCount);
        public int StaticMaxCount => Mathf.Max(_staticMinCount, _staticMaxCount);
        public int DynamicMinCount => Mathf.Min(_dynamicMinCount, _dynamicMaxCount);
        public int DynamicMaxCount => Mathf.Max(_dynamicMinCount, _dynamicMaxCount);

        public int FirstSpawnSeconds => _firstSpawnHour * 3600 + _firstSpawnMinute * 60;

        public float DynamicIntervalMinSeconds => Mathf.Min(_dynamicIntervalMinSeconds, _dynamicIntervalMaxSeconds);
        public float DynamicIntervalMaxSeconds => Mathf.Max(_dynamicIntervalMinSeconds, _dynamicIntervalMaxSeconds);
    }
}
