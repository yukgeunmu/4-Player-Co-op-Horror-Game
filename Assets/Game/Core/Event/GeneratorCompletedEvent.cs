namespace FourPlayerHorror.Core
{
    public static class GeneratorCompletedEvent
    {
        public static event System.Action OnCompleted;

        public static void Publish()
        {
            OnCompleted?.Invoke();
        }
    }
}