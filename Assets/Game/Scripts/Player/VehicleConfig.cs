using System;

using UnityEngine;

namespace Game.Player.Movement
{
    /// <summary>차량 주행 성능과 조작감 설정. 모드별 값과 모든 모드가 공유하는 값을 한 곳에 둔다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/VehicleConfig", fileName = "VehicleConfig")]
    public class VehicleConfig : ScriptableObject
    {
        [Serializable]
        public class DrivingModeSettings
        {
            [Tooltip("전진 최고 속도(m/s)")]
            [Min(0f)] public float MaxForwardSpeed = 3f;

            [Tooltip("가속도(m/s²)")]
            [Min(0f)] public float Acceleration = 5f;

            [Tooltip("최대 회전 속도(도/초)")]
            [Min(0f)] public float MaxTurnRate = 100f;

            [Tooltip("옆방향 미끄러짐이 줄어드는 비율(/초). 클수록 접지력이 높다")]
            [Min(0f)] public float LateralGrip = 10f;

            [Tooltip("조향 입력이 목표값에 도달하는 속도. 낮을수록 반응이 둔하다")]
            [Min(0f)] public float SteerSmoothing = 5f;
        }

        [Header("주행 모드")]
        [SerializeField] private DrivingModeSettings _normalMode = new DrivingModeSettings();

        // TODO(tuning): 회전 반경이 일반 모드의 2배 이상이 되도록 잡은 시작값. Play 테스트로 확정한다
        [SerializeField] private DrivingModeSettings _fastMode = new DrivingModeSettings
        {
            MaxForwardSpeed = 5f,
            Acceleration = 5.6f,
            MaxTurnRate = 62f,
            LateralGrip = 7.5f,
            SteerSmoothing = 3.5f,
        };

        [Tooltip("모드 전환 시 성능 값이 새 모드 값까지 이동하는 시간(초)")]
        [SerializeField, Min(0f)] private float _modeTransitionSeconds = 0.4f; // TODO(tuning)

        [Header("공통 주행")]
        [SerializeField, Min(0f)] private float _maxReverseSpeed = 2.5f;
        [SerializeField, Min(0f)] private float _brakeDeceleration = 20f;
        [SerializeField, Min(0f)] private float _coastDeceleration = 4f;

        [Tooltip("이보다 느리면 입력 반대 방향이어도 제동이 아니라 가속으로 취급한다(m/s)")]
        [SerializeField, Min(0f)] private float _brakeSpeedThreshold = 0.1f; // TODO(tuning)

