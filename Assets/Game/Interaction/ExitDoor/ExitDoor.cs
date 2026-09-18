using FourPlayerHorror.Core;
using FourPlayerHorror.Player;
using System;
using Unity.Netcode;
using UnityEngine;

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

        public event Action OnActivated;

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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            isActivated.OnValueChanged +=
                OnActivatedValueChanged;

            if (isActivated.Value)
            {
                OnActivatedValueChanged(false, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            isActivated.OnValueChanged -=
                OnActivatedValueChanged;

            base.OnNetworkDespawn();
        }

        private void OnActivatedValueChanged(bool previousValue, bool newValue)
        {
            if (!previousValue && newValue)
            {
                OnActivated?.Invoke();
            }
        }

        public string GetInteractionText()
        {
            if (!IsActivated)
                return "Close ExitDoor";

            return "Escape";
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

            if (NetworkManager.Singleton.ConnectedClients
                .TryGetValue(clientId, out NetworkClient client) == false)
            {
                Debug.LogWarning(
                    $"Escape failed. Client {clientId} not found."
                );

                return;
            }

            if (client.PlayerObject == null)
            {
                Debug.LogWarning(
                    $"Escape failed. PlayerObject for Client {clientId} not found."
                );

                return;
            }

            PlayerController player =
                client.PlayerObject.GetComponent<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning(
                    $"Escape failed. PlayerController for Client {clientId} not found."
                );

                return;
            }

            // Normal 상태의 플레이어만 탈출 가능
            if (player.CurrentState != PlayerState.Normal)
            {
                Debug.Log(
                    $"Client {clientId} cannot escape. " +
                    $"Current State: {player.CurrentState}"
                );

                return;
            }

            // 서버에서 탈출 상태 변경
            player.SetState(PlayerState.Escaped);

            Debug.Log(
                $"Client {clientId} escaped successfully."
            );

            NotifyEscapeClientRpc(clientId);
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