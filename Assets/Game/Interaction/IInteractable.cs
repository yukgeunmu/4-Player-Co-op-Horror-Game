namespace FourPlayerHorror.Interaction
{
    public interface IInteractable
    {
        string GetInteractionText();

        void Interact(ulong clientId);
    }
}