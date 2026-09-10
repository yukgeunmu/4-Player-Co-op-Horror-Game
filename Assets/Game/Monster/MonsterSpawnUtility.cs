using UnityEngine;
using UnityEngine.AI;

namespace FourPlayerHorror.Monster
{
    public static class MonsterSpawnUtility
    {
        public static bool TryGetValidPosition(
            Vector3 position,
            float radius,
            out Vector3 validPosition)
        {
            if (NavMesh.SamplePosition(
                    position,
                    out NavMeshHit hit,
                    radius,
                    NavMesh.AllAreas))
            {
                validPosition = hit.position;
                return true;
            }

            validPosition = position;
            return false;
        }
    }
}