using System;

using UnityEngine;
using UnityEngine.InputSystem;

using Unity.Profiling;

using Game.Manager;
using Game.Player.Interaction;
using Game.Player.Movement;
using Game.Systems;

namespace Game.Player.Recovery
{
    /// <summary>
    /// 차량 복구(리셋). R 홀드로 마지막 안전 지점에 복구하고 잠금 시간 동안 조작을 막는다.
    /// 규칙: design/gdd/game-concept.md 2.1·10.6·16.5, Story 007. 복구 이동은 충돌이 아니므로 내구도 컨트롤러와 무관하다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMovementController), typeof(PlayerInteractionController))]
    public class VehicleRecoveryController : MonoBehaviour
    {
        private static readonly ProfilerMarker SafePointCheckMarker = new("VehicleRecovery.SafePointCheck");

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private VehicleRecoveryConfig _recoveryConfig;
        [Tooltip("지면 판정(GroundMask, WheelOffsets, GroundNormalMinDot)을 주행과 같은 값으로 쓰기 위한 참조")]
        [SerializeField] private VehicleConfig _vehicleConfig;

        [Header("Input")]
        // 입력 에셋(InputSystem_Actions)에 리셋 액션이 없어 컴포넌트가 직접 가진다. 인스펙터에서 바인딩을 바꿀 수 있다
        [SerializeField] private InputAction _recoveryInput = new("Recovery", InputActionType.Button, "<Keyboard>/r");

        [Header("Fallback")]
        [Tooltip("쓸 수 있는 안전 지점이 없을 때 옮겨 갈 물류센터 앞 지정 위치. 이 Transform의 앞 방향을 본다")]
        [SerializeField] private Transform _logisticsCenterFront;

        private readonly Collider[] _overlapBuffer = new Collider[16];

        private PlayerMovementController _movement;
        private PlayerInteractionController _interaction;
        private SafePointBuffer _safePoints;
        private Collider[] _bodyColliders;

        private float _recordTimer;
        private bool _isHolding;
        private float _holdSeconds;
        private float _lockEndTime = float.NegativeInfinity;
        private bool _isRecoveryLocked;

        private float _stuckTimer;
        private bool _isStuck;

        private bool _lastIconVisible;
        private bool _lastIconLocked;

        /// <summary>복구 홀드 진행도(0~1). 진행 중이 아니면 isActive=false로 발생한다. 상·하차 진행 바와 같은 시그니처다.</summary>
        public event Action<float, bool> RecoveryProgressChanged;

        /// <summary>복구 패널 상태. visible은 갇힘 또는 R 홀드 중이면서 운영 중이고 잠금이 아닐 때, locked는 복구 후 잠금 중일 때다.</summary>
        public event Action<bool, bool> KeyIconStateChanged;

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (_recoveryConfig == null || _vehicleConfig == null)
            {
                Debug.LogError($"VehicleRecoveryConfig or VehicleConfig is not assigned in {gameObject.name}");
                enabled = false;
                return;
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            _movement = GetComponent<PlayerMovementController>();
            _interaction = GetComponent<PlayerInteractionController>();
            _safePoints = new SafePointBuffer(_recoveryConfig.BufferCapacity);

            // 트리거(상호작용 구역)는 겹침 판정 대상이 아니다
            var colliders = new System.Collections.Generic.List<Collider>();
            foreach (Collider body in _rigidbody.GetComponentsInChildren<Collider>())
            {
                if (body.isTrigger == false)
                {
                    colliders.Add(body);
                }
            }

            _bodyColliders = colliders.ToArray();
        }

        private void OnEnable()
        {
            _recoveryInput.Enable();
        }

        private void OnDisable()
        {
            _recoveryInput.Disable();
        }

        private bool IsOperating => DailyOperationSystem.Instance != null && DailyOperationSystem.Instance.IsOperating && GameManager.IsPaused == false;

        private void Update()
        {
            bool isLocked = Time.time < _lockEndTime;
            if (_isRecoveryLocked && isLocked == false)
            {
                _isRecoveryLocked = false;
                _movement.SetRecoveryLocked(false);
            }

            bool canUse = IsOperating && isLocked == false;
            UpdateStuck(canUse);
            UpdateHold(canUse);

            // 홀드 끝에 복구가 실행되면 잠금이 이번 프레임에 시작되므로 잠금 상태를 다시 읽는다
            isLocked = Time.time < _lockEndTime;
            UpdateKeyIcon(canUse && isLocked == false && (_isStuck || _isHolding), isLocked);
        }

        /// <summary>뒤집힘 또는 끼임(입력이 있는데 수평 속도가 v_stuck 미만)이 T_stuck 동안 계속되면 갇힘, 둘 다 풀리면 즉시 해제.</summary>
        private void UpdateStuck(bool canUse)
        {
            bool flipped = Vector3.Dot(_rigidbody.rotation * Vector3.up, Vector3.up) < _recoveryConfig.MinBodyUpDot;
            Vector3 velocity = _rigidbody.linearVelocity;
            bool wedged = _movement.HasThrottleInput
                && new Vector2(velocity.x, velocity.z).magnitude < _recoveryConfig.StuckSpeed;

            _stuckTimer = canUse && (flipped || wedged) ? _stuckTimer + Time.deltaTime : 0f;
            _isStuck = _stuckTimer >= _recoveryConfig.StuckSeconds;
        }

        private void UpdateKeyIcon(bool visible, bool locked)
        {
            if (visible == _lastIconVisible && locked == _lastIconLocked)
            {
                return;
            }

            _lastIconVisible = visible;
            _lastIconLocked = locked;
            KeyIconStateChanged?.Invoke(visible, locked);
        }

