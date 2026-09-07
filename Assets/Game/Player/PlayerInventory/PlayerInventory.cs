using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Player
{
    public class PlayerInventory : NetworkBehaviour
    {
        private NetworkVariable<int> fuseCount =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public int FuseCount => fuseCount.Value;

        public bool HasFuse()
        {
            return fuseCount.Value > 0;
        }

        public void AddFuse()
        {
            if (!IsServer)
                return;

            fuseCount.Value++;
        }

        public bool RemoveFuse()
        {
            if (!IsServer)
                return false;

            if (fuseCount.Value <= 0)
                return false;

            fuseCount.Value--;

            return true;
        }
    }
}