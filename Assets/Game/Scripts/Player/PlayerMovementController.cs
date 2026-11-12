using System;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Player.Interaction;

namespace Game.Player.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    // 파일명과 클래스명이 다르면 Unity가 스크립트를 바인딩하지 못해 컴포넌트가 로드되지 않는다
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private InputActionReference _playerMoveInput;
        [SerializeField] private InputActionReference _modeChangeInput;
        [SerializeField] private InputActionReference _driftInput;

        [Header("Config")]
        [SerializeField] private VehicleConfig _vehicleConfig;

        [Header("Suspension")]
        [Tooltip("바퀴 중심에서 쏘는 스피어캐스트 반지름(m). 바퀴 반지름보다 약간 작게 둔다")]
        [SerializeField] private float _suspensionRadius = 0.07f;
        [Tooltip("서스펜션이 늘어날 수 있는 최대 거리(m). 캐스트 길이 = 반지름 + 이 값")]
        [SerializeField] private float _suspensionTravel = 0.15f;
        [Tooltip("목표 휠 중심 높이(m). 차체가 지면에서 뜨는 정도를 결정한다")]
        [SerializeField] private float _rideHeight = 0.078f; // TODO(tuning): 정지 시 바퀴가 노면에 닿는 높이
        [Tooltip("스프링 강성(m/s² per m). 클수록 단단하다")]
        [SerializeField] private float _springStrength = 150f; // TODO(tuning)
        [Tooltip("스프링 감쇠(m/s² per m/s). 클수록 진동이 빨리 잦아든다")]
        [SerializeField] private float _springDamper = 15f; // TODO(tuning)
        [Tooltip("바퀴 중심의 루트 기준 오프셋(m). 앞-좌, 앞-우, 뒤-좌, 뒤-우 순서")]
        [SerializeField] private Vector3[] _wheelOffsets =
        {
            new Vector3(-0.10f, 0.075f, 0.305f),
            new Vector3(0.10f, 0.075f, 0.305f),
            new Vector3(-0.10f, 0.075f, -0.105f),
            new Vector3(0.10f, 0.075f, -0.105f),
        };
        [Tooltip("스피어캐스트가 지면으로 인정할 레이어")]
        [SerializeField] private LayerMask _groundMask = 1 << 7;
        [Tooltip("캐스트 법선의 위쪽 성분이 이 값 이상이어야 지면으로 인정한다 (0.5 ≈ 60도)")]
        [SerializeField] private float _groundNormalMinDot = 0.5f;
        [Tooltip("접지가 끊긴 뒤에도 접지로 취급하는 유예 시간(초)")]
        [SerializeField] private float _groundContactGraceSeconds = 0.1f;
        [Tooltip("캐스트 법선이 흔들릴 때 따라가는 속도(/초)")]
        [SerializeField] private float _groundNormalSmoothing = 10f;

        [Header("Self Righting")]
        [Tooltip("차체를 지면(공중이면 수직)에 맞추는 힘")]
        [SerializeField] private float _uprightStrength = 12f;
        [Tooltip("기울기 흔들림을 잡는 감쇠")]
        [SerializeField] private float _uprightDamping = 4f;
        [Tooltip("공중에서의 복원력 비율")]
        [SerializeField, Range(0f, 1f)] private float _airUprightFactor = 0.3f;

        // 이보다 느리면 입력 반대 방향이어도 제동이 아니라 가속으로 취급한다
        private const float BrakeSpeedThreshold = 0.1f;

        // 프리팹에 오프셋이 직렬화되지 않은 경우 사용할 기본값 (모델의 바퀴 배치 기준)
        private static readonly Vector3[] DefaultWheelOffsets =
        {
            new Vector3(-0.10f, 0.075f, 0.305f),
            new Vector3(0.10f, 0.075f, 0.305f),
            new Vector3(-0.10f, 0.075f, -0.105f),
            new Vector3(0.10f, 0.075f, -0.105f),
        };

        // 아래 값들은 VehicleConfig의 모드 값에 블렌딩을 적용한 현재 프레임의 실효값이다
        private float _maxForwardSpeed;
        private float _acceleration;
        private float _maxTurnRate;
        private float _lateralGrip;
        private float _steerSmoothing;

        private VehicleDrivingMode _drivingMode = VehicleDrivingMode.Normal;
        private float _modeBlend;
        private bool _isDriftHeld;
        private bool _isDrifting;

        private Vector2 _input;
        private float _steer;
        private float _forwardSpeed;
        private bool _isGrounded;
        private bool _isControlEnabled = true;
        private Vector3 _groundNormal = Vector3.up;
        private float _lastGroundedTime = float.NegativeInfinity;

        private PlayerInteractionController _interactionController;

        public event Action<VehicleDrivingMode> DrivingModeChanged;

        public VehicleDrivingMode DrivingMode => _drivingMode;

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (_vehicleConfig == null)
            {
                Debug.LogError($"VehicleConfig is not assigned in {gameObject.name}");
                enabled = false;
                return;
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInChildren<Rigidbody>();
                Debug.LogWarning($"Rigidbody is not assigned in {gameObject.name}");
            }

            if (_rigidbody == null)
            {
                Debug.LogError($"Rigidbody is missing in {gameObject.name}");
                enabled = false;
                return;
            }

            // 구동을 힘으로 인가하므로 감쇠가 남아 있으면 가속·조향 체감이 설정값과 어긋난다
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;

            if (_wheelOffsets == null || _wheelOffsets.Length == 0)
            {
                _wheelOffsets = DefaultWheelOffsets;
            }

            DisableGroundCollision();

            _interactionController = GetComponent<PlayerInteractionController>();

            UpdateModeParameters();
        }

        private void OnEnable()
        {
            _playerMoveInput.action.Enable();
            _modeChangeInput.action.Enable();
            _driftInput.action.Enable();
        }

        private void OnDisable()
        {
            _playerMoveInput.action.Disable();
            _modeChangeInput.action.Disable();
            _driftInput.action.Disable();
        }

        /// <summary>하루 종료 시 주행 입력을 끊고 차량을 즉시 세운다(기획서 3.5).</summary>
        public void SetControlEnabled(bool isEnabled)
        {
            _isControlEnabled = isEnabled;
            if (isEnabled)
            {
                return;
            }

            _input = Vector2.zero;
            _isDriftHeld = false;

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            if (_isControlEnabled == false)
            {
                return;
            }

            _input = _playerMoveInput.action.ReadValue<Vector2>();
            _isDriftHeld = _driftInput.action.IsPressed();

            if (_modeChangeInput.action.WasPressedThisFrame())
            {
                TryToggleDrivingMode();
            }
        }

        private void FixedUpdate()
        {
            UpdateModeParameters();
            ApplySuspension();
            UpdateDriftState();

            if (_isGrounded)
            {
                ApplyDrive();
                ApplySteering();
                ApplyLateralGrip();
            }

            KeepUpright();
        }

        /// <summary>지면은 레이캐스트로만 판정한다. 콜라이더 접촉은 타일 경계마다 수직 충격을 만든다.</summary>
        private void DisableGroundCollision()
        {
            int drivableLayer = LayerMask.NameToLayer("Drivable");
            if (drivableLayer >= 0)
            {
                Physics.IgnoreLayerCollision(gameObject.layer, drivableLayer, true);
            }
        }

        private void TryToggleDrivingMode()
        {
            // 공중과 상호작용 중에는 전환 요청을 받지 않는다
            if (_isGrounded == false)
            {
                return;
            }

            if (_interactionController != null && _interactionController.IsInteracting)
            {
                return;
            }

            _drivingMode = _drivingMode == VehicleDrivingMode.Normal
                ? VehicleDrivingMode.Fast
                : VehicleDrivingMode.Normal;

            DrivingModeChanged?.Invoke(_drivingMode);
        }

        private void UpdateModeParameters()
        {
            float targetBlend = _drivingMode == VehicleDrivingMode.Fast ? 1f : 0f;

            // 전환 시간에 걸쳐 서서히 이동시켜 회전력이 계단처럼 튀지 않게 한다
            _modeBlend = _vehicleConfig.ModeTransitionSeconds > 0f
                ? Mathf.MoveTowards(_modeBlend, targetBlend,
                                    Time.fixedDeltaTime / _vehicleConfig.ModeTransitionSeconds)
                : targetBlend;

            VehicleConfig.DrivingModeSettings normal = _vehicleConfig.GetModeSettings(VehicleDrivingMode.Normal);
            VehicleConfig.DrivingModeSettings fast = _vehicleConfig.GetModeSettings(VehicleDrivingMode.Fast);

            _maxForwardSpeed = Mathf.Lerp(normal.MaxForwardSpeed, fast.MaxForwardSpeed, _modeBlend);
            _acceleration = Mathf.Lerp(normal.Acceleration, fast.Acceleration, _modeBlend);
            _maxTurnRate = Mathf.Lerp(normal.MaxTurnRate, fast.MaxTurnRate, _modeBlend);
            _lateralGrip = Mathf.Lerp(normal.LateralGrip, fast.LateralGrip, _modeBlend);
            _steerSmoothing = Mathf.Lerp(normal.SteerSmoothing, fast.SteerSmoothing, _modeBlend);
        }

        private void UpdateDriftState()
        {
            // 작업 중에는 드리프트를 막는다(기획서 16.2-1)
            bool isBusy = _interactionController != null && _interactionController.IsInteracting;

            _isDrifting = _isGrounded
                && _isDriftHeld
                && isBusy == false
                && _forwardSpeed >= _vehicleConfig.DriftMinForwardSpeed;
        }

        /// <summary>
        /// 바퀴 4개에서 스피어캐스트로 지면을 읽고 서스펜션 힘을 인가한다.
        /// 콜라이더 접촉 대신 캐스트를 쓰면 타일 경계에서 접촉 매니폴드가 재구성되지 않아 충격이 없다.
        /// </summary>
        private void ApplySuspension()
        {
            // 캐스트를 바퀴 중심보다 위에서 시작해, 차체가 처져도 구가 지면에 파묻혀 히트를 놓치지 않게 한다
            float castLift = _suspensionTravel;
            float maxDistance = castLift + _suspensionRadius + _suspensionTravel;

            // 스프링이 무게를 받아 압축되는 만큼 목표 높이를 보정해, 정지 시 목표 높이에 서게 한다
            float sag = Physics.gravity.magnitude / (_wheelOffsets.Length * _springStrength);
            float targetHeight = _rideHeight + sag;

            Vector3 rootPosition = _rigidbody.position;
            Quaternion rootRotation = _rigidbody.rotation;
            Vector3 normalSum = Vector3.zero;
            bool hasGround = false;

            for (int i = 0; i < _wheelOffsets.Length; i++)
            {
                Vector3 origin = rootPosition + rootRotation * _wheelOffsets[i] + Vector3.up * castLift;

                if (Physics.SphereCast(origin, _suspensionRadius, Vector3.down, out RaycastHit hit,
                                       maxDistance, _groundMask, QueryTriggerInteraction.Ignore) == false)
                {
                    continue;
                }

                // 위쪽을 향하는 법선만 지면으로 인정해 벽·장애물을 걸러낸다
                if (hit.normal.y < _groundNormalMinDot)
                {
                    continue;
                }

                float height = hit.distance - castLift + _suspensionRadius;
                float compression = targetHeight - height;
                float normalVelocity = Vector3.Dot(_rigidbody.GetPointVelocity(hit.point), hit.normal);
                float force = _springStrength * compression - _springDamper * normalVelocity;

                _rigidbody.AddForceAtPosition(hit.normal * force, hit.point, ForceMode.Acceleration);

                normalSum += hit.normal;
                hasGround = true;
            }

            if (hasGround)
            {
                _lastGroundedTime = Time.fixedTime;
            }

            // 경계에서 접지가 한두 프레임 끊겨도 조작이 리셋되지 않도록 짧은 유예를 둔다
            _isGrounded = hasGround || Time.fixedTime - _lastGroundedTime <= _groundContactGraceSeconds;

            Vector3 targetNormal = hasGround ? normalSum.normalized : Vector3.up;
            float t = 1f - Mathf.Exp(-_groundNormalSmoothing * Time.fixedDeltaTime);
            Vector3 smoothed = Vector3.Slerp(_groundNormal, targetNormal, t);

            // 경계에서 법선이 튀어 0에 가까워지면 이후 계산이 무너지므로 방어한다
            _groundNormal = smoothed.sqrMagnitude > 0.0001f ? smoothed.normalized : Vector3.up;
        }

        private void GetGroundBasis(out Vector3 forward, out Vector3 right)
        {
            forward = Vector3.ProjectOnPlane(_rigidbody.transform.forward, _groundNormal);

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(_rigidbody.transform.up, _groundNormal);
            }

            forward.Normalize();
            right = Vector3.Cross(_groundNormal, forward);
        }

        private void ApplyDrive()
        {
            GetGroundBasis(out Vector3 forward, out _);

            float forwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, forward);
            float targetSpeed = GetTargetForwardSpeed();
            float changeRate = GetSpeedChangeRate(forwardSpeed);
            float nextSpeed = Mathf.MoveTowards(forwardSpeed, targetSpeed, changeRate * Time.fixedDeltaTime);

            // 속도 보정을 힘으로 인가해 접촉·경사·충돌이 그 위에 더해지게 한다
            _rigidbody.AddForce(forward * ((nextSpeed - forwardSpeed) / Time.fixedDeltaTime),
                                ForceMode.Acceleration);

            _forwardSpeed = nextSpeed;
        }

        private float GetTargetForwardSpeed()
        {
            float throttle = _input.y;

            return throttle >= 0f
                ? throttle * _maxForwardSpeed
                : throttle * _vehicleConfig.MaxReverseSpeed;
        }

        private float GetSpeedChangeRate(float forwardSpeed)
        {
            float throttle = _input.y;

            if (Mathf.Approximately(throttle, 0f))
            {
                return _vehicleConfig.CoastDeceleration;
            }

            if (Mathf.Abs(forwardSpeed) > BrakeSpeedThreshold
                && Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed))
            {
                return _vehicleConfig.BrakeDeceleration;
            }

            return _acceleration;
        }

        private void ApplySteering()
        {
            _steer = Mathf.MoveTowards(_steer, _input.x, _steerSmoothing * Time.fixedDeltaTime);

            float turnRate = _isDrifting ? _maxTurnRate * _vehicleConfig.DriftTurnMultiplier : _maxTurnRate;
            float speedRatio = Mathf.Clamp01(Mathf.Abs(_forwardSpeed) / _maxForwardSpeed);
            float turnMultiplier = _vehicleConfig.TurnBySpeed.Evaluate(speedRatio);

            float targetYaw = _steer * turnRate * turnMultiplier
                            * Mathf.Sign(_forwardSpeed) * Mathf.Deg2Rad;

            // 지면 법선 축의 회전만 교체하고, 피치/롤 회전은 물리에 맡김
            Vector3 angularVelocity = _rigidbody.angularVelocity;
            float currentYaw = Vector3.Dot(angularVelocity, _groundNormal);

            _rigidbody.angularVelocity = angularVelocity + _groundNormal * (targetYaw - currentYaw);
        }

        private void ApplyLateralGrip()
        {
            GetGroundBasis(out _, out Vector3 right);

            float lateralSpeed = Vector3.Dot(_rigidbody.linearVelocity, right);
            float grip = _isDrifting ? _lateralGrip * _vehicleConfig.DriftGripMultiplier : _lateralGrip;
            float nextLateral = Mathf.Lerp(lateralSpeed, 0f, 1f - Mathf.Exp(-grip * Time.fixedDeltaTime));

            // 횡방향 감쇠도 힘으로 인가해, 접지력이 부족한 상황(드리프트·경사)이 물리에 반영되게 한다
            _rigidbody.AddForce(right * ((nextLateral - lateralSpeed) / Time.fixedDeltaTime),
                                ForceMode.Acceleration);
        }

        private void KeepUpright()
        {
            Vector3 up = _rigidbody.transform.up;
            Vector3 targetUp = _isGrounded ? _groundNormal : Vector3.up;

            // 현재 위쪽 → 목표 위쪽으로 돌리는 축 (각도가 클수록 큼)
            Vector3 correction = Vector3.Cross(up, targetUp) * _uprightStrength;

            // 기울기 방향 회전만 감쇠 (조향 회전은 건드리지 않음)
            Vector3 angularVelocity = _rigidbody.angularVelocity;
            Vector3 tiltVelocity = angularVelocity - up * Vector3.Dot(angularVelocity, up);
            correction -= tiltVelocity * _uprightDamping;

            float factor = _isGrounded ? 1f : _airUprightFactor;
            _rigidbody.AddTorque(correction * factor, ForceMode.Acceleration);
        }
    }
}
