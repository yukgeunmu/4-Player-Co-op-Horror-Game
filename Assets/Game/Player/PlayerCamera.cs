using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Player
{
    public class PlayerCamera : NetworkBehaviour
    {
        [SerializeField]
        private Camera playerCamera;

        private void Start()
        {
            if (!IsOwner)
            {
                playerCamera.gameObject.SetActive(false);
                return;
            }

            playerCamera.gameObject.SetActive(true);
        }
    }
}