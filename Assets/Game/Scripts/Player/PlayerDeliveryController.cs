using System.Collections.Generic;
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

        // 겹친 구역 목록. 마지막 원소가 현재 상호작용 대상
        private readonly Queue<BuildingBase> _detectedBuildings = new();
        private BuildingBase _targetBuilding;

        private float _interactionTime;

        private void OnEnable() => _interactionInput.action.Enable();
        private void OnDisable() => _interactionInput.action.Disable();

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

            if (_detectedBuildings.Contains(detected) == false)
            {
                _detectedBuildings.Enqueue(detected);
                SetInterationTarget(_detectedBuildings.Peek());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            BuildingBase exited = other.GetComponentInParent<BuildingBase>();
            if (exited == null)
            {
                return;
            }

            if (_detectedBuildings.Contains(exited) == false)
            {
                return;
            }
            else
            {
                _detectedBuildings.Dequeue();
                if (_detectedBuildings.Count > 0)
                {
                    SetInterationTarget(_detectedBuildings.Peek());
                }
                else
                {
                    SetInterationTarget();
                }
            }
        }

        private void SetInterationTarget(BuildingBase building = null)
        {
            if (_targetBuilding == building)
            {
                return;
            }

            _targetBuilding = building;
            ResetInteraction();
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