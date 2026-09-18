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

        [SerializeField]
        private PlayerController playerController;

        private IInteractable currentInteractable;

        public event System.Action<string> OnInteractionPromptChanged;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            if (playerController.CurrentState != PlayerState.Normal)
            {
                ClearInteractable();
                return;
            }

            DetectInteractable();

            if (currentInteractable != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryInteract();
            }
        }

        private void DetectInteractable()
        {
            IInteractable detectedInteractable = null;

            Ray ray = new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    InteractionConst.InteractionDistance))
            {
                detectedInteractable =
                    hit.collider.GetComponent<IInteractable>();
            }

            UpdateCurrentInteractable(detectedInteractable);
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


            PlayerController requester = playerObject.GetComponent<PlayerController>();

            if (requester == null)
            {
                return;
            }

            // 서버 측 상태 검증
            if (requester.CurrentState != PlayerState.Normal)
            {
                Debug.LogWarning(
                    $"Client {clientId} interaction rejected. " +
                    $"Player state: {requester.CurrentState}"
                );

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

        private void UpdateCurrentInteractable(IInteractable newInteractable)
        {
            // 같은 대상을 계속 바라보고 있다면
            // UI를 다시 갱신하지 않는다.
            if (currentInteractable == newInteractable)
                return;
            


            currentInteractable = newInteractable;

            // 아무 대상도 바라보지 않는 경우
            if (currentInteractable == null)
            {
                OnInteractionPromptChanged?.Invoke(null);
                return;
            }

            // 새로운 상호작용 대상 발견
            OnInteractionPromptChanged?.Invoke(
                currentInteractable.GetInteractionText()
            );
        }

        private void ClearInteractable()
        {
            if (currentInteractable == null)
                return;

            currentInteractable = null;

            OnInteractionPromptChanged?.Invoke(null);
        }

    }
}