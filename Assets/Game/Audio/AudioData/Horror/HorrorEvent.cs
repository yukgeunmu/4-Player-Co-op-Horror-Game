using FourPlayerHorror.Audio;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FourPlayerHorror.Audio.Horror
{
    public class HorrorEvent : MonoBehaviour
    {
        [Header("Event Data")]
        [SerializeField] private HorrorEventData eventData;

        [Header("References")]
        [SerializeField] private Light targetLight;

        private bool isPlaying;


        public void Play()
        {
            if (isPlaying)
                return;

            if (eventData == null)
            {
                Debug.LogWarning(
                    "HorrorEvent: EventData가 설정되지 않았습니다."
                );

                return;
            }

            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            isPlaying = true;

            PlaySound();

            if (eventData.UseLight)
            {
                yield return StartCoroutine(
                    PlayLightEffect()
                );
            }

            isPlaying = false;
        }

        private void PlaySound()
        {
            if (eventData.Sound == null)
                return;

            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(
                eventData.Sound
            );
        }

        private IEnumerator PlayLightEffect()
        {
            if (targetLight == null)
                yield break;

            bool originalState = targetLight.enabled;

            targetLight.enabled = false;

            yield return new WaitForSeconds(
                eventData.LightOffDuration
            );

            targetLight.enabled = originalState;
        }
    }
}