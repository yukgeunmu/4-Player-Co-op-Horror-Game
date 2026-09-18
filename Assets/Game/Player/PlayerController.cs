using FourPlayerHorror.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FourPlayerHorror.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour, IInteractable
    {
        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 5f;


        [Header("Look")]
        [SerializeField]
        private float mouseSensitivity = 0.1f;

        [SerializeField]
        private Transform cameraRoot;

        public Transform CameraRoot => cameraRoot;

        [SerializeField]
        private float minLookAngle = -80f;

        [SerializeField]
        private float maxLookAngle = 80f;

        [Header("Input")]
        [SerializeField]
        private InputActionReference moveAction;

        [SerializeField]
        private InputActionReference lookAction;

        [Header("Animator")]
        [SerializeField]
        private Animator animator;

        private CharacterController characterController;

        private float cameraPitch;

        public NetworkVariable<PlayerState> playerState =
            new NetworkVariable<PlayerState>(
                global::PlayerState.Normal,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);


        private readonly NetworkVariable<Vector3> downPosition =
            new(
                Vector3.zero,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
                );

        private readonly NetworkVariable<Quaternion> downRotation =
            new(
                Quaternion.identity,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
                );


        private readonly NetworkVariable<bool> isBeingRevived =
            new(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
                );

        private readonly NetworkVariable<bool> isReviving =
            new(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
                );

        public bool IsReviving => isReviving.Value;

        private readonly NetworkVariable<ulong> reviveTargetClientId =
            new(
                ulong.MaxValue,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
                );

        public ulong ReviveTargetClientId =>
            reviveTargetClientId.Value;


        public PlayerState CurrentState => playerState.Value;

        public event System.Action<PlayerState, PlayerState> OnStateChanged;

        public Vector3 DownPosition => downPosition.Value;

        public Quaternion DownRotation => downRotation.Value;

        public bool IsBeingRevived => isBeingRevived.Value;

        private readonly NetworkVariable<double> reviveStartServerTime =
            new(
                0d,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        private ulong reviveRescuerClientId = ulong.MaxValue;
        public ulong ReviveRescuerClientId => reviveRescuerClientId;

        private readonly NetworkVariable<double> downStartServerTime =
            new(
                0d,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
    );

        private int normalLayer;

        public bool IsDown => playerState.Value == global::PlayerState.Down;
        public bool IsDead => playerState.Value == global::PlayerState.Dead;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            normalLayer = gameObject.layer;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            playerState.OnValueChanged += OnPlayerStateChanged;
            downPosition.OnValueChanged += OnDownPositionChanged;
            downRotation.OnValueChanged += OnDownRotationChanged;

            if (!IsOwner)
                return;

            moveAction.action.Enable();
            lookAction.action.Enable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnNetworkDespawn()
        {
            HandleDisconnect();

            playerState.OnValueChanged -= OnPlayerStateChanged;
            downPosition.OnValueChanged -= OnDownPositionChanged;
            downRotation.OnValueChanged -= OnDownRotationChanged;

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
            if (IsServer)
            {
                UpdateRevive();
                UpdateDownTimer();
            }


            if (!IsOwner)
                return;

            HandleLook();
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (CurrentState != PlayerState.Normal)
                return;

            Vector2 input = moveAction.action.ReadValue<Vector2>();

            if (IsReviving && input.sqrMagnitude > 0.01f)
            {
                CancelReviveByMovement();
                return;
            }

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
            if (CurrentState == PlayerState.Dead)
                return;

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

        private void HandleDeath()
        {
            if (!IsServer)
                return;

            // 구조 중이었다면 구조 취소
            if (IsBeingRevived)
            {
                CancelRevive();
            }

            // 이 플레이어가 구조자였다면
            // 구조 대상의 Revive도 취소
            if (IsReviving)
            {
                PlayerController target = FindReviveTarget();

                if (target != null)
                {
                    target.CancelRevive();
                }

                isReviving.Value = false;
                reviveTargetClientId.Value = ulong.MaxValue;
            }

            // Down Timer 종료
            downStartServerTime.Value = 0d;

            Debug.Log(
                $"Player {OwnerClientId} died."
            );
        }


        public void SetState(PlayerState newState)
        {
            if (!IsServer)
                return;

            if (playerState.Value == newState)
                return;

            if (newState == PlayerState.Dead)
            {
                HandleDeath();
            }

            playerState.Value = newState;
        }

        /// <summary>
        /// 서버에서 플레이어를 Down 상태로 전환한다.
        /// 공격 판정이 성공한 최종 위치/자세를 함께 확정한다.
        /// </summary>
        public bool SetDown(Vector3 finalPosition, Quaternion finalRotation)
        {
            if (!IsServer)
                return false;

            // Normal 상태에서만 Down 가능
            if (playerState.Value != PlayerState.Normal)
                return false;

            // 현재 이 플레이어가 구조 중이라면
            // 자신이 피격된 것이므로 구조를 먼저 취소한다.
            if (IsReviving)
            {
                PlayerController target = FindReviveTarget();

                if (target != null)
                {
                    target.CancelRevive();
                }
            }

            // 서버가 최종 Down 위치/자세를 확정
            downPosition.Value = finalPosition;
            downRotation.Value = finalRotation;

            // 실제 Transform도 서버에서 즉시 확정
            ApplyDownTransform();

            // Down 시작 시각 기록
            downStartServerTime.Value =
                NetworkManager.Singleton.ServerTime.Time;

            // 마지막에 상태 변경
            playerState.Value = PlayerState.Down;

            return true;
        }

        private void OnPlayerStateChanged(PlayerState previousState, PlayerState newState)
        {
            Debug.Log(
                $"Player {OwnerClientId} State: " +
                $"{previousState} -> {newState}"
            );

            if (newState == PlayerState.Normal)
            {
                RestoreNormalCollision();
            }

            if (newState == PlayerState.Down)
            {
                ApplyDownTransform();
                ApplyDownCollision();
            }

            if (newState == PlayerState.Dead)
            {
                ApplyDeadVisual();
            }

            OnStateChanged?.Invoke(previousState, newState);
        }

        private void OnDownPositionChanged(Vector3 previousPosition, Vector3 newPosition)
        {
            if (CurrentState != PlayerState.Down)
                return;

            ApplyDownTransform();
        }

        private void OnDownRotationChanged(Quaternion previousRotation, Quaternion newRotation)
        {
            if (CurrentState != PlayerState.Down)
                return;

            ApplyDownTransform();
        }

        private void ApplyDownTransform()
        {
            if (CurrentState != PlayerState.Down &&
                !IsServer)
            {
                return;
            }

            characterController.enabled = false;

            transform.SetPositionAndRotation(
                downPosition.Value,
                downRotation.Value
            );

            characterController.enabled = true;
        }

        private void ApplyDownCollision()
        {
            int downLayer = LayerMask.NameToLayer("DownPlayer");

            if (downLayer == -1)
            {
                Debug.LogError("DownPlayer Layer가 존재하지 않습니다.");
                return;
            }

            gameObject.layer = downLayer;

            Debug.Log(
                $"Player {OwnerClientId} Down Collision Layer = {LayerMask.LayerToName(gameObject.layer)}"
            );
        }

        private void RestoreNormalCollision()
        {
            gameObject.layer = normalLayer;
        }

        public string GetInteractionText()
        {
            return "Rescue";
        }

        public void Interact(ulong clientId)
        {
            if (!IsServer)
                return;

            TryStartRevive(clientId);
        }

        private void TryStartRevive(ulong clientId)
        {
            // 구조 대상 상태 확인
            if (CurrentState != PlayerState.Down)
            {
                Debug.LogWarning(
                    $"Revive rejected. Target state: {CurrentState}"
                );
                return;
            }

            // 이미 다른 플레이어가 구조 중
            if (IsBeingRevived)
            {
                Debug.LogWarning(
                    $"Revive rejected. Player {OwnerClientId} is already being revived."
                );

                return;
            }

            // 구조자 확인
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    clientId,
                    out NetworkClient rescuerClient))
            {
                return;
            }

            NetworkObject rescuerObject = rescuerClient.PlayerObject;

            if (rescuerObject == null)
                return;

            PlayerController rescuer =
                rescuerObject.GetComponent<PlayerController>();

            if (rescuer == null)
                return;

            // 구조자는 반드시 Normal
            if (rescuer.CurrentState != PlayerState.Normal)
            {
                Debug.LogWarning(
                    $"Revive rejected. Rescuer state: {rescuer.CurrentState}"
                );

                return;
            }

            // 같은 플레이어 자기 자신인지 확인
            if (rescuer == this)
            {
                Debug.LogWarning("Revive rejected. Cannot revive yourself.");
                return;
            }

            // 거리 검증
            float distance = Vector3.Distance(
                transform.position,
                rescuer.transform.position
            );

            if (distance > InteractionConst.InteractionDistance)
            {
                Debug.LogWarning(
                    $"Revive rejected. Distance: {distance:F2}"
                );

                return;
            }

            // 구조 락 획득
            isBeingRevived.Value = true;
            reviveRescuerClientId = clientId;

            reviveStartServerTime.Value =
                NetworkManager.Singleton.ServerTime.Time;

            rescuer.isReviving.Value = true;

            rescuer.reviveTargetClientId.Value = OwnerClientId;

            Debug.Log(
                $"Revive started. " +
                $"Target: {OwnerClientId}, " +
                $"Rescuer: {clientId}"
            );
        }

        public void CancelRevive()
        {
            if (!IsServer)
                return;

            PlayerController rescuer = null;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    reviveRescuerClientId,
                    out NetworkClient rescuerClient))
            {
                if (rescuerClient.PlayerObject != null)
                {
                    rescuer =
                        rescuerClient.PlayerObject
                            .GetComponent<PlayerController>();
                }
            }

            // 구조자 이동 제한 해제
            if (rescuer != null)
            {
                rescuer.isReviving.Value = false;
                rescuer.reviveTargetClientId.Value =
                    ulong.MaxValue;
            }

            // 구조 상태 해제
            isBeingRevived.Value = false;
            reviveRescuerClientId = ulong.MaxValue;
            reviveStartServerTime.Value = 0f;

            Debug.Log(
                $"Revive cancelled. Target: {OwnerClientId}"
            );
        }

        public void CancelReviveByRescuer(ulong rescuerClientId)
        {
            if (!IsServer)
                return;

            if (!IsBeingRevived)
                return;

            if (ReviveRescuerClientId != rescuerClientId)
                return;

            Debug.Log(
                $"Revive cancelled because rescuer {rescuerClientId} disconnected. " +
                $"Target: {OwnerClientId}"
            );

            CancelRevive();
        }

        private void UpdateRevive()
        {
            if (!isBeingRevived.Value)
                return;

            // 구조 대상이 더 이상 Down이 아니면 취소
            if (CurrentState != PlayerState.Down)
            {
                CancelRevive();
                return;
            }

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    reviveRescuerClientId,
                    out NetworkClient rescuerClient))
            {
                CancelRevive();
                return;
            }

            NetworkObject rescuerObject = rescuerClient.PlayerObject;

            if (rescuerObject == null)
            {
                CancelRevive();
                return;
            }

            PlayerController rescuer =
                rescuerObject.GetComponent<PlayerController>();

            if (rescuer == null)
            {
                CancelRevive();
                return;
            }

            // 구조자가 더 이상 Normal이 아니면 취소
            if (rescuer.CurrentState != PlayerState.Normal)
            {
                CancelRevive();
                return;
            }

            // 거리 확인
            float distance = Vector3.Distance(
                transform.position,
                rescuer.transform.position
            );

            if (distance > InteractionConst.InteractionDistance)
            {
                CancelRevive();
                return;
            }

            // 3초 완료
            double elapsedTime =
                NetworkManager.Singleton.ServerTime.Time -
                reviveStartServerTime.Value;

            if (elapsedTime >= InteractionConst.ReviveDuration)
            {
                CompleteRevive();
            }
        }

        private void CompleteRevive()
        {
            if (!IsServer)
                return;

            // 이미 다른 상태가 되었다면 무시
            if (CurrentState != PlayerState.Down)
                return;

            PlayerController rescuer = null;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(reviveRescuerClientId, out NetworkClient rescuerClient))
            {
                if (rescuerClient.PlayerObject != null)
                {
                    rescuer =
                        rescuerClient.PlayerObject
                            .GetComponent<PlayerController>();
                }
            }

            // 구조 대상 정상 복귀
            SetState(PlayerState.Normal);

            if (rescuer != null)
            {
                rescuer.isReviving.Value = false;
                rescuer.reviveTargetClientId.Value = ulong.MaxValue;
            }


            // 구조 락 해제
            isBeingRevived.Value = false;
            reviveRescuerClientId = ulong.MaxValue;
            reviveStartServerTime.Value = 0f;

            Debug.Log(
                $"Revive completed. Target: {OwnerClientId}"
            );
        }

        private void CancelReviveByMovement()
        {
            if (!IsOwner)
                return;

            if (!IsReviving)
                return;

            CancelReviveServerRpc();
        }

        [ServerRpc]
        private void CancelReviveServerRpc()
        {
            if (!IsReviving)
                return;

            PlayerController target = FindReviveTarget();

            if (target == null)
                return;

            target.CancelRevive();
        }

        private PlayerController FindReviveTarget()
        {
            if (!IsServer)
                return null;

            foreach (NetworkClient client
                     in NetworkManager.Singleton.ConnectedClientsList)
            {
                NetworkObject playerObject = client.PlayerObject;

                if (playerObject == null)
                    continue;

                PlayerController player =
                    playerObject.GetComponent<PlayerController>();

                if (player == null)
                    continue;

                if (player.IsBeingRevived &&
                    player.ReviveRescuerClientId == OwnerClientId)
                {
                    return player;
                }
            }

            return null;
        }

        private void UpdateDownTimer()
        {
            if (CurrentState != PlayerState.Down)
                return;

            double elapsedTime =
                NetworkManager.Singleton.ServerTime.Time -
                downStartServerTime.Value;

            if (elapsedTime < InteractionConst.DownDuration)
                return;

            HandleDownTimeout();
        }

        private void HandleDownTimeout()
        {
            if (!IsServer)
                return;

            // 이미 상태가 바뀌었다면 무시
            if (CurrentState != PlayerState.Down)
                return;

            SetState(PlayerState.Dead);

            Debug.Log(
                $"Player {OwnerClientId} died. Down timeout."
            );
        }

        private void ApplyDeadVisual()
        {
            if (animator == null)
                return;

            //animator.SetTrigger("Dead");

            // Step 4-9에서 관전 카메라 전환 연결
            Debug.Log(
                $"Player {OwnerClientId} entered Dead state."
            );
        }

        private void HandleDisconnect()
        {
            if (!IsServer)
                return;

            if (CurrentState == PlayerState.Down)
            {
                Debug.Log(
                    $"Player {OwnerClientId} disconnected while Down. " +
                    "Player is treated as Dead."
                );

                SetState(PlayerState.Dead);
            }

            // 자신이 구조자였다면 다른 플레이어의 Revive를 취소한다.
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                    continue;

                PlayerController target =
                    client.PlayerObject.GetComponent<PlayerController>();

                if (target == null)
                    continue;

                if (target.IsBeingRevived &&
                    target.ReviveRescuerClientId == OwnerClientId)
                {
                    target.CancelReviveByRescuer(OwnerClientId);
                }
            }
        }

        public float GetRemainingDownTime()
        {
            if (CurrentState != PlayerState.Down)
                return 0f;

            if (NetworkManager.Singleton == null)
                return 0f;

            double elapsedTime =
                NetworkManager.Singleton.ServerTime.Time -
                downStartServerTime.Value;

            double remainingTime =
                InteractionConst.DownDuration -
                elapsedTime;

            return Mathf.Max(
                0f,
                (float)remainingTime
            );
        }

        public float GetReviveProgress()
        {
            if (!IsBeingRevived)
                return 0f;

            if (NetworkManager.Singleton == null)
                return 0f;

            double elapsedTime =
                NetworkManager.Singleton.ServerTime.Time -
                reviveStartServerTime.Value;

            float progress =
                (float)(
                    elapsedTime /
                    InteractionConst.ReviveDuration
                );

            return Mathf.Clamp01(progress);
        }

        public float GetReviveTargetProgress()
        {
            PlayerController target =
                GetReviveTargetPlayer();

            if (target == null)
                return 0f;

            return target.GetReviveProgress();
        }

        public PlayerController GetReviveTargetPlayer()
        {
            if (!IsReviving)
                return null;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    ReviveTargetClientId,
                    out NetworkClient targetClient))
            {
                return null;
            }

            if (targetClient.PlayerObject == null)
                return null;

            return targetClient.PlayerObject
                .GetComponent<PlayerController>();
        }
    }
}