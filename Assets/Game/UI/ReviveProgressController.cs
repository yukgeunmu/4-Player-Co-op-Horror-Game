using TMPro;
using UnityEngine;

namespace FourPlayerHorror.UI.PlayerStatus
{
    public class ReviveProgressController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private GameObject revivePanel;

        [SerializeField]
        private TMP_Text reviveText;

        [SerializeField]
        private TMP_Text reviveProgressText;

        //[Header("Revive")]
        //[SerializeField]
        //private float reviveDuration = 3f;

        private void Awake()
        {
            Hide();
        }

        public void Show()
        {
            if (revivePanel != null)
            {
                revivePanel.SetActive(true);
            }

            if (reviveText != null)
            {
                reviveText.text = "Rescue ...";
            }
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (reviveProgressText == null)
                return;

            int percent =
                Mathf.RoundToInt(progress * 100f);

            int barCount = 10;
            int filledCount =
                Mathf.RoundToInt(progress * barCount);

            filledCount =
                Mathf.Clamp(
                    filledCount,
                    0,
                    barCount
                );

            reviveProgressText.text =
                $"{new string('-', filledCount)}" +
                $"{new string('_', barCount - filledCount)} " +
                $"{percent}%";
        }

        public void ShowCancel()
        {
            if (reviveText != null)
            {
                reviveText.text = "Rescue Cancel";
            }
        }

        public void Hide()
        {
            if (revivePanel != null)
            {
                revivePanel.SetActive(false);
            }
        }
    }
}