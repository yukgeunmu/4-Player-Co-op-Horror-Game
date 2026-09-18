using TMPro;
using UnityEngine;

namespace FourPlayerHorror.UI.GameResultUI
{
    public class GameResultUIController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private GameObject resultPanel;

        [SerializeField]
        private TMP_Text resultTitle;

        [SerializeField]
        private TMP_Text resultMessage;

        private void Awake()
        {
            Hide();
        }

        public void ShowVictory()
        {
            Show(
                "VICTORY",
                "All players have escaped."
            );
        }

        public void ShowDefeat()
        {
            Show(
                "DEFEAT",
                "All players have died."
            );
        }

        public void Hide()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void Show(
            string title,
            string message)
        {
            if (resultTitle != null)
            {
                resultTitle.text = title;
            }

            if (resultMessage != null)
            {
                resultMessage.text = message;
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }
        }
    }
}