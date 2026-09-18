using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Player
{
    public class PlayerSpectator : NetworkBehaviour
    {
        [SerializeField]
        private Camera spectatorCamera;

        private PlayerController playerController;

        private PlayerController spectatingPlayer;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                playerController.OnStateChanged += HandlePlayerStateChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && playerController != null)
            {
                playerController.OnStateChanged -= HandlePlayerStateChanged;
            }

            base.OnNetworkDespawn();
        }

        private void HandlePlayerStateChanged(
            PlayerState previousState,
            PlayerState newState)
        {
            if (!IsOwner)
                return;

            if (newState == PlayerState.Dead || newState == PlayerState.Escaped)
            {
                EnterSpectator();
            }
        }

        private void EnterSpectator()
        {
            if (spectatorCamera == null)
            {
                Debug.LogWarning(
                    $"Player {OwnerClientId}: Spectator Camera is not assigned.");
                return;
            }

            spectatingPlayer = FindLivingPlayer();

            if (spectatingPlayer == null)
            {
                Debug.LogWarning(
                    $"Player {OwnerClientId}: No living player found.");
                return;
            }

            Debug.Log(
                $"Player {OwnerClientId} entered Spectator. " +
                $"Following Player {spectatingPlayer.OwnerClientId}");
        }

        private PlayerController FindLivingPlayer()
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                PlayerController player =
                    client.PlayerObject.GetComponent<PlayerController>();

                if (player == null)
                    continue;

                // 자기 자신 제외
                if (player == playerController)
                    continue;

                // Normal / Down은 관전 가능 대상
                if (player.CurrentState == PlayerState.Normal ||
                    player.CurrentState == PlayerState.Down)
                {
                    return player;
                }
            }

            return null;
        }

        private void LateUpdate()
        {
            if (!IsOwner)
                return;

            if (playerController.CurrentState == PlayerState.Normal ||
                playerController.CurrentState == PlayerState.Down)
                return;

            if (spectatorCamera == null)
                return;

            if (spectatingPlayer == null)
            {
                spectatingPlayer = FindLivingPlayer();

                if (spectatingPlayer == null)
                    return;
            }

            UpdateSpectatorCamera();
        }

        private void UpdateSpectatorCamera()
        {
            Transform targetCameraRoot = spectatingPlayer.CameraRoot;

            if (targetCameraRoot == null)
            {
                spectatorCamera.transform.position =
                    spectatingPlayer.transform.position;

                spectatorCamera.transform.rotation =
                    spectatingPlayer.transform.rotation;

                return;
            }

            spectatorCamera.transform.position =
                targetCameraRoot.position;

            spectatorCamera.transform.rotation =
                targetCameraRoot.rotation;
        }
    }
}