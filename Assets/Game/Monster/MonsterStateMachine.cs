using UnityEngine;

namespace FourPlayerHorror.Monster
{
    public class MonsterStateMachine
    {
        public bool CanTransition(
            MonsterState currentState,
            MonsterState nextState)
        {
            switch (currentState)
            {
                case MonsterState.Idle:
                    return nextState == MonsterState.Patrol ||
                           nextState == MonsterState.Chase;

                case MonsterState.Patrol:
                    return nextState == MonsterState.Chase ||
                           nextState == MonsterState.Idle;

                case MonsterState.Chase:
                    return nextState == MonsterState.Attack ||
                           nextState == MonsterState.Patrol;

                case MonsterState.Attack:
                    return nextState == MonsterState.Chase ||
                           nextState == MonsterState.Kill;

                case MonsterState.Kill:
                    return nextState == MonsterState.Chase ||
                           nextState == MonsterState.Patrol;

                default:
                    return false;
            }
        }
    }
}