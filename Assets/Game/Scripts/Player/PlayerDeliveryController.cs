using UnityEngine;
using UnityEngine.InputSystem;

using Game.Interaction;
using Game.Building;

namespace Game.Player.Delivery
{
    public class PlayerDeliveryController : MonoBehaviour
    {
        [Header("Interact Input")]
        [SerializeField] InputActionReference _interactionInput;

        [Header("Parcels Settings")]
        [SerializeField] private int _loadedParcels = 0;
        [Tooltip("한 번에 적재할 수 있는 택배 수")]
        [SerializeField] private int _maxLoadedParcels = 6;

        private BuildingBase _targetBuilding;

        private float _interactionTime;

        private void OnEnable() => _interactionInput.action.Enable();
        private void OnDisable() => _interactionInput.action.Disable();

        private void OnTriggerEnter(Collider other)
        {
            BuildingBase detected = other.GetComponentInParent<BuildingBase>();
            if (detected != null && detected.CanInteract())
            {
                _targetBuilding = detected;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _targetBuilding = null;
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