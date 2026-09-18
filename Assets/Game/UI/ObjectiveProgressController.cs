using TMPro;
using UnityEngine;

namespace FourPlayerHorror.UI.HUD
{
    public class ObjectiveProgressController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private TMP_Text progressText;

        [Header("Objective")]
        [SerializeField]
        private int maxFuseCount = 5;

        private int currentFuseCount;

        private void Awake()
        {
            UpdateUI(0);
        }

        public void SetProgress(int count)
        {
            currentFuseCount = Mathf.Clamp(
                count,
                0,
                maxFuseCount
            );

            UpdateUI(currentFuseCount);
        }

        private void UpdateUI(int count)
        {
            if (progressText == null)
                return;

            progressText.text =
                $"{CreateProgressBar(count)} {count} / {maxFuseCount}";
        }

        private string CreateProgressBar(int count)
        {
            return
                "[" +
                new string('бс', count) +
                new string('бр', maxFuseCount - count) +
                "]";
        }
    }
}