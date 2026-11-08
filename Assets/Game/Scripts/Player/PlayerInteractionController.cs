using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Game.Building;

namespace Game.Player.Interaction
{
    public class PlayerInteractionController : MonoBehaviour
    {
        [Header("Interact Input")]
        [SerializeField] InputActionReference _interactionInput;

        [Header("Interaction UI")]
        [SerializeField] private PlayerUIController _uiController;

        [Header("Parcels Settings")]
        [SerializeField] private int _loadedParcels = 0;
        [Tooltip("한 번에 적재할 수 있는 택배 수")]
        [SerializeField] private int _maxLoadedParcels = 6;

        // 현재 겹쳐 있는 구역 목록. 진입 순서를 유지하며, 마지막 원소가 상호작용 대상
        private readonly List<BuildingBase> _detectedBuildings = new();
        private BuildingBase _targetBuilding;

        private float _interactionTime;

        private void Awake()
        {
            if (_uiController == null)
            {
                _uiController = GetComponentInChildren<PlayerUIController>(true);
            }
        }

        private void OnEnable()
        {
            _interactionInput.action.Enable();

            if (_targetBuilding != null)
            {
                _targetBuilding.InteractableChanged += RefreshInteractionUI;
            }
        }

        private void OnDisable()
        {
            _interactionInput.action.Disable();

            if (_targetBuilding != null)
            {
                _targetBuilding.InteractableChanged -= RefreshInteractionUI;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            BuildingBase detected = other.GetComponentInParent<BuildingBase>();
            if (detected == null)
            {
                return;
            }

            if (detected.CanInteract() == false)
            {
                return;
            }

            if (_detectedBuildings.Contains(detected))
            {
                return;
            }

            _detectedBuildings.Add(detected);
            SetInterationTarget(_detectedBuildings[_detectedBuildings.Count - 1]);
        }

        private void OnTriggerExit(Collider other)
        {
            BuildingBase exited = other.GetComponentInParent<BuildingBase>();
            if (exited == null)
            {
                return;
            }

            if (_detectedBuildings.Remove(exited) == false)
            {
                return;
            }

            if (_detectedBuildings.Count > 0)
            {
                SetInterationTarget(_detectedBuildings[_detectedBuildings.Count - 1]);
            }
            else
            {
                SetInterationTarget();
            }
        }

        private void SetInterationTarget(BuildingBase building = null)
        {
            if (_targetBuilding == building)
            {
                return;
            }

            if (_targetBuilding != null)
            {
                _targetBuilding.InteractableChanged -= RefreshInteractionUI;
            }

            _targetBuilding = building;

            if (_targetBuilding != null)
            {
                _targetBuilding.InteractableChanged += RefreshInteractionUI;
            }

            ResetInteraction();
            RefreshInteractionUI();
        }

        private void RefreshInteractionUI()
        {
            if (_uiController == null)
            {
                return;
            }

            bool canInteract = _targetBuilding != null && _targetBuilding.CanInteract();
            _uiController.Show(canInteract ? _targetBuilding.LoadType : LoadTypes.None);
        }

        private void Update()
        {
            bool IsInteracting = _interactionInput.action.IsPressed();
            if (IsInteracting == false || _targetBuilding == null)
            {
                ResetInteraction();
                return;
            }

            if (_targetBuilding.CanInteract() == true)
            {
                // 상호작용 키 유지
                _interactionTime += Time.deltaTime;
                if (_interactionTime >= _targetBuilding.InteractionDuration)
                {
                    if (_targetBuilding.LoadType == LoadTypes.Load)
                    {
                        if (TryLoadParcel() == true)
                        {
                            _targetBuilding.Interact();
                        }
                    }
                    else if (_targetBuilding.LoadType == LoadTypes.Unload)
                    {
                        if (TryUnloadParcel() == true)
                        {
                            _targetBuilding.Interact();
                        }
                    }
                    else
                    {
                        Debug.LogError($"Building type is not assigned in {gameObject.name}");
                    }

                    ResetInteraction();
                }
            }
        }

        private void ResetInteraction()
        {
            _interactionTime = 0f;
        }

        public bool TryLoadParcel()
        {
            if (_loadedParcels >= _maxLoadedParcels)
            {
                return false;
            }

            _loadedParcels++;
            return true;
        }

        public bool TryUnloadParcel()
        {
            if (_loadedParcels <= 0)
            {
                return false;
            }

            _loadedParcels--;
            return true;
        }
    }
}