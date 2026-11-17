using UnityEngine;

namespace Game.Player.Recovery
{
    /// <summary>차량 복구(리셋) 설정. 기획서 game-concept.md 10.6의 설정값 표와 같다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/VehicleRecoveryConfig", fileName = "VehicleRecoveryConfig")]
    public class VehicleRecoveryConfig : ScriptableObject
    {
        [Tooltip("복구가 실행되기까지 리셋 키를 누르고 있어야 하는 시간(초)")]
        [SerializeField, Min(0f)] private float _holdSeconds = 1f; // TODO(tuning)

        [Tooltip("안전 지점을 검사·기록하는 간격(초)")]
        [SerializeField, Min(0.01f)] private float _recordIntervalSeconds = 1f; // TODO(tuning)

        [Tooltip("보관하는 안전 지점 개수")]
        [SerializeField, Min(1)] private int _bufferCapacity = 5; // TODO(tuning)

        [Tooltip("복구 지점으로 쓰려면 기록된 뒤 지나야 하는 최소 시간(초). 방금 끼인 자리로 돌아가지 않기 위함")]
        [SerializeField, Min(0f)] private float _minAgeSeconds = 3f; // TODO(tuning)

        [Tooltip("최대 기울기. 차체 위쪽 벡터와 월드 위쪽의 내적 하한이다. 지면 판정 기준(VehicleConfig.GroundNormalMinDot)과 같게 둔다")]
        [SerializeField, Range(0f, 1f)] private float _minBodyUpDot = 0.5f; // TODO(tuning)

        [Tooltip("복구 후 조작할 수 없는 시간(초)")]
        [SerializeField, Min(0f)] private float _lockSeconds = 2f; // TODO(tuning)

        public float HoldSeconds => _holdSeconds;
        public float RecordIntervalSeconds => _recordIntervalSeconds;
        public int BufferCapacity => _bufferCapacity;
        public float MinAgeSeconds => _minAgeSeconds;
        public float MinBodyUpDot => _minBodyUpDot;
        public float LockSeconds => _lockSeconds;
    }
}
