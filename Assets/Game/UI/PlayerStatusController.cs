using TMPro;
using UnityEngine;

namespace FourPlayerHorror.UI.PlayerStatus
{
    public class PlayerStatusController : MonoBehaviour
    {
        [Header("Player State")]
        [SerializeField]
        private TMP_Text playerStateText;

        [Header("Down UI")]
        [SerializeField]
        private GameObject downPanel;

        [SerializeField]
        private TMP_Text downTimerText;

        private void Awake()
        {
            HideDownUI();
            HidePlayerState();
        }

        public void SetPlayerState(global::PlayerState state)
        {
            switch (state)
            {
                case global::PlayerState.Normal:
                    HidePlayerState();
                    HideDownUI();
                    break;

                case global::PlayerState.Down:
                    HidePlayerState();
                    ShowDownUI();
                    break;

                case global::PlayerState.Dead:
                    ShowPlayerState("YOU DIED");
                    HideDownUI();
                    break;

                case global::PlayerState.Escaped:
                    ShowPlayerState("ESCAPED");
                    HideDownUI();
                    break;
            }
        }

        public void SetDownTimer(float remainingTime)
        {
            if (downTimerText == null)
                return;

            remainingTime = Mathf.Max(
                0f,
                remainingTime
            );

            downTimerText.text =
                Mathf.CeilToInt(remainingTime).ToString();
        }

        private void ShowPlayerState(string message)
        {
            if (playerStateText == null)
                return;

            playerStateText.text = message;
            playerStateText.gameObject.SetActive(true);
        }

        private void HidePlayerState()
        {
            if (playerStateText == null)
                return;

            playerStateText.gameObject.SetActive(false);
        }

        private void ShowDownUI()
        {
            if (downPanel != null)
                downPanel.SetActive(true);
        }

        private void HideDownUI()
        {
            if (downPanel != null)
                downPanel.SetActive(false);
        }
    }
}