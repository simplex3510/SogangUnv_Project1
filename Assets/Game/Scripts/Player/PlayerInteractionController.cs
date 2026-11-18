using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Interaction;
using Game.Logistics;

namespace Game.Player.Interaction
{
    /// <summary>물류 하나 단위로 상차·하차를 진행한다. 상호작용 키를 유지하는 동안 현재 물류의 진행도가 쌓이고, 완료되면 다음 물류로 넘어간다.</summary>
    // 같은 프레임의 시간 판정(영업 종료·제한 시간)이 끝난 뒤 완료를 판정한다(기획서 3.7)
    [DefaultExecutionOrder(300)]
    public class PlayerInteractionController : MonoBehaviour, IInteractor
    {
        /// <summary>적재 한도 초과로 상차할 수 없을 때의 차단 사유.</summary>
        public const string CargoFullReason = "적재 초과";
        /// <summary>무게 한도 초과로 상차할 수 없을 때의 차단 사유. LogisticsCenterUI도 이 값과 비교한다.</summary>
        public const string WeightLimitReason = "무게 초과";
        /// <summary>배송지에 내릴 물류가 없을 때의 차단 사유.</summary>
        public const string NoParcelForTargetReason = "이 배송지로 갈 물류 없음";
        /// <summary>처분할 상한 물류가 없을 때의 차단 사유.</summary>
        public const string NoParcelToDisposeReason = "처분할 물류 없음";
        /// <summary>물류센터에 실을 물류가 없을 때의 차단 사유.</summary>
        public const string NoParcelToLoadReason = "실을 물류 없음";

        [Header("Interact Input")]
        [SerializeField] private InputActionReference _interactionInput;

        [Header("Cargo Settings")]
        [Tooltip("차량 적재 한도·무게 한도 설정(기획서 8.5)")]
        [SerializeField] private VehicleCargoConfig _cargoConfig;

        // 적재한 물류. 배송지별 필터링과 개별 상태 추적에 사용한다.
        private readonly List<Parcel> _loadedParcels = new();

        // 물류센터 UI에서 고른 상차 대상. 비어 있으면 대기열 선두를 사용한다.
        private readonly List<Parcel> _loadSelection = new();

        // UI에서 직접 고른 경우에는 그룹을 다 상차한 뒤 다른 물류로 넘어가지 않는다
        private bool _hasExplicitSelection;

        // 상차 UI에서 고른 물류를 키 입력 없이 순서대로 상차하는 중인지
        private bool _isLoadRequested;

        // 현재 겹쳐 있는 구역 목록. 진입 순서를 유지하며, 마지막 원소가 상호작용 대상
        private readonly List<IInteractable> _detectedInteractables = new();

        // 차체 콜라이더가 여러 개라 같은 구역에 Enter/Exit가 콜라이더 수만큼 들어온다.
        // 마지막 콜라이더가 빠질 때만 목록에서 제거하기 위해 겹침 개수를 센다.
        private readonly Dictionary<IInteractable, int> _overlapCounts = new();

        private PlayerCargoImpactController _impactController;
        private IInteractable _currentInteractable;
        private Parcel _activeParcel;
        private float _progressSeconds;
        private bool _isHolding;
        private bool _isControlEnabled = true;
        private bool _lastCanInteract;
        private InteractionTypes _lastType = InteractionTypes.None;

        public event Action<bool, InteractionTypes> InteractionContextChanged;

        /// <summary>현재 상호작용의 진행도(0~1). 진행 중이 아니면 isActive=false로 발생한다.</summary>
        public event Action<float, bool> InteractionProgressChanged;

        /// <summary>현재 대상이 처분 장소인지. 처분 장소도 종류는 Unload라서 폐기 아이콘 구분용으로 읽는다.</summary>
        public bool IsTargetDisposalSite => _currentInteractable is DisposalSite;

        public event Action<Parcel> LoadedParcelsChanged;

        /// <summary>적재 물류가 폐기로 전환될 때 발생한다(원인 무관, 기획서 11.6). 폐기 알림 표시에 사용한다.</summary>
        public event Action<Parcel> ParcelSpoiled;

        /// <summary>상호작용이 막힌 이유. 안내 문구 표시에 사용한다.</summary>
        public event Action<string> InteractionBlocked;

