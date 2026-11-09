using System;

namespace Game.Interaction
{
    public interface IInteractable
    {
        public InteractionTypes InteractionType { get; }

        public float InteractionDuration { get; }

        public bool CanInteract();

        public void Interact();
    }
}