        [Tooltip("속도 대비 조향 비율 곡선. 가로축은 현재 모드 최고 속도에 대한 비율")]
        [SerializeField] private AnimationCurve _turnBySpeed = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.6f));

        [Header("드리프트")]
        [Tooltip("드리프트가 발동하는 최소 전진 속도(m/s)")]
        [SerializeField, Min(0f)] private float _driftMinForwardSpeed = 1.25f; // TODO(tuning)

        [Tooltip("드리프트 중 접지력 배율")]
        [SerializeField, Min(0f)] private float _driftGripMultiplier = 0.15f; // TODO(tuning)

        [Tooltip("드리프트 중 최대 회전 속도 배율")]
        [SerializeField, Min(0f)] private float _driftTurnMultiplier = 1.3f; // TODO(tuning)

        [Header("서스펜션")]
        [Tooltip("바퀴 중심에서 쏘는 스피어캐스트 반지름(m). 바퀴 반지름보다 약간 작게 둔다")]
        [SerializeField, Min(0f)] private float _suspensionRadius = 0.07f;

        [Tooltip("서스펜션이 늘어날 수 있는 최대 거리(m). 캐스트 길이 = 반지름 + 이 값")]
        [SerializeField, Min(0f)] private float _suspensionTravel = 0.15f;

        [Tooltip("목표 휠 중심 높이(m). 차체가 지면에서 뜨는 정도를 결정한다")]
        [SerializeField] private float _rideHeight = 0.078f; // TODO(tuning): 정지 시 바퀴가 노면에 닿는 높이

        [Tooltip("스프링 강성(m/s² per m). 클수록 단단하다")]
        [SerializeField, Min(0f)] private float _springStrength = 150f; // TODO(tuning)

        [Tooltip("스프링 감쇠(m/s² per m/s). 클수록 진동이 빨리 잦아든다")]
        [SerializeField, Min(0f)] private float _springDamper = 15f; // TODO(tuning)

        [Tooltip("바퀴 중심의 루트 기준 오프셋(m). 앞-좌, 앞-우, 뒤-좌, 뒤-우 순서")]
        [SerializeField] private Vector3[] _wheelOffsets =
        {
            new Vector3(-0.10f, 0.075f, 0.305f),
            new Vector3(0.10f, 0.075f, 0.305f),
            new Vector3(-0.10f, 0.075f, -0.105f),
            new Vector3(0.10f, 0.075f, -0.105f),
        };

        [Header("지면 판정")]
        [Tooltip("스피어캐스트가 지면으로 인정할 레이어")]
        [SerializeField] private LayerMask _groundMask = 1 << 7;

        [Tooltip("캐스트 법선의 위쪽 성분이 이 값 이상이어야 지면으로 인정한다 (0.5 ≈ 60도)")]
        [SerializeField, Range(0f, 1f)] private float _groundNormalMinDot = 0.5f;

        [Tooltip("접지가 끊긴 뒤에도 접지로 취급하는 유예 시간(초)")]
        [SerializeField, Min(0f)] private float _groundContactGraceSeconds = 0.1f;

        [Tooltip("캐스트 법선이 흔들릴 때 따라가는 속도(/초)")]
        [SerializeField, Min(0f)] private float _groundNormalSmoothing = 10f;

        [Header("자세 복원")]
        [Tooltip("차체를 지면(공중이면 수직)에 맞추는 힘")]
        [SerializeField, Min(0f)] private float _uprightStrength = 12f;

        [Tooltip("기울기 흔들림을 잡는 감쇠")]
        [SerializeField, Min(0f)] private float _uprightDamping = 4f;

        [Tooltip("공중에서의 복원력 비율")]
        [SerializeField, Range(0f, 1f)] private float _airUprightFactor = 0.3f;

        // 에셋에서 배열을 비워 둔 경우 사용할 기본값 (모델의 바퀴 배치 기준)
        private static readonly Vector3[] DefaultWheelOffsets =
        {
            new Vector3(-0.10f, 0.075f, 0.305f),
            new Vector3(0.10f, 0.075f, 0.305f),
            new Vector3(-0.10f, 0.075f, -0.105f),
            new Vector3(0.10f, 0.075f, -0.105f),
        };

        public DrivingModeSettings GetModeSettings(VehicleDrivingMode mode)
            => mode == VehicleDrivingMode.Fast ? _fastMode : _normalMode;

        public float ModeTransitionSeconds => _modeTransitionSeconds;

        public float MaxReverseSpeed => _maxReverseSpeed;
        public float BrakeDeceleration => _brakeDeceleration;
        public float CoastDeceleration => _coastDeceleration;
        public AnimationCurve TurnBySpeed => _turnBySpeed;

        public float DriftMinForwardSpeed => _driftMinForwardSpeed;
        public float DriftGripMultiplier => _driftGripMultiplier;
        public float DriftTurnMultiplier => _driftTurnMultiplier;

        public float BrakeSpeedThreshold => _brakeSpeedThreshold;

        public float SuspensionRadius => _suspensionRadius;
        public float SuspensionTravel => _suspensionTravel;
        public float RideHeight => _rideHeight;
        public float SpringStrength => _springStrength;
        public float SpringDamper => _springDamper;

        public Vector3[] WheelOffsets
            => _wheelOffsets != null && _wheelOffsets.Length > 0 ? _wheelOffsets : DefaultWheelOffsets;

        public LayerMask GroundMask => _groundMask;
        public float GroundNormalMinDot => _groundNormalMinDot;
        public float GroundContactGraceSeconds => _groundContactGraceSeconds;
        public float GroundNormalSmoothing => _groundNormalSmoothing;

        public float UprightStrength => _uprightStrength;
        public float UprightDamping => _uprightDamping;
        public float AirUprightFactor => _airUprightFactor;
    }
}
