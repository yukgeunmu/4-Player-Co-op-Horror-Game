using UnityEngine;

namespace FourPlayerHorror.Core
{
    public class GeneratorCompleteTest : MonoBehaviour
    {
        private void OnEnable()
        {
            GeneratorCompletedEvent.OnCompleted +=
                HandleGeneratorCompleted;
        }

        private void OnDisable()
        {
            GeneratorCompletedEvent.OnCompleted -=
                HandleGeneratorCompleted;
        }

        private void HandleGeneratorCompleted()
        {
            Debug.Log(
                $"[{name}] Generator Completed Event Received"
            );
        }
    }
}