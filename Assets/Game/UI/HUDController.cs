using FourPlayerHorror.Interaction;
using FourPlayerHorror.Player;
using FourPlayerHorror.UI.Interaction;
using FourPlayerHorror.UI.PlayerStatus;
using FourPlayerHorror.UI.Spectator;
using FourPlayerHorror.UI.GameResultUI;
using FourPlayerHorror.GameFlow;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.UI.HUD
{
    public class HUDController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField]
        private GameObject hudRoot;

        [Header("Initialize")]
        [SerializeField]
        private float initializeRetryInterval = 0.2f;

        private PlayerController localPlayer;

        private Coroutine initializeCoroutine;


        [Header("Generator State")]
        [SerializeField]
        private ObjectiveProgressController objectiveProgressController;

        private Generator generator;

        [Header("Interaction")]
        [SerializeField]
        private InteractionPromptController interactionPromptController;

        private PlayerInteraction playerInteraction;

        [Header("Player Status")]
        [SerializeField]
        private PlayerStatusController playerStatusController;

        [Header("Revive")]
        [SerializeField]
        private ReviveProgressController reviveProgressController;

        [Header("Spectator")]
        [SerializeField]
        private SpectatorUIController spectatorUIController;

        [Header("Game Result")]
        [SerializeField]
        private GameResultUIController gameResultUIController;

        private GameManager gameManager;

        private void Awake()
        {
            if (hudRoot == null)
            {
                hudRoot = gameObject;
            }
        }

        private void Start()
        {
            initializeCoroutine =
                StartCoroutine(InitializeRoutine());
        }

        private void Update()
        {
            if (localPlayer == null)
                return;

            UpdateDownUI();
            UpdateReviveUI();
        }

        private void UpdateDownUI()
        {
            if (playerStatusController == null)
                return;

            if (localPlayer.CurrentState != PlayerState.Down)
                return;

            float remainingTime =
                localPlayer.GetRemainingDownTime();

            playerStatusController.SetDownTimer(
                remainingTime
            );
        }

        private void UpdateReviveUI()
        {
            if (reviveProgressController == null)
                return;

            if (!localPlayer.IsReviving)
            {
                reviveProgressController.Hide();
                return;
            }


            reviveProgressController.Show();


            float progress = localPlayer.GetReviveTargetProgress();

            reviveProgressController.SetProgress(
                progress
            );
        }

        private void OnDestroy()
        {
            if (initializeCoroutine != null)
            {
                StopCoroutine(initializeCoroutine);
            }

            UnsubscribeFromPlayer();
            UnsubscribeFromGenerator();
            UnsubscribeFromGameManager();

        }

        private IEnumerator InitializeRoutine()
        {
            bool playerBound = false;
            bool generatorBound = false;
            bool gameManagerBound = false;

            while (!playerBound || !generatorBound || !gameManagerBound)
            {
                if (!playerBound)
                {
                    playerBound = TryBindLocalPlayer();
                }

                if (!generatorBound)
                {
                    generatorBound = TryBindGenerator();
                }

                if (!gameManagerBound)
                    gameManagerBound = TryBindGameManager();

                yield return new WaitForSeconds(
                    initializeRetryInterval
                );
            }
        }

        private bool TryBindLocalPlayer()
        {
            if (NetworkManager.Singleton == null)
            {
                return false;
            }

            if (!NetworkManager.Singleton.IsClient)
            {
                HideHUD();
                return false;
            }

            if (NetworkManager.Singleton.LocalClient == null)
            {
                return false;
            }

            NetworkObject playerObject =
                NetworkManager.Singleton.LocalClient.PlayerObject;

            if (playerObject == null)
            {
                return false;
            }

            PlayerController player =
                playerObject.GetComponent<PlayerController>();

            if (player == null)
            {
                return false;
            }

            BindLocalPlayer(player);

            return true;
        }

        private bool TryBindGenerator()
        {
            Generator target =
                FindFirstObjectByType<Generator>();

            if (target == null)
                return false;

            if (generator == target)
                return true;

            UnsubscribeFromGenerator();

            generator = target;

            generator.OnFuseCountChanged +=
                HandleFuseCountChanged;

            HandleFuseCountChanged(
                generator.InstalledFuseCount
            );

            return true;
        }



        private void UnsubscribeFromGenerator()
        {
            if (generator == null)
                return;

            generator.OnFuseCountChanged -=
                HandleFuseCountChanged;

            generator = null;
        }

        private bool TryBindGameManager()
        {
            GameManager target =
                FindFirstObjectByType<GameManager>();

            if (target == null)
                return false;

            if (gameManager == target)
                return true;

            UnsubscribeFromGameManager();

            gameManager = target;

            gameManager.OnGameResultChanged +=
                HandleGameResultChanged;

            HandleGameResultChanged(
                gameManager.CurrentGameResult
            );

            Debug.Log(
                $"HUDController: GameManager 연결 완료."
            );

            return true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (gameManager == null)
                return;

            gameManager.OnGameResultChanged -=
                HandleGameResultChanged;

            gameManager = null;
        }

        private void HandleFuseCountChanged(int count)
        {
            if (objectiveProgressController == null)
                return;

            objectiveProgressController.SetProgress(count);
        }

        private void BindLocalPlayer(
            PlayerController player)
        {
            if (localPlayer == player)
                return;

            UnsubscribeFromPlayer();

            localPlayer = player;

            localPlayer.OnStateChanged +=
                HandlePlayerStateChanged;

            playerInteraction = localPlayer.GetComponent<PlayerInteraction>();

            if (playerInteraction != null)
            {
                playerInteraction.OnInteractionPromptChanged +=
                    HandleInteractionPromptChanged;

                playerStatusController.SetPlayerState(localPlayer.CurrentState);
            }


            HandlePlayerStateChanged(
                localPlayer.CurrentState,
                localPlayer.CurrentState
            );

            ShowHUD();

        }

        private void UnsubscribeFromPlayer()
        {
            if (localPlayer != null)
            {
                localPlayer.OnStateChanged -=
                    HandlePlayerStateChanged;
            }

            if (playerInteraction != null)
            {
                playerInteraction.OnInteractionPromptChanged -=
                    HandleInteractionPromptChanged;

                playerInteraction = null;
            }

            localPlayer = null;
        }

        private void HandlePlayerStateChanged(
            PlayerState previousState,
            PlayerState newState)
        {
            Debug.Log(
                $"HUDController: Player State " +
                $"{previousState} -> {newState}"
            );

            if (playerStatusController == null)
                return;

            playerStatusController.SetPlayerState(newState);

            if (spectatorUIController == null)
                return;

            if (newState == PlayerState.Dead)
            {
                spectatorUIController.Show();
            }
            else
            {
                spectatorUIController.Hide();
            }
        }

        private void HandleInteractionPromptChanged(string interactionText)
        {
            if (interactionPromptController == null)
                return;

            if (string.IsNullOrEmpty(interactionText))
            {
                interactionPromptController.Hide();
                return;
            }

            interactionPromptController.Show(
                interactionText
            );
        }

        private void ShowHUD()
        {
            if (hudRoot == null)
                return;

            hudRoot.SetActive(true);
        }

        private void HideHUD()
        {
            if (hudRoot == null)
                return;

            hudRoot.SetActive(false);
        }

        private void SetPanelActive( GameObject target, bool active)
        {
            if (target == null)
                return;

            target.SetActive(active);
        }

        private void HandleGameResultChanged(GameResult result)
        {
            if (gameResultUIController == null)
                return;

            spectatorUIController.Hide();

            switch (result)
            {
                case GameResult.Victory:
                    gameResultUIController.ShowVictory();
                    break;

                case GameResult.Defeat:
                    gameResultUIController.ShowDefeat();
                    break;

                case GameResult.None:
                default:
                    gameResultUIController.Hide();
                    break;
            }
        }
    }
}