        public IReadOnlyList<Parcel> LoadedParcels => _loadedParcels;
        public int CargoCapacity => _cargoConfig != null ? _cargoConfig.CargoCapacity : 0;
        public int WeightCapacity => _cargoConfig != null ? _cargoConfig.WeightCapacity : 0;
        public float MaxWeight => _cargoConfig != null ? _cargoConfig.MaxWeight : 0f;

        /// <summary>적재 무게가 무게 한도 W를 넘었는지. 폐기 물류도 처분 전까지 무게에 포함된다.</summary>
        public bool IsOverweight => UsedWeight > WeightCapacity;

        /// <summary>현재 무게의 속도 배율 m. 이동 컨트롤러는 적재 무게를 모르고 이 값만 받는다.</summary>
        public float SpeedMultiplier { get; private set; } = 1f;

        /// <summary>속도 배율을 다시 계산할 때마다 발생한다(상차·하차·처분 완료).</summary>
        public event Action<float> SpeedMultiplierChanged;

        // 그룹·선택 상차에서 이번 홀드에 이미 실은 물류 수. 첫 물류만 W 초과를 허용한다
        private int _loadedInHold;

        /// <summary>적재 무게가 바뀐 뒤 속도 배율을 다시 계산한다. 폐기 물류 처분 완료(Story 005) 시에도 호출한다.</summary>
        public void RecomputeSpeedMultiplier()
        {
            SpeedMultiplier = _cargoConfig != null ? _cargoConfig.GetSpeedMultiplier(UsedWeight) : 1f;
            SpeedMultiplierChanged?.Invoke(SpeedMultiplier);
        }

        /// <summary>그룹·선택 상차를 이어갈 수 있는지. 개별 한도에 더해, 이번 홀드에서 이미 실었다면 상차 후 무게가 W 이하여야 한다.</summary>
        public bool CanContinueGroupLoad(Parcel next)
        {
            if (CanLoad(next) == false)
            {
                return false;
            }

            return _loadedInHold == 0 || UsedWeight + next.Cargo.WeightUsage <= WeightCapacity;
        }

        // 적재 목록이 작아 매번 합산한다. 별도 누적값을 두면 상차·하차·실패 경로마다 동기화가 필요하다
        public int UsedCapacity
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _loadedParcels.Count; i++)
                {
                    sum += _loadedParcels[i].Cargo.CapacityUsage;
                }

