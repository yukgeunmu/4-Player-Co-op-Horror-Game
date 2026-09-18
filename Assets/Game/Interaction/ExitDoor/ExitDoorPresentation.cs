using FourPlayerHorror.Audio;
using TMPro;
using UnityEngine;

namespace FourPlayerHorror.Interaction
{
    public class ExitDoorPresentation : MonoBehaviour
    {
        [Header("Exit Door")]
        [SerializeField] private ExitDoor exitDoor;

        [Header("Lighting")]
        [SerializeField] private Light[] doorLights;

        [Header("Audio")]
        [SerializeField] private AudioData activationAudio;

        [Header("HUD")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float messageDuration = 3f;

        private bool hasPlayed;
        private float messageTimer;

        private void Awake()
        {
            HideMessage();
            SetDoorLights(false);
        }

        private void OnEnable()
        {
            if (exitDoor != null)
            {
                exitDoor.OnActivated +=
                    HandleExitDoorActivated;
            }
        }

        private void OnDisable()
        {
            if (exitDoor != null)
            {
                exitDoor.OnActivated -=
                    HandleExitDoorActivated;
            }
        }

        private void Update()
        {
            UpdateMessage();
        }

        private void HandleExitDoorActivated()
        {
            if (hasPlayed)
                return;

            PlayActivationPresentation();
        }

        private void PlayActivationPresentation()
        {
            hasPlayed = true;

            SetDoorLights(true);
            PlayActivationAudio();
            ShowMessage();

            Debug.Log("ExitDoor Presentation: 탈출구 활성화 연출");
        }

        private void SetDoorLights(bool enabled)
        {
            if (doorLights == null)
                return;

            foreach (Light light in doorLights)
            {
                if (light == null)
                    continue;

                light.enabled = enabled;
            }
        }

        private void PlayActivationAudio()
        {
            if (activationAudio == null)
                return;

            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(
                activationAudio
            );
        }

        private void ShowMessage()
        {
            if (messageText == null)
                return;

            messageText.text =
                "The escape route has been activated.";

            messageText.gameObject.SetActive(true);

            messageTimer = messageDuration;
        }

        private void UpdateMessage()
        {
            if (messageText == null)
                return;

            if (!messageText.gameObject.activeSelf)
                return;

            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0f)
            {
                HideMessage();
            }
        }

        private void HideMessage()
        {
            if (messageText == null)
                return;

            messageText.gameObject.SetActive(false);
        }

    }
}