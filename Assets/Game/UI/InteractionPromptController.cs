using TMPro;
using UnityEngine;

namespace FourPlayerHorror.UI.Interaction
{
    public class InteractionPromptController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private GameObject promptRoot;

        [SerializeField]
        private TMP_Text promptText;

        private void Awake()
        {
            Hide();
        }

        public void Show(string interactionText)
        {

            if (promptText != null)
            {
                promptText.text =
                    $"[E] {interactionText}";
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }
    }
}