using System;

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

        /// <summary>CanInteract() 결과가 바뀔 수 있는 상태가 변경될 때 발생.</summary>
        public event Action InteractableChanged;

        // CanInteract()의 근거 상태를 바꾼 뒤 반드시 호출할 것
        protected void NotifyInteractableChanged() => InteractableChanged?.Invoke();

        public abstract bool CanInteract();
        public abstract void Interact();
    }
}