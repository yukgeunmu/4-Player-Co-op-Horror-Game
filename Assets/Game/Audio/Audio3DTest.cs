using FourPlayerHorror.Audio;
using UnityEngine;

namespace FourPlayerHorror.Audio
{
    public class Audio3DTest : MonoBehaviour
    {
        [SerializeField]
        private AudioData testAudio;

        [SerializeField]
        private float interval = 3f;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer < interval)
                return;

            timer = 0f;

            if (AudioManager.Instance == null)
                return;

            AudioManager.Instance.Play3DSFX(
                testAudio,
                transform.position
            );
        }
    }
}