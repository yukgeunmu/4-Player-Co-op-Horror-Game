using System;
using Unity.Netcode;
using UnityEngine;
using FourPlayerHorror.Player;
using FourPlayerHorror.Core;

namespace FourPlayerHorror.Interaction
{
    public class Generator : NetworkBehaviour, IInteractable
    {
        private NetworkVariable<int> installedFuseCount =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public int InstalledFuseCount => installedFuseCount.Value;

        public bool IsCompleted =>
            installedFuseCount.Value >= InteractionConst.RequiredFuseCount;

        public event Action<int> OnFuseCountChanged;

        public string GetInteractionText()
        {
            if (IsCompleted)
                return "Complete Generator";

            return $"Equip Fuse ({InstalledFuseCount}/{InteractionConst.RequiredFuseCount})";
        }

        public void Interact(ulong clientId)
        {
            if (!IsServer)
                return;

            if (IsCompleted)
                return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    clientId,
                    out NetworkClient client))
            {
                return;
            }

            if (client.PlayerObject == null)
            {
                return;
            }

            PlayerInventory inventory =
                client.PlayerObject.GetComponent<Player.PlayerInventory>();

            if (inventory == null)
            {
                return;
            }

            if (!inventory.HasFuse())
            {
                Debug.Log(
                    $"Client {clientId} has no Fuse."
                );

                return;
            }

            if (!inventory.RemoveFuse())
            {
                return;
            }

            installedFuseCount.Value++;

            Debug.Log(
                $"Client {clientId} installed Fuse. " +
                $"Generator: {InstalledFuseCount}/{InteractionConst.RequiredFuseCount}"
            );
        }

        public override void OnNetworkSpawn()
        {
            installedFuseCount.OnValueChanged += OnInstalledFuseCountChanged;
        }

        public override void OnNetworkDespawn()
        {
            installedFuseCount.OnValueChanged -= OnInstalledFuseCountChanged;
        }

        private void OnInstalledFuseCountChanged(int previousValue,int newValue)
        {
            OnFuseCountChanged?.Invoke(newValue);

            if (newValue < InteractionConst.RequiredFuseCount)
                return;

            GeneratorCompletedEvent.Publish();
        }
    }
}