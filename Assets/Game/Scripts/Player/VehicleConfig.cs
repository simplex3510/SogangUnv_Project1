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
    }
}
