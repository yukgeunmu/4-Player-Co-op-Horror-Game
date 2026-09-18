using System;
using Unity.Netcode;
using UnityEngine;
using FourPlayerHorror.Player;

namespace FourPlayerHorror.GameFlow
{
    public class GameManager : NetworkBehaviour
    {
        private readonly NetworkVariable<GameResult> gameResult =
            new(
                GameResult.None,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public GameResult CurrentGameResult =>
            gameResult.Value;

        private bool gameEnded;

        public event Action<GameResult> OnGameResultChanged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            gameResult.OnValueChanged += HandleGameResultChanged;

            if (IsServer)
            {
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.OnPlayerCountChanged +=
                        HandlePlayerCountChanged;
                }
            }

            // 이미 결과가 결정된 상태로 늦게 들어온 클라이언트 대응
            if (gameResult.Value != GameResult.None)
            {
                HandleGameResultChanged(
                    GameResult.None,
                    gameResult.Value
                );
            }
        }

        public override void OnNetworkDespawn()
        {
            gameResult.OnValueChanged -= HandleGameResultChanged;

            if (IsServer && PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnPlayerCountChanged -=
                    HandlePlayerCountChanged;
            }

            base.OnNetworkDespawn();
        }

        private void HandlePlayerCountChanged(
            int aliveCount,
            int deadCount,
            int escapedCount)
        {
            if (!IsServer)
                return;

            if (gameEnded)
                return;

            if (aliveCount > 0)
                return;

            if (escapedCount > 0)
            {
                EndGame(GameResult.Victory);
            }
            else
            {
                EndGame(GameResult.Defeat);
            }
        }

        private void EndGame(GameResult result)
        {
            if (!IsServer)
                return;

            if (gameEnded)
                return;

            gameEnded = true;
            gameResult.Value = result;

            Debug.Log(
                $"Game Ended. Result: {result}"
            );
        }

        private void HandleGameResultChanged(GameResult previousResult, GameResult newResult)
        {
            Debug.Log(
                $"Game Result Changed: " +
                $"{previousResult} -> {newResult}"
            );

            OnGameResultChanged?.Invoke(newResult);
        }
    }
}