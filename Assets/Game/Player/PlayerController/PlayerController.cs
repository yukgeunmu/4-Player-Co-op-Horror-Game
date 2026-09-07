using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField]
        private float moveSpeed = 5f;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            HandleMovement();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical);

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            characterController.Move(
                direction * moveSpeed * Time.deltaTime
            );
        }
    }
}