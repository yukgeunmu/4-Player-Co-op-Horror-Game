using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FourPlayerHorror.Audio.Horror
{
    public class LightingController : MonoBehaviour
    {
        [Header("Lights")]
        [SerializeField] private Light[] lights;

        [Header("Default")]
        [SerializeField] private bool defaultEnabled = true;

        private Coroutine flickerCoroutine;

        private void Awake()
        {
            SetLights(defaultEnabled);
        }

        public void SetLights(bool enabled)
        {
            if (lights == null)
                return;

            foreach (Light light in lights)
            {
                if (light == null)
                    continue;

                light.enabled = enabled;
            }
        }

        public void TurnOn()
        {
            SetLights(true);
        }

        public void TurnOff()
        {
            SetLights(false);
        }

        public void Flicker(
            int count = 3,
            float offDuration = 0.1f,
            float interval = 0.2f)
        {
            if (flickerCoroutine != null)
                StopCoroutine(flickerCoroutine);

            flickerCoroutine = StartCoroutine(
                FlickerRoutine(
                    count,
                    offDuration,
                    interval
                )
            );
        }

        private IEnumerator FlickerRoutine(
            int count,
            float offDuration,
            float interval)
        {
            if (lights == null || lights.Length == 0)
                yield break;

            bool originalState = lights[0] != null &&
                                 lights[0].enabled;

            for (int i = 0; i < count; i++)
            {
                SetLights(false);

                yield return new WaitForSeconds(
                    offDuration
                );

                SetLights(true);

                if (i < count - 1)
                {
                    yield return new WaitForSeconds(
                        interval
                    );
                }
            }

            SetLights(originalState);

            flickerCoroutine = null;
        }

        private void OnDisable()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
        }
    }
}