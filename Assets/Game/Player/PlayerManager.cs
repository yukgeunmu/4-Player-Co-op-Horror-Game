using System;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Player
{
    public class PlayerManager : NetworkBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        private readonly NetworkVariable<int> aliveCount =
            new(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        private readonly NetworkVariable<int> deadCount =
            new(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        private readonly NetworkVariable<int> escapedCount =
            new(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public int AliveCount => aliveCount.Value;
        public int DeadCount => deadCount.Value;
        public int EscapedCount => escapedCount.Value;

        public event Action<int, int, int> OnPlayerCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            aliveCount.OnValueChanged += OnAliveCountChanged;
            deadCount.OnValueChanged += OnDeadCountChanged;
            escapedCount.OnValueChanged += OnEscapedCountChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback +=
                    HandleClientConnected;

                NetworkManager.Singleton.OnClientDisconnectCallback +=
                    HandleClientDisconnected;

                RegisterPlayerStateEvents();
                RefreshPlayerCounts();
            }
        }

        public override void OnNetworkDespawn()
        {
            aliveCount.OnValueChanged -= OnAliveCountChanged;
            deadCount.OnValueChanged -= OnDeadCountChanged;
            escapedCount.OnValueChanged -= OnEscapedCountChanged;

            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -=
                    HandleClientConnected;

                NetworkManager.Singleton.OnClientDisconnectCallback -=
                    HandleClientDisconnected;
            }

            base.OnNetworkDespawn();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer)
                return;

            RegisterPlayerStateEvents();
            RefreshPlayerCounts();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer)
                return;

            Debug.Log($"Client disconnected: {clientId}");

            CleanupDisconnectedPlayer(clientId);

            RefreshPlayerCounts();
        }

        private void OnAliveCountChanged(int previousValue, int newValue)
        {
            NotifyCountChanged();
        }

        private void OnDeadCountChanged(int previousValue, int newValue)
        {
            NotifyCountChanged();
        }

        private void OnEscapedCountChanged(int previousValue, int newValue)
        {
            NotifyCountChanged();
        }

        private void NotifyCountChanged()
        {
            OnPlayerCountChanged?.Invoke(
                aliveCount.Value,
                deadCount.Value,
                escapedCount.Value
            );
        }

        public void RefreshPlayerCounts()
        {
            if (!IsServer)
                return;

            int alive = 0;
            int dead = 0;
            int escaped = 0;

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                PlayerController player =
                    client.PlayerObject.GetComponent<PlayerController>();

                if (player == null)
                    continue;

                switch (player.CurrentState)
                {
                    case PlayerState.Normal:
                    case PlayerState.Down:
                        alive++;
                        break;

                    case PlayerState.Dead:
                        dead++;
                        break;

                    case PlayerState.Escaped:
                        escaped++;
                        break;
                }
            }

            aliveCount.Value = alive;
            deadCount.Value = dead;
            escapedCount.Value = escaped;
        }

        private void RegisterPlayerStateEvents()
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                PlayerController player =
                    client.PlayerObject.GetComponent<PlayerController>();

                if (player == null)
                    continue;

                player.OnStateChanged -= HandlePlayerStateChanged;
                player.OnStateChanged += HandlePlayerStateChanged;
            }
        }

        private void HandlePlayerStateChanged(PlayerState previousState, PlayerState newState)
        {
            if (!IsServer)
                return;

            RefreshPlayerCounts();

            Debug.Log(
                $"Player Count Updated - " +
                $"Alive: {AliveCount}, " +
                $"Dead: {DeadCount}, " +
                $"Escaped: {EscapedCount}");
        }

        private void CleanupDisconnectedPlayer(ulong disconnectedClientId)
        {
            if (!IsServer)
                return;

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                PlayerController player =
                    client.PlayerObject.GetComponent<PlayerController>();

                if (player == null)
                    continue;

                // 연결이 끊긴 플레이어가 이 플레이어를 구조 중이었다면
                // 구조를 취소한다.
                if (player.IsBeingRevived &&
                    player.ReviveRescuerClientId == disconnectedClientId)
                {
                    player.CancelReviveByRescuer(disconnectedClientId);
                }
            }
        }
    }
}