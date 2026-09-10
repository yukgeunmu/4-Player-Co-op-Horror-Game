using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace FourPlayerHorror.Monster
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterController : NetworkBehaviour
    {
        private NavMeshAgent agent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // NavMeshAgent는 Server에서만 사용
            agent.enabled = IsServer;
        }


        public void SetDestination(Vector3 destination)
        {
            if (!IsServer)
                return;

            if (!agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        public bool HasReachedDestination(float stoppingDistance = 0.2f)
        {
            if (!IsServer)
                return false;

            if (!agent.isOnNavMesh)
                return false;

            if (agent.pathPending)
                return false;

            if (agent.remainingDistance == Mathf.Infinity)
                return false;

            return agent.remainingDistance <=
                   agent.stoppingDistance + stoppingDistance;
        }

        public void Stop()
        {
            if (!IsServer)
                return;

            if (!agent.isOnNavMesh)
                return;

            agent.isStopped = true;
        }

        public void Resume()
        {
            if (!IsServer)
                return;

            if (!agent.isOnNavMesh)
                return;

            agent.isStopped = false;
        }

        public bool IsMoving()
        {
            if (!IsServer)
                return false;

            if (!agent.isOnNavMesh)
                return false;

            return agent.velocity.sqrMagnitude > 0.01f;
        }
    }
}