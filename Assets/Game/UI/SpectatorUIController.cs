using UnityEngine;

namespace FourPlayerHorror.UI.Spectator
{
    public class SpectatorUIController : MonoBehaviour
    {
        [SerializeField]
        private GameObject spectatorPanel;

        private void Awake()
        {
            Hide();
        }

        public void Show()
        {
            if (spectatorPanel == null)
                return;

            spectatorPanel.SetActive(true);
        }

        public void Hide()
        {
            if (spectatorPanel == null)
                return;

            spectatorPanel.SetActive(false);
        }
    }
}