using FourPlayerHorror.Audio;
using FourPlayerHorror.Audio.Horror;
using FourPlayerHorror.Core;
using System.Collections;
using UnityEngine;

namespace FourPlayerHorror.Audio.Generator
{
    public class GeneratorPresentation : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioData generatorStartAudio;
        [SerializeField] private AudioData powerOnAudio;

        [Header("Lighting")]
        [SerializeField] private LightingController lightingController;

        [Header("Timing")]
        [SerializeField] private float powerOnDelay = 1.5f;

        private bool hasPlayed;

        private void OnEnable()
        {
            GeneratorCompletedEvent.OnCompleted +=
                HandleGeneratorCompleted;
        }

        private void OnDisable()
        {
            GeneratorCompletedEvent.OnCompleted -=
                HandleGeneratorCompleted;
        }

        private void HandleGeneratorCompleted()
        {
            if (hasPlayed)
                return;

            hasPlayed = true;

            StartCoroutine(
                GeneratorCompleteRoutine()
            );
        }

        private IEnumerator GeneratorCompleteRoutine()
        {
            PlayGeneratorStartAudio();

            yield return new WaitForSeconds(
                powerOnDelay
            );

            PlayPowerOnAudio();

            if (lightingController != null)
                lightingController.TurnOn();
        }

        private void PlayGeneratorStartAudio()
        {
            if (generatorStartAudio == null)
                return;

            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(
                generatorStartAudio
            );
        }

        private void PlayPowerOnAudio()
        {
            if (powerOnAudio == null)
                return;

            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(
                powerOnAudio
            );
        }
    }
}