using UnityEngine;

namespace FourPlayerHorror.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField]
        private AudioSource bgmSource;

        [SerializeField]
        private AudioSource sfxSource;

        [Header("Volume")]
        [SerializeField]
        [Range(0f, 1f)]
        private float masterVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float bgmVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float sfxVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        public void PlayBGM(AudioData audioData)
        {
            if (bgmSource == null)
                return;

            if (audioData == null ||
                audioData.clip == null)
                return;

            bgmSource.clip = audioData.clip;
            bgmSource.volume =
                audioData.volume *
                masterVolume *
                bgmVolume;

            bgmSource.pitch = audioData.pitch;
            bgmSource.loop = audioData.loop;

            bgmSource.Play();
        }

        public void StopBGM()
        {
            if (bgmSource == null)
                return;

            bgmSource.Stop();
        }

        public void PlaySFX(AudioData audioData)
        {
            if (sfxSource == null)
                return;

            if (audioData == null ||
                audioData.clip == null)
                return;

            float volume =
                audioData.volume *
                masterVolume *
                sfxVolume;

            sfxSource.PlayOneShot(
                audioData.clip,
                volume
            );
        }

        public void StopSFX()
        {
            if (sfxSource == null)
                return;

            sfxSource.Stop();
        }

        public void Play3DSFX( AudioData audioData, Vector3 position)
        {
            if (audioData == null)
                return;

            if (audioData.clip == null)
                return;

            GameObject audioObject =
                new GameObject("3D Audio");

            audioObject.transform.position = position;

            AudioSource source =
                audioObject.AddComponent<AudioSource>();

            source.clip = audioData.clip;
            source.volume =
                audioData.volume *
                masterVolume *
                sfxVolume;

            source.pitch = audioData.pitch;
            source.loop = audioData.loop;

            source.spatialBlend = audioData.spatialBlend;

            source.minDistance =
                audioData.minDistance;

            source.maxDistance =
                audioData.maxDistance;

            source.rolloffMode =
                AudioRolloffMode.Linear;

            source.Play();

            if (!audioData.loop)
            {
                Destroy(
                    audioObject,
                    audioData.clip.length
                );
            }
        }
    }
}