using FourPlayerHorror.Player;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Audio.Horror
{
    public class HorrorTrigger : MonoBehaviour
    {
        [Header("Horror Event")]
        [SerializeField] private HorrorEvent horrorEvent;

        [Header("Trigger")]
        [SerializeField] private bool oneShot = true;

        [Header("Jump Scare")]
        [SerializeField] private JumpScareEvent jumpScareEvent;

        private bool hasTriggered;

        private void Awake()
        {
            if (horrorEvent == null)
                horrorEvent = GetComponent<HorrorEvent>();

            if (jumpScareEvent == null)
                jumpScareEvent = GetComponent<JumpScareEvent>();

        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneShot && hasTriggered)
                return;

            PlayerController player =
                other.GetComponentInParent<PlayerController>();

            if (player == null)
                return;

            NetworkObject networkObject =
                player.GetComponent<NetworkObject>();

            if (networkObject == null)
                return;

            // 자신의 Player가 들어온 경우에만 실행
            if (!networkObject.IsOwner)
                return;

            TriggerHorrorEvent();
        }

        private void TriggerHorrorEvent()
        {
            if (horrorEvent == null)
            {
                Debug.LogWarning(
                    $"HorrorTrigger: HorrorEvent가 연결되지 않았습니다. " +
                    $"Object = {gameObject.name}"
                );

                return;
            }

            if (oneShot)
                hasTriggered = true;

            if (jumpScareEvent != null)
            {
                jumpScareEvent.Play();
                return;
            }

            if (horrorEvent != null)
            {
                horrorEvent.Play();
                return;
            }

            Debug.Log(
                $"HorrorTrigger: 공포 이벤트 실행 - {gameObject.name}"
            );
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }
    }
}