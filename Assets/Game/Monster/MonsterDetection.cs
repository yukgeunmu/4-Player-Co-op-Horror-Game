using FourPlayerHorror.Player;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Monster
{
    public enum DetectionAudioState
    {
        None,
        Low,
        Medium,
        High
    }


    public class MonsterDetection : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField]
        private float detectionRange = 15f;

        [SerializeField]
        private float detectionAngle = 120f;

        [SerializeField]
        private float detectionThreshold = 100f;

        [Header("Decay")]
        [SerializeField]
        private float detectionDecay = 10f;

        [Header("Noise")]
        [SerializeField]
        private float noiseDetectionRate = 20f;

        [Header("Light")]
        [SerializeField]
        private float lightDetectionRate = 15f;

        [Header("LOS")]
        [SerializeField]
        private LayerMask obstacleMask;

        private readonly Dictionary<ulong, float> detectionGauges =
            new Dictionary<ulong, float>();

        public float DetectionRange => detectionRange;

        public float DetectionThreshold => detectionThreshold;

        /// <summary>
        /// ////////////////////
        /// </summary>

        private NetworkVariable<DetectionAudioState> detectionAudioState =
            new NetworkVariable<DetectionAudioState>(
                DetectionAudioState.None,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public DetectionAudioState CurrentDetectionAudioState =>
            detectionAudioState.Value;


        private void Update()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            foreach (
                NetworkClient client
                in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkObject playerObject =
                    client.PlayerObject;

                if (playerObject == null)
                    continue;

                Transform player =
                    playerObject.transform;

                UpdateDetection(
                    client.ClientId,
                    player,
                    true,
                    true
                );
            }




        }

        public float GetGauge(ulong playerId)
        {
            if (!detectionGauges.TryGetValue(playerId, out float gauge))
                return 0f;

            return gauge;
        }


        public void UpdateDetection( ulong playerId, Transform player, bool isMakingNoise, bool isUsingFlashlight)
        {
            if (player == null)
                return;

            float gauge = GetGauge(playerId);

            // 자연 감소
            gauge -= detectionDecay * Time.deltaTime;

            // 범위 밖이면 감소만 적용
            if (!IsWithinRange(player))
            {
                detectionGauges[playerId] =
                    Mathf.Clamp(
                        gauge,
                        0f,
                        detectionThreshold
                    );

                return;
            }

            // 시야각
            if (!IsWithinFieldOfView(player))
            {
                detectionGauges[playerId] =
                    Mathf.Clamp(
                        gauge,
                        0f,
                        detectionThreshold
                    );

                return;
            }

            // LOS
            if (!HasLineOfSight(player))
            {
                detectionGauges[playerId] =
                    Mathf.Clamp(
                        gauge,
                        0f,
                        detectionThreshold
                    );

                return;
            }

            // 거리 감쇠
            float distanceMultiplier =
                GetDistanceMultiplier(player);

            // 소리
            if (isMakingNoise)
            {
                gauge +=
                    noiseDetectionRate *
                    distanceMultiplier *
                    Time.deltaTime;
            }

            // 손전등
            if (isUsingFlashlight)
            {
                gauge +=
                    lightDetectionRate *
                    distanceMultiplier *
                    Time.deltaTime;
            }

            detectionGauges[playerId] =
                Mathf.Clamp(
                    gauge,
                    0f,
                    detectionThreshold
                );
        }

        public bool IsDetected(ulong playerId)
        {
            return GetGauge(playerId) >= detectionThreshold;
        }

        private bool IsWithinRange(Transform player)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position
                );

            return distance <= detectionRange;
        }

        private bool IsWithinFieldOfView(Transform player)
        {
            Vector3 direction =
                player.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return true;

            float angle =
                Vector3.Angle(
                    transform.forward,
                    direction.normalized
                );

            return angle <= detectionAngle * 0.5f;
        }

        private bool HasLineOfSight(Transform player)
        {
            Vector3 origin =
                transform.position + Vector3.up * 1.2f;

            Vector3 target =
                player.position + Vector3.up * 1.0f;

            Vector3 direction =
                target - origin;

            float distance = direction.magnitude;

            if (Physics.Raycast(
                    origin,
                    direction.normalized,
                    out RaycastHit hit,
                    distance,
                    obstacleMask))
            {
                return false;
            }

            return true;
        }

        private float GetDistanceMultiplier(Transform player)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position
                );

            return Mathf.Clamp01(
                1f - distance / detectionRange
            );
        }

        public bool TryGetBestDetectedPlayer( out ulong playerId, out Transform player)
        {
            float highestGauge = detectionThreshold;

            playerId = default;
            player = null;

            foreach (NetworkClient client
                     in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkObject playerObject =
                    client.PlayerObject;

                if (playerObject == null)
                    continue;

                if (IsPlayerUnavailable(playerObject))
                    continue;

                float gauge =
                    GetGauge(client.ClientId);

                if (gauge < highestGauge)
                    continue;

                highestGauge = gauge;

                playerId = client.ClientId;
                player = playerObject.transform;
            }

            return player != null;
        }

        public bool TryGetHighestGaugePlayer( out ulong playerId, out float gauge)
        {
            playerId = default;
            gauge = 0f;

            foreach (NetworkClient client
                     in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkObject playerObject =
                    client.PlayerObject;

                if (playerObject == null)
                    continue;

                if (IsPlayerUnavailable(playerObject))
                    continue;

                float currentGauge =
                    GetGauge(client.ClientId);

                if (currentGauge <= gauge)
                    continue;

                gauge = currentGauge;
                playerId = client.ClientId;
            }

            return gauge > 0f;
        }

        public bool CanCurrentlyDetect(Transform player)
        {
            if (player == null)
                return false;

            if (!IsWithinRange(player))
                return false;

            if (!IsWithinFieldOfView(player))
                return false;

            if (!HasLineOfSight(player))
                return false;

            return true;
        }

        private bool IsPlayerUnavailable(NetworkObject playerObject)
        {
            PlayerController playerController =
                playerObject.GetComponent<PlayerController>();

            if (playerController == null)
                return true;

            return playerController.IsDown ||
                   playerController.IsDead;
        }

    }
}