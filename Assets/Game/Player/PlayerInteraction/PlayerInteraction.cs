using FourPlayerHorror.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FourPlayerHorror.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [SerializeField]
        private Camera playerCamera;

        private IInteractable currentInteractable;

        private void Update()
        {
            if (!IsOwner)
                return;

            DetectInteractable();

            if (currentInteractable != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryInteract();
            }
        }

        private void DetectInteractable()
        {
            currentInteractable = null;

            Ray ray = new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    InteractionConst.InteractionDistance))
            {
                return;
            }

            currentInteractable =
                hit.collider.GetComponent<IInteractable>();
        }

        private void TryInteract()
        {
            if (currentInteractable == null)
                return;

            NetworkObject networkObject =
                ((Component)currentInteractable)
                .GetComponent<NetworkObject>();

            if (networkObject == null)
                return;

            RequestInteractServerRpc(
                networkObject.NetworkObjectId
            );
        }

        [ServerRpc]
        private void RequestInteractServerRpc(
            ulong networkObjectId,
            ServerRpcParams rpcParams = default)
        {

            ulong clientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId,
                    out NetworkClient client))
            {
                return;
            }

            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null)
            {
                return;
            }


            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    networkObjectId,
                    out NetworkObject interactableObject))
            {
                return;
            }

            IInteractable interactable =
                interactableObject.GetComponent<IInteractable>();

            if (interactable == null)
                return;


            float distance = Vector3.Distance( playerObject.transform.position,
                                               interactableObject.transform.position);


            if (distance > InteractionConst.InteractionDistance)
            {
                Debug.LogWarning(
                    $"Client {clientId} interaction rejected. " +
                    $"Distance: {distance:F2}"
                );

                return;
            }

            interactable.Interact(clientId);
        }
    }
}