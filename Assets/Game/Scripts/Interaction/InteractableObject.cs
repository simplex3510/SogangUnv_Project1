using System;

using UnityEngine;

namespace Game.Interaction
{
    public abstract class InteractableObject : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [Tooltip("상호작용 1회에 걸리는 시간(초)")]
        [SerializeField, Min(0f)] protected float _interactionDuration = 1f;
        public float InteractionDuration => _interactionDuration;

        [SerializeField] protected InteractionTypes _interactionType;

        public InteractionTypes InteractionType => _interactionType;

        public abstract bool CanInteract();
        public abstract void Interact();
    }
}