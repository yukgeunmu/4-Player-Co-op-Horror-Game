using UnityEngine;

namespace FourPlayerHorror.Monster
{
    public class MonsterSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            Gizmos.DrawRay(
                transform.position,
                transform.forward
            );
        }
    }
}