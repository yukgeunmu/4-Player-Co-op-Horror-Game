using FourPlayerHorror.Monster;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FourPlayerHorror.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 5f;


        [Header("Look")]
        [SerializeField]
        private float mouseSensitivity = 0.1f;

        [SerializeField]
        private Transform cameraRoot;

        [SerializeField]
        private float minLookAngle = -80f;

        [SerializeField]
        private float maxLookAngle = 80f;

        [Header("Input")]
        [SerializeField]
        private InputActionReference moveAction;

        [SerializeField]
        private InputActionReference lookAction;

        private CharacterController characterController;

        private float cameraPitch;

        public NetworkVariable<PlayerState> CurrentState = new NetworkVariable<PlayerState>(
                                                                PlayerState.Normal,
                                                                NetworkVariableReadPermission.Everyone,
                                                                NetworkVariableWritePermission.Server);


        public bool IsDown => CurrentState.Value == PlayerState.Down;
        public bool IsDead => CurrentState.Value == PlayerState.Dead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
                return;

            moveAction.action.Enable();
            lookAction.action.Enable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                moveAction.action.Disable();
                lookAction.action.Disable();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            HandleLook();

            if (CurrentState.Value != PlayerState.Normal)
                return;

            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();

            Vector3 direction =
                transform.forward * input.y +
                transform.right * input.x;

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            characterController.Move(
                direction * moveSpeed * Time.deltaTime
            );
        }

        private void HandleLook()
        {
            Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            // 좌우 회전
            transform.Rotate(
                Vector3.up * mouseX
            );

            // 상하 회전
            cameraPitch -= mouseY;

            cameraPitch = Mathf.Clamp(
                cameraPitch,
                minLookAngle,
                maxLookAngle
            );

            cameraRoot.localRotation = Quaternion.Euler(
                cameraPitch,
                0f,
                0f
            );
        }

        public void Down()
        {
            if (!IsServer)
                return;

            if (CurrentState.Value != PlayerState.Normal)
                return;

            CurrentState.Value =
                PlayerState.Down;
        }
    }
}