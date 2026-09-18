using FourPlayerHorror.Player;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Interaction
{
    public class Fuse : NetworkBehaviour, IInteractable
    {
        public string GetInteractionText()
        {
            return "Get Fuse";
        }

        public void Interact(ulong clientId)
        {

            if (!IsServer)
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

            PlayerInventory inventory = client.PlayerObject.GetComponent<PlayerInventory>();

            if (inventory == null)
                return;


            inventory.AddFuse();

            Debug.Log( $"Client {clientId} picked up Fuse. " + $"FuseCount: {inventory.FuseCount}");

            NetworkObject.Despawn();
        }
    }
}