                return sum;
            }
        }

        public int UsedWeight
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _loadedParcels.Count; i++)
                {
                    sum += _loadedParcels[i].Cargo.WeightUsage;
                }

                return sum;
            }
        }

        /// <summary>상차 후에도 적재 한도와 무게 한도를 모두 넘지 않는지(기획서 8.5).</summary>
        public bool CanLoad(Parcel parcel)
        {
            return parcel != null && GetLoadBlockReason(parcel) == null;
        }

        /// <summary>상차할 수 없는 이유. 둘 다 넘으면 적재 한도를 먼저 알린다. 실을 수 있으면 null.</summary>
        public string GetLoadBlockReason(Parcel parcel)
        {
            if (UsedCapacity + parcel.Cargo.CapacityUsage > CargoCapacity)
            {
                return CargoFullReason;
            }

            if (UsedWeight + parcel.Cargo.WeightUsage > MaxWeight)
            {
                return WeightLimitReason;
            }

            return null;
        }

        /// <summary>상차하면 무게 한도 W를 넘지만 W_max 이내라 실을 수 있는 물류인지(상차 UI 경고 행).</summary>
        public bool WouldExceedWeightCapacity(Parcel parcel)
        {
            return UsedWeight + parcel.Cargo.WeightUsage > WeightCapacity;
        }

        /// <summary>물류 하나를 처리하는 중인지. 주행 모드 전환·드리프트 차단에 사용한다.</summary>
        public bool IsInteracting => _activeParcel != null;

        public float GetProgress01()
        {
            if (_currentInteractable == null || _currentInteractable.InteractionDuration <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(_progressSeconds / _currentInteractable.InteractionDuration);
        }

        private void Awake()
        {
            if (_cargoConfig == null)
            {
                // 한도가 0이 되어 아무것도 실을 수 없으므로 배선 누락을 바로 알린다
                Debug.LogError($"VehicleCargoConfig is not assigned in {gameObject.name}");
            }
        }

        private void Start()
        {
            // 적재한 Fresh의 신선도 0도 내구도 0과 같은 폐기 경로를 쓴다(기획서 11.6). 시스템 Awake 순서에 의존하지 않으려고 Start에서 구독한다
            if (DeliveryDeadlineSystem.Instance != null)
            {
                DeliveryDeadlineSystem.Instance.FreshnessDepleted += SpoilParcel;
            }
        }

        private void OnDestroy()
        {
            if (DeliveryDeadlineSystem.Instance != null)
            {
                DeliveryDeadlineSystem.Instance.FreshnessDepleted -= SpoilParcel;
            }
        }

        private void OnEnable()
        {
            _interactionInput.action.Enable();

            // 내구도 0 도달은 같은 차량의 충돌 컨트롤러가 알린다(Story 004)
            _impactController = GetComponent<PlayerCargoImpactController>();
            if (_impactController != null)
            {
                _impactController.ParcelDepleted += SpoilParcel;
            }
        }

        private void OnDisable()
        {
            _interactionInput.action.Disable();

            if (_impactController != null)
            {
                _impactController.ParcelDepleted -= SpoilParcel;
            }
        }

        /// <summary>
        /// 적재 물류를 폐기로 전환한다(기획서 11.6). 원인과 무관하게 공통이며 내구도 0·신선도 0(Fresh)이 같은 경로를 쓴다.
        /// 배송 실패 확정(페널티는 정산이 집계), 목적지 재설정(처분 장소), 진행 중 하차 중단을 처리한다.
        /// </summary>
        public void SpoilParcel(Parcel parcel)
        {
            if (parcel == null || parcel.IsSpoiled || _loadedParcels.Contains(parcel) == false)
            {
                return;
            }

            parcel.Status = ParcelStatus.Spoiled;

            // 하차 진행 중이던 물류면 중단하고 진행도를 초기화한다(규칙 5)
            if (_activeParcel == parcel)
            {
                StopInteraction();
            }

            if (DeliveryDeadlineSystem.Instance != null)
            {
                DeliveryDeadlineSystem.Instance.NotifySpoiled(parcel);
            }

            // 원래 배송지의 아이콘에서 빠지고 적재·처분 UI가 갱신되도록 알린다. 적재·무게 사용량은 그대로다(규칙 4)
            parcel.Target.NotifyParcelChanged(parcel);
            LoadedParcelsChanged?.Invoke(parcel);
            ParcelSpoiled?.Invoke(parcel);
            Debug.Log($"[폐기] 물류 #{parcel.Id} 폐기 전환, 배송 실패 확정 {FormatUsage()}");
        }

        /// <summary>영업 종료 시 차량에 남은 폐기 물류를 정리한다. 페널티는 폐기 전환 때 이미 정산에 잡혔다(기획서 11.6-6).</summary>
        public void ClearSpoiledParcels()
        {
            if (_loadedParcels.RemoveAll(parcel => parcel.IsSpoiled) > 0)
            {
                RecomputeSpeedMultiplier();
                // 처분 장소 아이콘 등이 남지 않도록 갱신을 알린다(대상 물류가 여럿이라 null)
                LoadedParcelsChanged?.Invoke(null);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

            // 다른 콜라이더가 이미 들어와 있으면 개수만 늘린다
            if (_overlapCounts.TryGetValue(interactable, out int count))
            {
                _overlapCounts[interactable] = count + 1;
                return;
            }

            _overlapCounts.Add(interactable, 1);
            _detectedInteractables.Add(interactable);
        }

        private void OnTriggerExit(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null || _overlapCounts.TryGetValue(interactable, out int count) == false)
            {
                return;
            }

            // 마지막 콜라이더가 빠질 때만 구역에서 벗어난 것으로 본다
            if (count > 1)
            {
                _overlapCounts[interactable] = count - 1;
                return;
            }

            _overlapCounts.Remove(interactable);
            _detectedInteractables.Remove(interactable);
        }

        private void Update()
        {
            if (_isControlEnabled == false)
            {
                return;
            }

            RefreshInteractionState();

            bool isPressed = _interactionInput.action.IsPressed();
            if (isPressed && _isHolding == false)
            {
                _isHolding = true;
                OnInteractionPressed();
            }
            else if (isPressed == false && _isHolding)
            {
                _isHolding = false;
                StopInteraction();
            }

            if (_isHolding == false)
            {
                return;
            }

            // 물류센터 UI를 조작하는 동안에는 상호작용 키로 상차를 진행하지 않는다
            if (_currentInteractable is LogisticsCenter { IsUIOpen: true })
            {
                return;
            }

            TickInteraction(Time.deltaTime);
        }

        private void LateUpdate()
        {
            // 상차 UI에서 고른 물류는 키를 누르지 않아도 물류 1개당 소요시간을 거쳐 실린다
            if (_isLoadRequested == false || _isControlEnabled == false)
            {
                return;
            }

            // 다음 물류가 한도를 넘으면 TryBeginNextParcel이 선택을 비우므로 여기서 함께 멈춘다
            if (_currentInteractable is LogisticsCenter center
                && center.IsUIOpen
                && HasRemainingSelection(center))
            {
                TickInteraction(Time.deltaTime);
                return;
            }

            _isLoadRequested = false;
        }

        /// <summary>상호작용 키를 유지하는 동안 호출되어 진행도를 쌓는다. 검증용으로도 호출할 수 있다.</summary>
        public void TickInteraction(float deltaSeconds)
        {
            // 고른 물류가 남아 있으면 실을 수 있는지는 TryBeginNextParcel이 판정하고 막힌 이유를 알린다
            bool continuesSelection = _currentInteractable is LogisticsCenter selectedCenter && HasRemainingSelection(selectedCenter);
            if (_currentInteractable == null || (continuesSelection == false && CanInteract(_currentInteractable) == false))
            {
                StopInteraction();
                return;
            }

            // 물류센터 UI 열기는 물류 작업과 별도 소요시간을 쓴다
            if (_currentInteractable is LogisticsCenter center
                && center.IsUIOpen == false
                && HasRemainingSelection(center) == false)
            {
                TickUIOpen(center, deltaSeconds);
                return;
            }

            // 방해물은 물류 없이 InteractionDuration만큼 유지하면 실행한다(Story 010)
            if (_currentInteractable.InteractionType == InteractionTypes.Obstacle)
            {
                TickObstacle(_currentInteractable, deltaSeconds);
                return;
            }

            if (_activeParcel == null && TryBeginNextParcel() == false)
            {
                return;
            }

            _progressSeconds += deltaSeconds;
            if (_progressSeconds < _currentInteractable.InteractionDuration)
            {
                InteractionProgressChanged?.Invoke(GetProgress01(), true);
                return;
            }

            CompleteActiveParcel();
        }

        private void TickUIOpen(LogisticsCenter center, float deltaSeconds)
        {
            _progressSeconds += deltaSeconds;

            float duration = center.UIOpenDuration;
            if (duration > 0f && _progressSeconds < duration)
            {
                InteractionProgressChanged?.Invoke(Mathf.Clamp01(_progressSeconds / duration), true);
                return;
            }

            StopInteraction();
            Interact(center);
        }

        private void TickObstacle(IInteractable obstacle, float deltaSeconds)
        {
            _progressSeconds += deltaSeconds;
            if (_progressSeconds < obstacle.InteractionDuration)
            {
                InteractionProgressChanged?.Invoke(GetProgress01(), true);
                return;
            }

            StopInteraction();
            Interact(obstacle);
        }

        /// <summary>물류센터 UI가 고른 상차 대상을 넘긴다. 빈 목록이면 대기열 선두를 사용한다.</summary>
        public void SetLoadSelection(IReadOnlyList<Parcel> parcels)
        {
            _loadedInHold = 0;
            _loadSelection.Clear();
            _hasExplicitSelection = parcels != null && parcels.Count > 0;

            if (parcels == null)
            {
                return;
            }

            for (int i = 0; i < parcels.Count; i++)
            {
                if (parcels[i] != null)
                {
                    _loadSelection.Add(parcels[i]);
                }
            }
        }

        /// <summary>상차 UI에서 행을 누르고 있는 동안 물류를 순서대로 상차한다.</summary>
        public void BeginHoldLoad(IReadOnlyList<Parcel> parcels)
        {
            SetLoadSelection(parcels);
            _isLoadRequested = _hasExplicitSelection;
        }

        /// <summary>마우스를 놓거나 UI를 닫으면 진행 중인 상차를 취소하고 진행도를 초기화한다.</summary>
        public void EndHoldLoad()
        {
            _isLoadRequested = false;
            StopInteraction();
        }

        /// <summary>하루 종료 시 상호작용과 열려 있는 상차 UI를 즉시 멈춘다(기획서 3.5).</summary>
        public void SetControlEnabled(bool isEnabled)
        {
            _isControlEnabled = isEnabled;
            if (isEnabled)
            {
                return;
            }

            EndHoldLoad();
            CloseUIOf(_currentInteractable);

            // Update가 멈추므로 키 안내를 여기서 끈다. 다시 켜지면 Update가 현재 상태로 다시 알린다
            _lastCanInteract = false;
            _lastType = InteractionTypes.None;
            InteractionContextChanged?.Invoke(false, InteractionTypes.None);
        }

        /// <summary>대상의 상호작용을 실행한다. 물류센터는 상차 UI를 연다.</summary>
        public void Interact(IInteractable interactable)
        {
            if (interactable != null)
            {
                interactable.Interact();
            }
        }

        public bool CanInteract(IInteractable interactable)
        {
            if (interactable == null || interactable.CanInteract() == false)
            {
                return false;
            }

            if (interactable.InteractionType == InteractionTypes.Load)
            {
                // 실을 수 있는 물류가 하나라도 남아 있어야 물류센터와 상호작용한다
                return interactable is LogisticsCenter center && HasLoadableParcel(center);
            }

            if (interactable.InteractionType == InteractionTypes.Unload)
            {
                // 처분 장소는 폐기 물류를 실고 있을 때만 허용한다
                if (interactable is DisposalSite)
                {
                    return _loadedParcels.Exists(parcel => parcel.IsSpoiled);
                }

                // 해당 배송지로 배송할 물류를 실고 있을 때만 하차를 허용한다
                return interactable is DeliveryTarget target
                    && _loadedParcels.Exists(parcel => parcel.IsDeliverableTo(target));
            }

            // 방해물은 실행 가능 여부(1회·잠금·대상)를 자신이 판정한다
            return interactable.InteractionType == InteractionTypes.Obstacle;
        }

        /// <summary>구역 진입·이탈과 상태 변화를 매 프레임 반영해 프롬프트를 갱신한다. 상태가 바뀔 때만 이벤트를 발생시킨다.</summary>
        private void RefreshInteractionState()
        {
            IInteractable current = _detectedInteractables.Count > 0
                ? _detectedInteractables[_detectedInteractables.Count - 1]
                : null;

            if (current != _currentInteractable)
            {
                // 대상이 바뀌면 이전 작업을 중단하고 진행도를 초기화한다(기획서 16.2-4)
                StopInteraction();
                _isLoadRequested = false;
                CloseUIOf(_currentInteractable);
                if (_currentInteractable is LogisticsCenter)
                {
                    _loadSelection.Clear();
                    _hasExplicitSelection = false;
                }

                _currentInteractable = current;
            }

            bool canInteract = CanInteract(_currentInteractable);
            InteractionTypes type = canInteract ? _currentInteractable.InteractionType : InteractionTypes.None;

            if (canInteract == _lastCanInteract && type == _lastType)
            {
                return;
            }

            _lastCanInteract = canInteract;
            _lastType = type;
            InteractionContextChanged?.Invoke(canInteract, type);
        }

        private void OnInteractionPressed()
        {
            _loadedInHold = 0;

            if (_currentInteractable == null)
            {
                return;
            }

            if (_currentInteractable is LogisticsCenter center)
            {
                if (CanInteract(center) == false)
                {
                    InteractionBlocked?.Invoke(GetBlockedReason(center));
                }

                // UI 열기와 상차는 키를 유지하는 동안 TickInteraction이 진행한다
                return;
            }

            if (CanInteract(_currentInteractable))
            {
                return;
            }

            string reason = GetBlockedReason(_currentInteractable);
            if (string.IsNullOrEmpty(reason) == false)
            {
                InteractionBlocked?.Invoke(reason);
            }
        }

        private string GetBlockedReason(IInteractable interactable)
        {
            // 배송지에 도착했지만 배송할 물류가 없는 경우에만 사유를 알린다(기획서 16.2-6)
            if (interactable is DeliveryTarget target
                && _loadedParcels.Exists(parcel => parcel.IsDeliverableTo(target)) == false)
            {
                return NoParcelForTargetReason;
            }

            if (interactable is DisposalSite && _loadedParcels.Exists(parcel => parcel.IsSpoiled) == false)
            {
                return NoParcelToDisposeReason;
            }

            if (interactable is LogisticsCenter center)
            {
                // 대기열이 남아 있는데 실을 수 없으면 선두 물류 기준으로 어느 한도인지 알린다
                Parcel head = center.PeekNextParcel();
                if (head != null && head.Cargo.WeightUsage > MaxWeight)
                {
                    // 단일 물류가 W_max를 넘으면 영원히 실을 수 없으므로 설정 오류로 알린다
                    Debug.LogWarning($"Parcel #{head.Id} weight {head.Cargo.WeightUsage} exceeds W_max {MaxWeight}; it can never be loaded");
                }

                return head != null ? GetLoadBlockReason(head) ?? NoParcelToLoadReason : NoParcelToLoadReason;
            }

            return null;
        }

        private bool HasLoadableParcel(LogisticsCenter center)
        {
            IReadOnlyList<Parcel> available = center.AvailableParcels;
            for (int i = 0; i < available.Count; i++)
            {
                if (CanLoad(available[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryBeginNextParcel()
        {
            if (_currentInteractable is LogisticsCenter center)
            {
                if (StopLoadingIfBlocked(center))
                {
                    return false;
                }

                return BeginParcel(FindNextLoadParcel(center));
            }

            if (_currentInteractable is DeliveryTarget target)
            {
                return BeginParcel(FindNextUnloadParcel(target));
            }

            if (_currentInteractable is DisposalSite)
            {
                return BeginParcel(_loadedParcels.Find(parcel => parcel.IsSpoiled));
            }

            return false;
        }

        /// <summary>처리 순서상 다음 물류가 한도를 넘으면 뒤의 물류로 건너뛰지 않고 상차를 끝낸다(기획서 8.5, 결정 D5).</summary>
        private bool StopLoadingIfBlocked(LogisticsCenter center)
        {
            Parcel next = FindNextLoadParcel(center);
            if (next == null || CanContinueGroupLoad(next))
            {
                return false;
            }

            _loadSelection.Clear();
            _hasExplicitSelection = false;
            _isLoadRequested = false;
            // 개별 한도는 아니고 그룹 상차의 W 초과로 멈춘 경우도 WEIGHT LIMIT로 알린다
            InteractionBlocked?.Invoke(GetLoadBlockReason(next) ?? WeightLimitReason);
            return true;
        }

        private bool BeginParcel(Parcel parcel)
        {
            if (parcel == null)
            {
                return false;
            }

            _activeParcel = parcel;
            _progressSeconds = 0f;
            InteractionProgressChanged?.Invoke(0f, true);
            return true;
        }

        /// <summary>선택한 물류 중 아직 대기열에 있는 것을 우선하고, 선택이 없으면 대기열 선두를 쓴다.</summary>
        private Parcel FindNextLoadParcel(LogisticsCenter center)
        {
            for (int i = 0; i < _loadSelection.Count; i++)
            {
                if (center.ContainsParcel(_loadSelection[i]))
                {
                    return _loadSelection[i];
                }
            }

            // 직접 고른 그룹을 다 실었으면 다른 그룹으로 넘어가지 않는다
            return _hasExplicitSelection ? null : center.PeekNextParcel();
        }

        private bool HasRemainingSelection(LogisticsCenter center)
        {
            if (_hasExplicitSelection == false)
            {
                return false;
            }

            for (int i = 0; i < _loadSelection.Count; i++)
            {
                if (center.ContainsParcel(_loadSelection[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>해당 배송지로 보낼 물류 중 처리 순서가 가장 앞선 것(기획서 7.4, 9.3).</summary>
        private Parcel FindNextUnloadParcel(DeliveryTarget target)
        {
            Parcel next = null;
            for (int i = 0; i < _loadedParcels.Count; i++)
            {
                Parcel parcel = _loadedParcels[i];
                if (parcel.IsDeliverableTo(target) == false)
                {
                    continue;
                }

                if (next == null || Parcel.CompareProcessingOrder(parcel, next) < 0)
                {
                    next = parcel;
                }
            }

            return next;
        }

        private void CompleteActiveParcel()
        {
            Parcel parcel = _activeParcel;
            if (parcel == null)
            {
                return;
            }

            if (_currentInteractable is LogisticsCenter center && center.TryTakeParcel(parcel))
            {
                parcel.Status = ParcelStatus.Loaded;
                // 상차 완료 시점부터 줄어드는 유형(Fresh)의 기준 시각
                parcel.LoadCompletionTime = Game.Systems.GameTimeSystem.Instance.CurrentSeconds;
                _loadedParcels.Add(parcel);
                _loadSelection.Remove(parcel);
                _loadedInHold++;
                RecomputeSpeedMultiplier();

                // UI가 이벤트를 받을 때 선택이 이미 정리되어 있도록 알림보다 먼저 판정한다
                if (_hasExplicitSelection)
                {
                    StopLoadingIfBlocked(center);
                }

                parcel.Target.NotifyParcelChanged(parcel);
                LoadedParcelsChanged?.Invoke(parcel);
                Debug.Log($"[상차] 물류 #{parcel.Id} 적재 완료 {FormatUsage()}");
            }
            else if (_currentInteractable is DeliveryTarget target && _loadedParcels.Remove(parcel))
            {
                DeliverParcel(parcel, target);
            }
            else if (_currentInteractable is DisposalSite && _loadedParcels.Remove(parcel))
            {
                DisposeParcel(parcel);
            }

            StopInteraction();
        }

        /// <summary>폐기 물류를 차량에서 내린다. 보상·비용 없이 적재·무게 한도만 회복한다(기획서 11.6).</summary>
        private void DisposeParcel(Parcel parcel)
        {
            // 폐기 전환 때 실패가 이미 확정·집계되었으므로 추적 목록에는 더 알리지 않는다
            parcel.Status = ParcelStatus.Failed;
            RecomputeSpeedMultiplier();
            LoadedParcelsChanged?.Invoke(parcel);
            Debug.Log($"[처분] 폐기 물류 #{parcel.Id} 처분 완료 {FormatUsage()}");
        }

        private void DeliverParcel(Parcel parcel, DeliveryTarget target)
        {
            // 신선도 보상은 하차 완료 시점의 남은 신선도로 확정한다(기획서 14.2)
            parcel.SettleFreshBonus();
            parcel.Status = ParcelStatus.Delivered;
            RecomputeSpeedMultiplier();
            target.NotifyParcelChanged(parcel);
            LoadedParcelsChanged?.Invoke(parcel);

            if (DeliveryDeadlineSystem.Instance != null)
            {
                DeliveryDeadlineSystem.Instance.NotifyDelivered(parcel);
            }

            Debug.Log($"[하차] 물류 #{parcel.Id} 배송 완료 {FormatUsage()}");
        }

        private string FormatUsage()
        {
            return $"(용량 {UsedCapacity}/{CargoCapacity}, 무게 {UsedWeight}/{WeightCapacity})";
        }

        /// <summary>진행 중인 작업과 진행도를 초기화한다. 완료 전에는 대기열·적재 목록을 건드리지 않는다.</summary>
        private void StopInteraction()
        {
            bool hadProgress = _activeParcel != null || _progressSeconds > 0f;

            _activeParcel = null;
            _progressSeconds = 0f;

            if (hadProgress)
            {
                InteractionProgressChanged?.Invoke(0f, false);
            }
        }

        private static void CloseUIOf(IInteractable interactable)
        {
            if (interactable is LogisticsCenter center)
            {
                center.CloseUI();
            }
        }
    }
}