        private void UpdateHold(bool canUse)
        {
            if (_isHolding == false)
            {
                if (canUse && _recoveryInput.WasPressedThisFrame())
                {
                    _isHolding = true;
                    _holdSeconds = 0f;
                    RecoveryProgressChanged?.Invoke(0f, true);
                }

                return;
            }

            // 도중에 떼거나 하루가 끝나면 취소한다
            if (canUse == false || _recoveryInput.IsPressed() == false)
            {
                EndHold();
                return;
            }

            _holdSeconds += Time.deltaTime;
            if (_holdSeconds < _recoveryConfig.HoldSeconds)
            {
                RecoveryProgressChanged?.Invoke(_holdSeconds / _recoveryConfig.HoldSeconds, true);
                return;
            }

            EndHold();
            Recover();
        }

        private void EndHold()
        {
            _isHolding = false;
            _holdSeconds = 0f;
            RecoveryProgressChanged?.Invoke(0f, false);
        }

        private void FixedUpdate()
        {
            _recordTimer += Time.fixedDeltaTime;
            if (_recordTimer < _recoveryConfig.RecordIntervalSeconds)
            {
                return;
            }

            _recordTimer = 0f;
            if (IsOperating && Time.time >= _lockEndTime)
            {
                TryRecordSafePoint();
            }
        }

        /// <summary>기울기 → 바퀴 4개 접지 → 겹침 순으로(싼 검사부터) 확인하고 모두 통과하면 기록한다.</summary>
        private void TryRecordSafePoint()
        {
            using (SafePointCheckMarker.Auto())
            {
                Transform root = _rigidbody.transform;
                if (Vector3.Dot(_rigidbody.rotation * Vector3.up, Vector3.up) < _recoveryConfig.MinBodyUpDot
                    || AreAllWheelsGrounded() == false
                    || Overlaps(_rigidbody.position, _rigidbody.rotation))
                {
                    return;
                }

                _safePoints.Record(new SafePoint(_rigidbody.position, FlattenForward(root.forward), Time.time));
            }
        }

        // 주행 컨트롤러의 서스펜션 캐스트와 같은 시작점·반지름·길이로 4바퀴 각각의 접지를 읽는다
        private bool AreAllWheelsGrounded()
        {
            Vector3[] wheelOffsets = _vehicleConfig.WheelOffsets;
            float castLift = _vehicleConfig.SuspensionTravel;
            float maxDistance = castLift + _vehicleConfig.SuspensionRadius + _vehicleConfig.SuspensionTravel;

            for (int i = 0; i < wheelOffsets.Length; i++)
            {
                Vector3 origin = _rigidbody.position + _rigidbody.rotation * wheelOffsets[i] + Vector3.up * castLift;

                if (Physics.SphereCast(origin, _vehicleConfig.SuspensionRadius, Vector3.down, out RaycastHit hit,
                                       maxDistance, _vehicleConfig.GroundMask, QueryTriggerInteraction.Ignore) == false
                    || hit.normal.y < _vehicleConfig.GroundNormalMinDot)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>차량 루트가 주어진 자세일 때 차체 콜라이더가 지면 외의 다른 콜라이더와 겹치는지.</summary>
        private bool Overlaps(Vector3 rootPosition, Quaternion rootRotation)
        {
            Transform root = _rigidbody.transform;
            Quaternion delta = rootRotation * Quaternion.Inverse(root.rotation);
            int mask = ~_vehicleConfig.GroundMask.value;

            for (int i = 0; i < _bodyColliders.Length; i++)
            {
                Collider body = _bodyColliders[i];
                Vector3 position = rootPosition + delta * (body.transform.position - root.position);
                Quaternion rotation = delta * body.transform.rotation;

                // 콜라이더 center 오프셋까지 덮도록 구의 중심은 bounds 중심 기준으로 옮긴다
                Vector3 sphereCenter = rootPosition + delta * (body.bounds.center - root.position);
                int count = Physics.OverlapSphereNonAlloc(sphereCenter, body.bounds.extents.magnitude, _overlapBuffer,
                                                          mask, QueryTriggerInteraction.Ignore);
                for (int j = 0; j < count; j++)
                {
                    Collider other = _overlapBuffer[j];
                    if (other.attachedRigidbody == _rigidbody)
                    {
                        continue;
                    }

                    if (Physics.ComputePenetration(body, position, rotation,
                                                   other, other.transform.position, other.transform.rotation,
                                                   out _, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Vector3 FlattenForward(Vector3 forward)
        {
            return Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        }

        private static Quaternion UprightRotation(Vector3 flatForward)
        {
            return Quaternion.LookRotation(flatForward, Vector3.up);
        }

        private void Recover()
        {
            SafePoint target;
            bool found = _safePoints.TryPick(Time.time, _recoveryConfig.MinAgeSeconds,
                point => Overlaps(point.Position, UprightRotation(point.Forward)), out target);

            if (found == false)
            {
                if (_logisticsCenterFront == null)
                {
                    Debug.LogWarning($"Logistics center front is not assigned in {gameObject.name}; recovery skipped");
                    return;
                }

                target = new SafePoint(_logisticsCenterFront.position,
                                       FlattenForward(_logisticsCenterFront.forward), Time.time);
            }

            // 진행 중이던 상·하차를 중단하고 현재 물류의 진행도를 초기화한다(기획서 10.6)
            _interaction.EndHoldLoad();

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.position = target.Position;
            _rigidbody.rotation = UprightRotation(target.Forward);

            _lockEndTime = Time.time + _recoveryConfig.LockSeconds;
            _isRecoveryLocked = true;
            _movement.SetRecoveryLocked(true);
        }
    }
}
