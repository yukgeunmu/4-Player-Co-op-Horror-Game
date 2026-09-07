using Unity.Netcode;
using UnityEngine;
using FourPlayerHorror.Core;

namespace FourPlayerHorror.Interaction
{
    public class ExitDoor : NetworkBehaviour, IInteractable
    {
        private NetworkVariable<bool> isActivated =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public bool IsActivated => isActivated.Value;

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

        public string GetInteractionText()
        {
            if (!IsActivated)
                return "출구가 잠겨있다.";

            return "탈출하기";
        }

        public void Interact(ulong clientId)
        {
            if (!IsServer)
                return;

            if (!IsActivated)
            {
                Debug.Log(
                    $"Client {clientId} tried to use inactive ExitDoor."
                );

                return;
            }

            Debug.Log(
                $"Client {clientId} escaped."
            );

            NotifyEscapeClientRpc(clientId);

            // 다음 단계에서 실제 탈출 처리를 구현한다.
        }

        [ClientRpc]
        private void NotifyEscapeClientRpc(ulong clientId)
        {
            Debug.Log($"Client {clientId} escaped.");
        }


        private void HandleGeneratorCompleted()
        {
            if (!IsServer)
                return;

            if (isActivated.Value)
                return;

            isActivated.Value = true;

            Debug.Log("ExitDoor Activated!");
        }
    }
}