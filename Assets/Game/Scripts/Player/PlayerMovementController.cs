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
            Vector3[] wheelOffsets = _vehicleConfig.WheelOffsets;

            // 캐스트를 바퀴 중심보다 위에서 시작해, 차체가 처져도 구가 지면에 파묻혀 히트를 놓치지 않게 한다
            float castLift = _vehicleConfig.SuspensionTravel;
            float maxDistance = castLift + _vehicleConfig.SuspensionRadius + _vehicleConfig.SuspensionTravel;

            // 스프링이 무게를 받아 압축되는 만큼 목표 높이를 보정해, 정지 시 목표 높이에 서게 한다
            float sag = Physics.gravity.magnitude / (wheelOffsets.Length * _vehicleConfig.SpringStrength);
            float targetHeight = _vehicleConfig.RideHeight + sag;

            Vector3 rootPosition = _rigidbody.position;
            Quaternion rootRotation = _rigidbody.rotation;
            Vector3 normalSum = Vector3.zero;
            bool hasGround = false;

            for (int i = 0; i < wheelOffsets.Length; i++)
            {
                Vector3 origin = rootPosition + rootRotation * wheelOffsets[i] + Vector3.up * castLift;

                if (Physics.SphereCast(origin, _vehicleConfig.SuspensionRadius, Vector3.down, out RaycastHit hit,
                                       maxDistance, _vehicleConfig.GroundMask, QueryTriggerInteraction.Ignore) == false)
                {
                    continue;
                }

                // 위쪽을 향하는 법선만 지면으로 인정해 벽·장애물을 걸러낸다
                if (hit.normal.y < _vehicleConfig.GroundNormalMinDot)
                {
                    continue;
                }

                float height = hit.distance - castLift + _vehicleConfig.SuspensionRadius;
                float compression = targetHeight - height;
                float normalVelocity = Vector3.Dot(_rigidbody.GetPointVelocity(hit.point), hit.normal);
                float force = _vehicleConfig.SpringStrength * compression
                            - _vehicleConfig.SpringDamper * normalVelocity;

                _rigidbody.AddForceAtPosition(hit.normal * force, hit.point, ForceMode.Acceleration);

                normalSum += hit.normal;
                hasGround = true;
            }

            if (hasGround)
            {
                _lastGroundedTime = Time.fixedTime;
            }

            // 경계에서 접지가 한두 프레임 끊겨도 조작이 리셋되지 않도록 짧은 유예를 둔다
            _isGrounded = hasGround
                || Time.fixedTime - _lastGroundedTime <= _vehicleConfig.GroundContactGraceSeconds;

            Vector3 targetNormal = hasGround ? normalSum.normalized : Vector3.up;
            float t = 1f - Mathf.Exp(-_vehicleConfig.GroundNormalSmoothing * Time.fixedDeltaTime);
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

            if (Mathf.Abs(forwardSpeed) > _vehicleConfig.BrakeSpeedThreshold
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
            Vector3 correction = Vector3.Cross(up, targetUp) * _vehicleConfig.UprightStrength;

            // 기울기 방향 회전만 감쇠 (조향 회전은 건드리지 않음)
            Vector3 angularVelocity = _rigidbody.angularVelocity;
            Vector3 tiltVelocity = angularVelocity - up * Vector3.Dot(angularVelocity, up);
            correction -= tiltVelocity * _vehicleConfig.UprightDamping;

            float factor = _isGrounded ? 1f : _vehicleConfig.AirUprightFactor;
            _rigidbody.AddTorque(correction * factor, ForceMode.Acceleration);
        }
    }
}
