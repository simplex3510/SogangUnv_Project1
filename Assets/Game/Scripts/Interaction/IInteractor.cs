namespace Game.Interaction
{
    public interface IInteractor
    {
        public bool CanInteract(IInteractable interactable);

        public void Interact(IInteractable interactable);
    }
} 