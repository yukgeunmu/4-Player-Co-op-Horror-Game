using FourPlayerHorror.Player;
using Unity.Netcode;
using UnityEngine;

namespace FourPlayerHorror.Monster
{
    public class MonsterAI : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField]
        private MonsterController controller;

        [SerializeField]
        private MonsterDetection detection;

        [SerializeField]
        private Animator animator;

        [Header("Patrol")]
        [SerializeField]
        private float patrolRadius = 10f;

        [SerializeField]
        private float navMeshSampleRadius = 3f;

        [SerializeField]
        private int patrolPositionAttempts = 5;

        [SerializeField]
        private float patrolWaitMin = 2f;

        [SerializeField]
        private float patrolWaitMax = 5f;

        [Header("Chase")]
        [SerializeField]
        private float chaseUpdateInterval = 0.2f;

        [SerializeField]
        private float targetLostTimeout = 6f;

        [Header("Attack")]
        [SerializeField]
        private float attackRange = 1.8f;

        [SerializeField]
        private float attackCooldown = 2f;

        [Header("Animator")]
        [SerializeField]
        private string stateParameterName = "State";

        private int stateHash;

        // --------------------------------------------------
        // State
        // --------------------------------------------------

        public NetworkVariable<MonsterState> CurrentState =
            new NetworkVariable<MonsterState>(
                MonsterState.Idle,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        private readonly MonsterStateMachine stateMachine =
            new MonsterStateMachine();


        // --------------------------------------------------
        // Patrol
        // --------------------------------------------------

        private Vector3 patrolDestination;
        private bool hasPatrolDestination;

        private float patrolWaitTimer;


        // --------------------------------------------------
        // Chase
        // --------------------------------------------------

        private ulong targetPlayerId;
        private Transform targetPlayer;

        private float chaseUpdateTimer;
        private float targetLostTimer;

        private bool hasTarget;


        // --------------------------------------------------
        // Attack
        // --------------------------------------------------

        private float attackCooldownTimer;


        // --------------------------------------------------
        // Unity
        // --------------------------------------------------

        private void Awake()
        {
            stateHash = Animator.StringToHash(stateParameterName);

            if (controller == null)
                controller = GetComponent<MonsterController>();

            if (detection == null)
                detection = GetComponent<MonsterDetection>();

            if (animator == null)
                animator = GetComponent<Animator>();
        }


        // --------------------------------------------------
        // Network
        // --------------------------------------------------

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CurrentState.OnValueChanged += OnStateChanged;

            // 현재 상태 즉시 반영
            ApplyState(CurrentState.Value);

            // Server에서만 AI 실행
            if (IsServer)
            {
                EnterState(CurrentState.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            CurrentState.OnValueChanged -= OnStateChanged;

            base.OnNetworkDespawn();
        }


        // --------------------------------------------------
        // Update
        // --------------------------------------------------

        private void Update()
        {
            if (!IsServer)
                return;

            switch (CurrentState.Value)
            {
                case MonsterState.Idle:
                    UpdateIdle();
                    break;

                case MonsterState.Patrol:
                    UpdatePatrol();
                    break;

                case MonsterState.Chase:
                    UpdateChase();
                    break;

                case MonsterState.Attack:
                    UpdateAttack();
                    break;

                case MonsterState.Kill:
                    UpdateKill();
                    break;
            }
        }


        // ==================================================
        // State
        // ==================================================

        private void OnStateChanged(
            MonsterState previousState,
            MonsterState newState)
        {
            ApplyState(newState);

            // State 판단은 Server에서만
            if (!IsServer)
                return;

            EnterState(newState);
        }

        private void ApplyState(MonsterState state)
        {
            if (animator == null)
                return;

            animator.SetInteger(
                stateHash,
                (int)state
            );
        }

        private void EnterState(MonsterState state)
        {
            switch (state)
            {
                case MonsterState.Idle:
                    EnterIdle();
                    break;

                case MonsterState.Patrol:
                    EnterPatrol();
                    break;

                case MonsterState.Chase:
                    EnterChase();
                    break;

                case MonsterState.Attack:
                    EnterAttack();
                    break;

                case MonsterState.Kill:
                    EnterKill();
                    break;
            }
        }

        private void TryChangeState(MonsterState nextState)
        {
            if (!IsServer)
                return;

            if (CurrentState.Value == nextState)
                return;

            if (!stateMachine.CanTransition(
                    CurrentState.Value,
                    nextState))
            {
                return;
            }

            CurrentState.Value = nextState;
        }


        // ==================================================
        // Idle
        // ==================================================

        private void EnterIdle()
        {
            controller.Stop();
        }

        private void UpdateIdle()
        {
            // 대기 중에도 플레이어 감지는 가능
            if (detection.TryGetBestDetectedPlayer(
                    out ulong playerId,
                    out Transform player))
            {
                SetTarget(playerId, player);

                TryChangeState(MonsterState.Chase);
                return;
            }

            patrolWaitTimer -= Time.deltaTime;

            if (patrolWaitTimer > 0f)
                return;

            // 대기 종료
            TryChangeState(MonsterState.Patrol);
        }


        // ==================================================
        // Patrol
        // ==================================================

        private void EnterPatrol()
        {
            hasPatrolDestination = false;

            controller.Resume();
        }

        private void UpdatePatrol()
        {
            // 플레이어 감지
            if (detection.TryGetBestDetectedPlayer(
                    out ulong playerId,
                    out Transform player))
            {
                SetTarget(playerId, player);

                TryChangeState(MonsterState.Chase);
                return;
            }

            // 목적지 탐색
            if (!hasPatrolDestination)
            {
                if (!TryGetPatrolPosition(
                        out patrolDestination))
                {
                    HandlePatrolPositionFailed();
                    return;
                }

                hasPatrolDestination = true;

                controller.SetDestination(
                    patrolDestination
                );

                return;
            }

            // 이동 중
            if (!controller.HasReachedDestination())
                return;

            // 목적지 도착
            controller.Stop();

            hasPatrolDestination = false;

            // 대기 시간 설정
            patrolWaitTimer = Random.Range(
                patrolWaitMin,
                patrolWaitMax
            );

            // Patrol → Idle
            TryChangeState(MonsterState.Idle);
        }

        private bool TryGetPatrolPosition(
            out Vector3 position)
        {
            for (int i = 0; i < patrolPositionAttempts; i++)
            {
                Vector2 random =
                    Random.insideUnitCircle * patrolRadius;

                Vector3 candidate =
                    transform.position +
                    new Vector3(
                        random.x,
                        0f,
                        random.y
                    );

                if (UnityEngine.AI.NavMesh.SamplePosition(
                        candidate,
                        out UnityEngine.AI.NavMeshHit hit,
                        navMeshSampleRadius,
                        UnityEngine.AI.NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            position = transform.position;
            return false;
        }

        private void HandlePatrolPositionFailed()
        {
            controller.Stop();

            patrolWaitTimer = Random.Range(
                patrolWaitMin,
                patrolWaitMax
            );

            hasPatrolDestination = false;

            TryChangeState(MonsterState.Idle);
        }


        // ==================================================
        // Chase
        // ==================================================

        private void EnterChase()
        {
            if (!hasTarget ||
                targetPlayer == null)
            {
                if (!TryGetNextTarget())
                {
                    ClearTarget();
                    TryChangeState(MonsterState.Patrol);
                    return;
                }
            }

            chaseUpdateTimer = 0f;
            targetLostTimer = 0f;

            controller.Resume();
        }

        private void UpdateChase()
        {
            if (!ValidateTarget())
            {
                if (!TryGetNextTarget())
                {
                    ClearTarget();
                    TryChangeState(MonsterState.Patrol);
                }

                return;
            }

            // 공격 사거리 진입
            float distance =
                Vector3.Distance(
                    transform.position,
                    targetPlayer.position
                );

            if (distance <= attackRange)
            {
                TryChangeState(MonsterState.Attack);
                return;
            }

            // 목적지 갱신
            chaseUpdateTimer -= Time.deltaTime;

            if (chaseUpdateTimer <= 0f)
            {
                chaseUpdateTimer =
                    chaseUpdateInterval;

                controller.SetDestination(
                    targetPlayer.position
                );
            }

            // 타겟 감지 여부 확인
            UpdateTargetLostTimer();
        }

        private void UpdateTargetLostTimer()
        {
            if (targetPlayer == null)
                return;

            if (detection.CanCurrentlyDetect(
                    targetPlayer))
            {
                targetLostTimer = 0f;
                return;
            }

            targetLostTimer += Time.deltaTime;

            if (targetLostTimer < targetLostTimeout)
                return;

            // 타겟을 일정 시간 완전히 놓침
            ClearTarget();

            TryChangeState(MonsterState.Patrol);
        }

        private bool ValidateTarget()
        {
            if (!hasTarget)
                return false;

            if (targetPlayer == null)
                return false;

            NetworkObject networkObject =
                targetPlayer.GetComponent<NetworkObject>();

            if (networkObject == null)
                return false;

            PlayerController player =
                targetPlayer.GetComponent<PlayerController>();

            if (player == null)
                return false;

            // Down / Dead 플레이어는 타겟에서 제외
            if (player.IsDown ||
                player.IsDead)
            {
                return false;
            }

            return true;
        }

        private bool TryGetNextTarget()
        {
            if (!detection.TryGetBestDetectedPlayer(
                    out ulong playerId,
                    out Transform player))
            {
                return false;
            }

            SetTarget(
                playerId,
                player
            );

            return true;
        }

        private void SetTarget(
            ulong playerId,
            Transform player)
        {
            targetPlayerId = playerId;
            targetPlayer = player;

            hasTarget = true;

            targetLostTimer = 0f;
        }

        private void ClearTarget()
        {
            targetPlayerId = 0;
            targetPlayer = null;

            hasTarget = false;

            targetLostTimer = 0f;
        }


        // ==================================================
        // Attack
        // ==================================================

        private void EnterAttack()
        {
            controller.Stop();

            attackCooldownTimer = 0f;

            FaceTarget();
        }

        private void UpdateAttack()
        {
            if (!ValidateTarget())
            {
                if (TryGetNextTarget())
                {
                    TryChangeState(MonsterState.Chase);
                }
                else
                {
                    ClearTarget();
                    TryChangeState(MonsterState.Patrol);
                }

                return;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    targetPlayer.position
                );

            // 공격 사거리 이탈
            if (distance > attackRange)
            {
                TryChangeState(MonsterState.Chase);
                return;
            }

            FaceTarget();

            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer > 0f)
                return;

            attackCooldownTimer =
                attackCooldown;

            PlayAttackAnimation();
        }

        private void FaceTarget()
        {
            if (targetPlayer == null)
                return;

            Vector3 direction =
                targetPlayer.position -
                transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return;

            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        private void PlayAttackAnimation()
        {
            // 현재 Animator 구조에서는
            // State == Attack가 되면 Attack Animation이 재생된다.

            if (animator == null)
                return;

            animator.SetInteger(
                stateHash,
                (int)MonsterState.Attack
            );
        }


        // ==================================================
        // Animation Event
        // ==================================================

        /// <summary>
        /// Attack Animation의 공격 명중 프레임에서 호출
        /// </summary>
        public void OnMonsterAttackHitEvent()
        {
            if (!IsServer)
                return;

            ProcessAttackHit();
        }

        private void ProcessAttackHit()
        {
            if (!ValidateTarget())
            {
                return;
            }


            float distance =
                Vector3.Distance(
                    transform.position,
                    targetPlayer.position
                );

            // Animation Event가 발생했더라도
            // 서버에서 실제 공격 사거리 재검증
            if (distance > attackRange)
            {
                return;
            }

            PlayerController player =
                targetPlayer.GetComponent<PlayerController>();

            if (player == null)
            {
                return;
            }

            // 이미 Down / Dead라면 공격하지 않음
            if (player.IsDown || player.IsDead)
            {
                return;
            }


            // Player Down
            player.Down();


            // Down 성공 확인

            if (!player.IsDown)
            {
                return;
            }

            // Attack → Kill
            TryChangeState(MonsterState.Kill);
        }


        // ==================================================
        // Kill
        // ==================================================

        private void EnterKill()
        {
            controller.Stop();

            FaceTarget();
        }

        private void UpdateKill()
        {
            // Kill은 Animation Event를 기다린다.
            //
            // Kill Animation 마지막 프레임에서
            // OnKillFinished()가 호출된다.
        }

        /// <summary>
        /// Kill Animation 마지막 프레임에서 호출
        /// </summary>
        public void OnKillFinished()
        {
            if (!IsServer)
                return;

            ProcessKillFinished();
        }

        private void ProcessKillFinished()
        {
            // 현재 타겟은 이미 Down 상태이므로
            // 새로운 타겟을 탐색한다.
            ClearTarget();

            if (TryGetNextTarget())
            {
                // 다른 플레이어가 감지되고 있음
                TryChangeState(MonsterState.Chase);
                return;
            }

            // 새로운 타겟 없음
            TryChangeState(MonsterState.Patrol);
        }
    }
}