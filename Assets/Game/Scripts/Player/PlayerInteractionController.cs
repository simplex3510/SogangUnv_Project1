using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Interaction;
using System;
using TMPro;

namespace Game.Player.Interaction
{
    public class PlayerInteractionController : MonoBehaviour, IInteractor
    {
        [Header("Interact Input")]
        [SerializeField] InputActionReference _interactionInput;

        [Header("Parcels Settings")]
        [SerializeField] private int _loadedParcels = 0;
        [Tooltip("한 번에 적재할 수 있는 택배 수")]
        [SerializeField] private int _maxLoadedParcels = 6;

        public event Action<bool, InteractionTypes> InteractionContextChanged;

        // 현재 겹쳐 있는 구역 목록. 진입 순서를 유지하며, 마지막 원소가 상호작용 대상
        private readonly List<IInteractable> _detectedInteractables = new();
        private IInteractable _currentInteractable;
        private float _interactionTime = 0f;

        // private void Awake()
        // {
        // }

        // private void Start()
        // {
        // }

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
            if (interactable == null)
            {
                return;
            }

            bool canInteract = CanInteract(interactable);
            UpdateInteractables(interactable, canInteract);
        }

        private void OnTriggerExit(Collider other)
        {
            InteractableObject interactable = other.GetComponentInParent<InteractableObject>();
            if (interactable == null)
            {
                return;
            }

            UpdateInteractables(interactable, false);
            CanInteract(_currentInteractable);
        }

        private void Update()
        {
            bool isPressed = _interactionInput.action.IsPressed();
            if (isPressed == false || _currentInteractable == null)
            {
                ResetInteractionTime();
                return;
            }

            if (CanInteract(_currentInteractable) == false)
            {
                ResetInteractionTime();
                return;
            }

            // 상호작용 키 유지
            _interactionTime += Time.deltaTime;
            if (_interactionTime >= _currentInteractable.InteractionDuration)
            {
                Interact(_currentInteractable);
            }
        }

        public bool CanInteract(IInteractable interactable)
        {
            bool canInteract = false;

            if (interactable == null)
            {
                InteractionContextChanged?.Invoke(false, InteractionTypes.None);
                return false;
            }

            if (interactable.CanInteract() == false)
            {
                InteractionContextChanged?.Invoke(false, InteractionTypes.None);
                return false;
            }

            var type = interactable.InteractionType;
            if (type == InteractionTypes.Load)
            {
                canInteract = _loadedParcels < _maxLoadedParcels;
            }
            else if (type == InteractionTypes.Unload)
            {
                canInteract = 0 < _loadedParcels;
            }

            InteractionContextChanged?.Invoke(canInteract, type);
            return canInteract;
        }

        public void Interact(IInteractable interactable)
        {
            string debugLog = string.Empty;

            interactable.Interact();

            var type = interactable.InteractionType;
            if (type == InteractionTypes.Load)
            {
                debugLog = $"택배 상차 완료: ";
                _loadedParcels++;
            }
            else if (type == InteractionTypes.Unload)
            {
                debugLog = $"택배 하차 완료: ";
                _loadedParcels--;
            }

            ResetInteractionTime();

            debugLog += $"{_loadedParcels}";

            Debug.Log(debugLog);
        }

        private void ResetInteractionTime()
        {
            _interactionTime = 0f;
        }

        private void UpdateInteractables(IInteractable interactable, bool canInteract)
        {
            if (canInteract == false)
            {
                _detectedInteractables.Remove(interactable);
                if (_detectedInteractables.Count <= 0)
                {
                    _currentInteractable = null;
                    return;
                }
            }
            else
            {
                if (_detectedInteractables.Contains(interactable) == false)
                {
                    _detectedInteractables.Add(interactable);
                }
            }            
            
            int lastIndex = _detectedInteractables.Count - 1;
            _currentInteractable =_detectedInteractables[lastIndex];
        }
    }
}
