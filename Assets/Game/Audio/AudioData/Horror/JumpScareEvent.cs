using System.Collections;
using UnityEngine;

namespace FourPlayerHorror.Audio.Horror
{
    public class JumpScareEvent : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioData jumpScareAudio;

        [Header("Visual")]
        [SerializeField] private GameObject jumpScareObject;

        [Header("Timing")]
        [SerializeField] private float duration = 1f;

        private bool isPlaying;

        public void Play()
        {
            if (isPlaying)
                return;

            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            isPlaying = true;

            PlayAudio();
            ShowVisual();

            yield return new WaitForSeconds(duration);

            HideVisual();

            isPlaying = false;
        }

        private void PlayAudio()
        {
            if (jumpScareAudio == null)
                return;

            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.PlaySFX(
                jumpScareAudio
            );
        }

        private void ShowVisual()
        {
            if (jumpScareObject == null)
                return;

            jumpScareObject.SetActive(true);
        }

        private void HideVisual()
        {
            if (jumpScareObject == null)
                return;

            jumpScareObject.SetActive(false);
        }

        private void Awake()
        {
            HideVisual();
        }
    }
}