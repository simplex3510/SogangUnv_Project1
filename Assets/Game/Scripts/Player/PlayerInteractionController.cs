using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Interaction;
using Game.Logistics;

namespace Game.Player.Interaction
{
    /// <summary>물류 하나 단위로 상차·하차를 진행한다. 상호작용 키를 유지하는 동안 현재 물류의 진행도가 쌓이고, 완료되면 다음 물류로 넘어간다.</summary>
    public class PlayerInteractionController : MonoBehaviour, IInteractor
    {
        [Header("Interact Input")]
        [SerializeField] private InputActionReference _interactionInput;

        [Header("Parcels Settings")]
        [Tooltip("한 번에 적재할 수 있는 택배 수")]
        [SerializeField, Min(1)] private int _maxLoadedParcels = 6;

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

        public event Action<Parcel> LoadedParcelsChanged;

        /// <summary>상호작용이 막힌 이유. 안내 문구 표시에 사용한다.</summary>
        public event Action<string> InteractionBlocked;

        public IReadOnlyList<Parcel> LoadedParcels => _loadedParcels;
        public int MaxLoadedParcels => _maxLoadedParcels;

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

        private void OnEnable()
        {
            _interactionInput.action.Enable();
        }

        private void OnDisable()
        {
            _interactionInput.action.Disable();
        }

        private void OnTriggerEnter(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null || _detectedInteractables.Contains(interactable))
            {
                return;
            }

            _detectedInteractables.Add(interactable);
        }

        private void OnTriggerExit(Collider other)
        {
            IInteractable interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return;
            }

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

            if (_currentInteractable is LogisticsCenter center
                && center.IsUIOpen
                && HasRemainingSelection(center)
                && _loadedParcels.Count < _maxLoadedParcels)
            {
                TickInteraction(Time.deltaTime);
                return;
            }

            _isLoadRequested = false;
        }

        /// <summary>상호작용 키를 유지하는 동안 호출되어 진행도를 쌓는다. 검증용으로도 호출할 수 있다.</summary>
        public void TickInteraction(float deltaSeconds)
        {
            if (_currentInteractable == null || CanInteract(_currentInteractable) == false)
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

        /// <summary>물류센터 UI가 고른 상차 대상을 넘긴다. 빈 목록이면 대기열 선두를 사용한다.</summary>
        public void SetLoadSelection(IReadOnlyList<Parcel> parcels)
        {
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
                return _loadedParcels.Count < _maxLoadedParcels;
            }

            if (interactable.InteractionType == InteractionTypes.Unload)
            {
                // 해당 배송지로 배송할 물류를 실고 있을 때만 하차를 허용한다
                return interactable is DeliveryTarget target
                    && _loadedParcels.Exists(parcel => parcel.Target == target);
            }

            return false;
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
            if (_currentInteractable == null)
            {
                return;
            }

            if (_currentInteractable is LogisticsCenter center)
            {
                if (CanInteract(center) == false)
                {
                    InteractionBlocked?.Invoke("NO PARCELS TO LOAD");
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
                && _loadedParcels.Exists(parcel => parcel.Target == target) == false)
            {
                return "NO PARCEL FOR THIS TARGET";
            }

            if (interactable.InteractionType == InteractionTypes.Load)
            {
                return _loadedParcels.Count >= _maxLoadedParcels ? "CARGO FULL" : "NO PARCELS TO LOAD";
            }

            return null;
        }

        private bool TryBeginNextParcel()
        {
            if (_currentInteractable is LogisticsCenter center)
            {
                if (_loadedParcels.Count >= _maxLoadedParcels)
                {
                    return false;
                }

                Parcel next = FindNextLoadParcel(center);
                return BeginParcel(next);
            }

            if (_currentInteractable is DeliveryTarget target)
            {
                return BeginParcel(FindNextUnloadParcel(target));
            }

            return false;
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

        /// <summary>해당 배송지로 보낼 물류 중 남은 제한 시간이 가장 짧은 것(기획서 9.3).</summary>
        private Parcel FindNextUnloadParcel(DeliveryTarget target)
        {
            Parcel next = null;
            for (int i = 0; i < _loadedParcels.Count; i++)
            {
                Parcel parcel = _loadedParcels[i];
                if (parcel.Target != target)
                {
                    continue;
                }

                if (next == null || parcel.RemainingDeadline < next.RemainingDeadline)
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
                _loadedParcels.Add(parcel);
                _loadSelection.Remove(parcel);
                parcel.Target.NotifyParcelChanged(parcel);
                LoadedParcelsChanged?.Invoke(parcel);
                Debug.Log($"[상차] 물류 #{parcel.Id} 적재 완료 ({_loadedParcels.Count}/{_maxLoadedParcels})");

                // 적재 한도에 도달하면 남은 선택을 버리고 상차를 종료한다(기획서 8.5)
                if (_loadedParcels.Count >= _maxLoadedParcels)
                {
                    _loadSelection.Clear();
                    _hasExplicitSelection = false;
                }
            }
            else if (_currentInteractable is DeliveryTarget target && _loadedParcels.Remove(parcel))
            {
                DeliverParcel(parcel, target);
            }

            StopInteraction();
        }

        private void DeliverParcel(Parcel parcel, DeliveryTarget target)
        {
            parcel.Status = ParcelStatus.Delivered;
            target.NotifyParcelChanged(parcel);
            LoadedParcelsChanged?.Invoke(parcel);

            if (DeliveryDeadlineSystem.Instance != null)
            {
                DeliveryDeadlineSystem.Instance.NotifyDelivered(parcel);
            }

            Debug.Log($"[하차] 물류 #{parcel.Id} 배송 완료 ({_loadedParcels.Count}/{_maxLoadedParcels})");
        }

        /// <summary>작업 중인 물류를 취소한다. 완료 전에는 대기열·적재 목록을 건드리지 않으므로 진행도만 초기화하면 된다.</summary>
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
