using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Movement
{
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private InputActionReference _playerMoveInput;

        [Header("Movement Configuration")]
        [SerializeField]
        private float _movePower = 20f;       // 값이 작으면 가속이 느립니다. (기존 5에서 상향 조절 필요)
        [SerializeField]
        private float _maxMoveSpeed = 15f;    // 자동차의 최대 직진 속도 제한
        [SerializeField]
        private float _linearDamping = 2f;    // 직진 가감속 부드러움 (높을수록 키를 뗐을 때 빨리 멈춤)

        [Header("Rotation Configuration")]
        [SerializeField]
        private float _turnPower = 5f;        // angularVelocity 기준이므로 3~8 사이로 조절하세요.
        [SerializeField]
        private float _angularDamping = 8f;   // 회전 가감속 부드러움 (높을수록 회전이 칼같이 멈춤)

        // 차량의 뒷바퀴 모델의 위치 (양수 값으로 거리만 활용)
        private const float OffsetToRearAxle = 0.61f;

        private Vector2 _inputDirection = Vector2.zero;

        private void Awake()
        {
            if (_rigidbody == null)
            {
                Debug.LogWarning("Rigidbody component is not assigned in the inspector.");
                _rigidbody = GetComponentInChildren<Rigidbody>();
                if (_rigidbody == null)
                {
                    Debug.LogError("Rigidbody component is not found in the player's children.");
                    enabled = false;
                }
            }

            if (_playerMoveInput == null)
            {
                Debug.LogWarning("InputActionReference is not assigned in the inspector.");
                enabled = false;
            }
        }

        private void Start()
        {
            // 리지드바디의 내장 댐핑 프로퍼티 설정
            _rigidbody.linearDamping = _linearDamping;
            _rigidbody.angularDamping = _angularDamping;
        }

        private void OnEnable()
        {
            _playerMoveInput.action.Enable();
            _playerMoveInput.action.performed += HandleInput;
            _playerMoveInput.action.canceled += HandleInput;
        }

        private void OnDisable()
        {
            _playerMoveInput.action.performed -= HandleInput;
            _playerMoveInput.action.canceled -= HandleInput;
            _playerMoveInput.action.Disable();
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            // 가감속 방식에서는 댐핑 수치가 인스펙터에서 실시간 변경될 수 있으므로 동기화 (에디터에서만)
            _rigidbody.linearDamping = _linearDamping;
            _rigidbody.angularDamping = _angularDamping;
#endif
            Turn();
            Move();
        }

        private void HandleInput(InputAction.CallbackContext context)
        {
            _inputDirection = context.ReadValue<Vector2>();
        }

        private void Turn()
        {
            if (_inputDirection.x == 0f || _inputDirection.y == 0f)
                return;

            float steeringInput = _inputDirection.x;
            float movementInput = _inputDirection.y;

            float turnAmount = steeringInput * movementInput * _turnPower * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, turnAmount, 0f);

            Vector3 pivotPosition = _rigidbody.position + transform.forward * OffsetToRearAxle;
            Vector3 targetPosition = pivotPosition - deltaRotation * (pivotPosition - _rigidbody.position);

            _rigidbody.MoveRotation(deltaRotation * _rigidbody.rotation);
            _rigidbody.MovePosition(targetPosition);
        }

        private void Move()
        {
            // 가해줄 힘의 방향과 크기
            Vector3 moveForce = transform.forward * _inputDirection.y * _movePower;
            moveForce.y = 0f; // 수직 방향 힘 제거 (차량이 공중에 뜨는 것을 방지)

            // 전륜 차량 물리 궤적
            _rigidbody.AddForce(moveForce, ForceMode.Acceleration);

            // 최고 속도 제한
            if (_rigidbody.linearVelocity.magnitude > _maxMoveSpeed)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maxMoveSpeed;
            }
        }
    }
}
