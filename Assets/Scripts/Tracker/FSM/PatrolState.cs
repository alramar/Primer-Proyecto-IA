using UnityEngine;

public class PatrolState : IEnemyState
{
    public void Enter(TrackerEnemy enemy)
    {
        enemy.objectiveReached = true;
        Debug.Log("[FSM] Entrando en estado Patrol");

    }

    public void Exit(TrackerEnemy enemy)
    {
        Debug.Log("[FSM] Saliendo de Patrol");

    }

    public void Update(TrackerEnemy enemy)
    {
        enemy.Patrol();

        if (enemy.target != null)
        {
            enemy.stateMachine.ChangeState(new PursuitState(), enemy);
        }
        else if (enemy.isFollowingTracks && enemy.playerTracks.playerTracksGONodes.Count < 0)
        {
            enemy.stateMachine.ChangeState(new FollowTracksState(), enemy);
        }
    }
}