using FourPlayerHorror.Monster;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Player
{
    public class FlashlightFlicker : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Light flashlight;

        [Header("Monster Detection")]
        [SerializeField] private float flickerStartDistance = 10f;
        [SerializeField] private float strongFlickerDistance = 5f;

        [Header("Flicker")]
        [SerializeField] private float weakFlickerInterval = 0.8f;
        [SerializeField] private float strongFlickerInterval = 0.25f;

        [SerializeField] private float weakOffDuration = 0.08f;
        [SerializeField] private float strongOffDuration = 0.15f;

        private MonsterController monster;

        private Coroutine flickerCoroutine;
        private bool originalEnabled;

        private void Awake()
        {
            if (flashlight == null)
                flashlight = GetComponentInChildren<Light>();

            if (flashlight != null)
                originalEnabled = flashlight.enabled;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 자신의 화면에서만 손전등 연출
            if (!IsOwner)
                return;

            monster = FindFirstObjectByType<MonsterController>();

            if (flashlight != null)
            {
                flashlight.enabled = originalEnabled;
            }
        }

        public override void OnNetworkDespawn()
        {
            StopFlicker();

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            if (flashlight == null)
                return;

            if (monster == null)
            {
                monster = FindFirstObjectByType<Monster.MonsterController>();

                if (monster == null)
                {
                    StopFlicker();
                    return;
                }
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    monster.transform.position
                );

            UpdateFlicker(distance);
        }

        private void UpdateFlicker(float distance)
        {
            // 몬스터가 충분히 멀리 있음
            if (distance > flickerStartDistance)
            {
                StopFlicker();
                return;
            }

            // 이미 깜빡이는 중이면 다시 시작하지 않음
            if (flickerCoroutine != null)
                return;

            if (distance <= strongFlickerDistance)
            {
                flickerCoroutine = StartCoroutine(
                    FlickerRoutine(
                        strongFlickerInterval,
                        strongOffDuration
                    )
                );
            }
            else
            {
                flickerCoroutine = StartCoroutine(
                    FlickerRoutine(
                        weakFlickerInterval,
                        weakOffDuration
                    )
                );
            }
        }

        private IEnumerator FlickerRoutine(
            float interval,
            float offDuration)
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);

                if (flashlight == null)
                    yield break;

                flashlight.enabled = false;

                yield return new WaitForSeconds(offDuration);

                if (flashlight == null)
                    yield break;

                flashlight.enabled = true;
            }
        }

        private void StopFlicker()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            if (flashlight != null)
                flashlight.enabled = originalEnabled;
        }
    }
}