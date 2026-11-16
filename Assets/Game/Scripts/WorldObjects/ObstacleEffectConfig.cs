using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>정적 방해물의 효과 파라미터와 잔류 시간(방해물 프레임워크 R3). 오브젝트별 수치는 Story 011에서 정한다.</summary>
    [CreateAssetMenu(menuName = "Game/WorldObjects/ObstacleEffectConfig", fileName = "ObstacleEffectConfig")]
    public class ObstacleEffectConfig : ScriptableObject
    {
        [Header("Speed Multiplier Effect")]
        [Tooltip("최고 속도 배율. 1 미만이면 감속. 0이면 조향 계산이 깨지므로 최소값을 둔다")]
        [SerializeField, Range(0.01f, 3f)] private float _speedMultiplier = 0.5f; // TODO(tuning)

        [Tooltip("최고 속도 배율 지속 시간(초)")]
        [SerializeField, Min(0f)] private float _speedDurationSeconds = 3f; // TODO(tuning)

        [Header("Heading Rotation Effect")]
        [Tooltip("진행 방향 회전 각도(도)")]
        [SerializeField, Min(0f)] private float _headingYawDegrees = 45f; // TODO(tuning)

        [Tooltip("true면 좌우 방향을 무작위로 정한다")]
        [SerializeField] private bool _headingRandomSign = true; // TODO(tuning)

        [Header("Lifetime")]
        [Tooltip("실행 후 잔류하다 제거되기까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _lingerSeconds = 2f; // TODO(tuning)

        public float SpeedMultiplier => _speedMultiplier;
        public float SpeedDurationSeconds => _speedDurationSeconds;
        public float HeadingYawDegrees => _headingYawDegrees;
        public bool HeadingRandomSign => _headingRandomSign;
        public float LingerSeconds => _lingerSeconds;
    }
}
