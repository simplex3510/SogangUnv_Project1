using UnityEngine;

using Game.Interaction;

namespace Game.Building
{
    public enum LoadTypes : int
    {
        None = 0,
        Load,
        Unload,
    }

    public abstract class BuildingBase : MonoBehaviour, IInteractable
    {
        [Tooltip("상호작용 1회에 걸리는 시간(초)")]
        [SerializeField, Min(0f)] protected float _interactionDuration = 1f;
        public float InteractionDuration => _interactionDuration;

        public LoadTypes LoadType { get; protected set; } = LoadTypes.None;

        public abstract bool CanInteract();
        public abstract void Interact();
    }
}