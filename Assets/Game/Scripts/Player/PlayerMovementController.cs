using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    // 파일명과 클래스명이 다르면 Unity가 스크립트를 바인딩하지 못해 컴포넌트가 로드되지 않는다
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private InputActionReference _playerMoveInput;

        [Header("Speed")]
        [SerializeField] private float _maxForwardSpeed = 12f;
        [SerializeField] private float _maxReverseSpeed = 5f;

        [Header("Acceleration (m/s²)")]
        [SerializeField] private float _acceleration = 8f;
        [SerializeField] private float _brakeDeceleration = 20f;
        [SerializeField] private float _coastDeceleration = 4f;

        [Header("Steering")]
        [SerializeField] private float _maxTurnRate = 100f;
        [SerializeField]
        private AnimationCurve _turnBySpeed = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.6f));
        [SerializeField] private float _steerSmoothing = 5f;

        [Header("Grip")]
        [Tooltip("옆방향 미끄러짐이 줄어드는 비율(/초). 클수록 접지력이 높아 코너에서 덜 미끄러진다")]
        [SerializeField] private float _lateralGrip = 6f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private LayerMask _ignoreMask = ~0;
        [Tooltip("피벗에서 바닥까지의 거리보다 약간 크게")]
        [SerializeField] private float _groundCheckDistance = 0.7f;
        [Tooltip("지면에 붙어 있다고 볼 여유 거리. 이보다 멀면 공중으로 취급")]
        [SerializeField] private float _groundStickRange = 0.15f;

        [Header("Self Righting")]
        [Tooltip("차체를 지면(공중이면 수직)에 맞추는 힘")]
        [SerializeField] private float _uprightStrength = 12f;
        [Tooltip("기울기 흔들림을 잡는 감쇠")]
        [SerializeField] private float _uprightDamping = 4f;
        [Tooltip("공중에서의 복원력 비율")]
        [SerializeField, Range(0f, 1f)] private float _airUprightFactor = 0.3f;

        private const float GroundOriginOffset = 0.2f;

        private Vector2 _input;
        private float _steer;
        private float _forwardSpeed;
        private bool _isGrounded;
        private Vector3 _groundNormal = Vector3.up;
        private Vector3 _groundPoint;
        private float _pivotToBottom;

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponentInChildren<Rigidbody>();
                Debug.LogWarning($"Rigidbody is not assigned in {gameObject.name}");
                if (_rigidbody == null)
                {
                    Debug.LogError($"Rigidbody is missing in {gameObject.name}");
                }                
            }

            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;

            // 무게중심을 낮춰 쉽게 뒤집히지 않게
            // _rigidbody.centerOfMass = new Vector3(0f, -0.5f, 0f);

            // 지면에 붙일 때 쓸 피벗 → 콜라이더 최저점 거리
            float bottom = float.MaxValue;
            foreach (Collider c in GetComponentsInChildren<Collider>())
                if (c.enabled) bottom = Mathf.Min(bottom, c.bounds.min.y);

            if (bottom < float.MaxValue)
                _pivotToBottom = bottom - _rigidbody.position.y;
        }

        private void OnEnable() => _playerMoveInput.action.Enable();
        private void OnDisable() => _playerMoveInput.action.Disable();

        private void Update()
        {
            _input = _playerMoveInput.action.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            CheckGround();

            if (_isGrounded)
            {
                Drive();
                Steer();
                StickToGround();
            }

            KeepUpright();
        }

        private void CheckGround()
        {
            Transform transform = _rigidbody.transform;
            Vector3 origin = _rigidbody.position + (transform.up * GroundOriginOffset);

            _isGrounded = Physics.Raycast(origin, -transform.up, out RaycastHit hit,
                                          _groundCheckDistance + GroundOriginOffset,
                                          _groundMask);

            if (!_isGrounded)
            {
                _groundNormal = Vector3.up;
                return;
            }

            // 도로와는 물리 충돌하지 않으므로, 지면에 충분히 가까울 때만 접지로 본다
            _groundPoint = hit.point;
            _isGrounded = Mathf.Abs(_rigidbody.position.y - (hit.point.y - _pivotToBottom)) <= _groundStickRange;
            _groundNormal = _isGrounded ? hit.normal : Vector3.up;
        }

        private void StickToGround()
        {
            // 물리 접촉 대신 레이캐스트로 지면 높이에 붙인다
            Vector3 position = _rigidbody.position;
            position.y = _groundPoint.y - _pivotToBottom;
            _rigidbody.position = position;
        }

        private void Drive()
        {
            Vector3 velocity = _rigidbody.linearVelocity;

            // 차체 방향을 지면 평면에 투영 → 기울어져도, 경사에서도 정상 이동
            Vector3 forward = Vector3.ProjectOnPlane(_rigidbody.transform.forward, _groundNormal).normalized;
            Vector3 right = Vector3.Cross(_groundNormal, forward);

            float forwardSpeed = Vector3.Dot(velocity, forward);
            float lateralSpeed = Vector3.Dot(velocity, right);

            forwardSpeed = UpdateForwardSpeed(forwardSpeed);
            lateralSpeed = UpdateLateralSpeed(lateralSpeed);

            // 도로와 물리 충돌하지 않으므로 지면 방향 성분은 버린다 (튕김 방지)
            _rigidbody.linearVelocity = forward * forwardSpeed + right * lateralSpeed;

            _forwardSpeed = forwardSpeed;
        }

        private float UpdateForwardSpeed(float forwardSpeed)
        {
            float throttle = _input.y;

            float targetSpeed = throttle >= 0f
                ? throttle * _maxForwardSpeed
                : throttle * _maxReverseSpeed;

            float rate;
            if (Mathf.Approximately(throttle, 0f))
                rate = _coastDeceleration;
            else if (Mathf.Abs(forwardSpeed) > 0.1f && Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed))
                rate = _brakeDeceleration;
            else
                rate = _acceleration;

            return Mathf.MoveTowards(forwardSpeed, targetSpeed, rate * Time.fixedDeltaTime);
        }

        private float UpdateLateralSpeed(float lateralSpeed)
        {
            float t = 1f - Mathf.Exp(-_lateralGrip * Time.fixedDeltaTime);
            return Mathf.Lerp(lateralSpeed, 0f, t);
        }

        private void Steer()
        {
            _steer = Mathf.MoveTowards(_steer, _input.x, _steerSmoothing * Time.fixedDeltaTime);

            float speedRatio = Mathf.Clamp01(Mathf.Abs(_forwardSpeed) / _maxForwardSpeed);
            float turnMultiplier = _turnBySpeed.Evaluate(speedRatio);

            float targetYaw = _steer * _maxTurnRate * turnMultiplier
                            * Mathf.Sign(_forwardSpeed) * Mathf.Deg2Rad;

            // 지면 법선 축의 회전만 교체하고, 피치/롤 회전은 물리에 맡김
            Vector3 angularVelocity = _rigidbody.angularVelocity;
            float currentYaw = Vector3.Dot(angularVelocity, _groundNormal);

            _rigidbody.angularVelocity = angularVelocity + _groundNormal * (targetYaw - currentYaw);
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