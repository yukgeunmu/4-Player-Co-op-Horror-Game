using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace FourPlayerHorror.Monster
{
    public class MonsterSpawnManager : NetworkBehaviour
    {
        [Header("Monster")]
        [SerializeField]
        private NetworkObject monsterPrefab;

        [Header("Spawn Points")]
        [SerializeField]
        private MonsterSpawnPoint[] spawnPoints;

        [Header("NavMesh")]
        [SerializeField]
        private float navMeshSampleRadius = 2f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer)
                return;

            SpawnMonster();
        }

        private void SpawnMonster()
        {
            if (monsterPrefab == null)
            {
                Debug.LogError(
                    "MonsterSpawnManager: Monster Prefab이 설정되지 않았습니다."
                );

                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError(
                    "MonsterSpawnManager: Spawn Point가 없습니다."
                );

                return;
            }

            MonsterSpawnPoint spawnPoint =
                spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (!TryGetNavMeshPosition(
                    spawnPoint.transform.position,
                    out Vector3 spawnPosition))
            {
                Debug.LogError(
                    $"MonsterSpawnManager: " +
                    $"'{spawnPoint.name}'에서 유효한 NavMesh 위치를 찾지 못했습니다."
                );

                return;
            }

            NetworkObject monster =
                Instantiate(
                    monsterPrefab,
                    spawnPosition,
                    spawnPoint.transform.rotation
                );

            monster.Spawn();
        }

        private bool TryGetNavMeshPosition(
            Vector3 position,
            out Vector3 navMeshPosition)
        {
            if (NavMesh.SamplePosition(
                    position,
                    out NavMeshHit hit,
                    navMeshSampleRadius,
                    NavMesh.AllAreas))
            {
                navMeshPosition = hit.position;
                return true;
            }

            navMeshPosition = position;
            return false;
        }
    }
}