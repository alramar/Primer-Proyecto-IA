using UnityEngine;

public class PursuitState : IEnemyState
{
    public void Enter(TrackerEnemy enemy)
    {
        enemy.pursuitTimer = 0f;
        if (enemy.target != null)
            enemy.lastKnownTargetPosition = enemy.target.transform.position;
        Debug.Log("[FSM] Entrando en estado Pursuit");
    }

    public void Exit(TrackerEnemy enemy)
    {
        Debug.Log("[FSM] Saliendo de Pursuit");
        enemy.pursuitTimer = 0f;
        enemy.target = null;
    }

    public void Update(TrackerEnemy enemy)
    {
        enemy.Pursuit();
        if (enemy.target != null)
        {
            enemy.pursuitTimer = 0f; // lo sigue viendo
        }
        else
        {
            enemy.pursuitTimer += Time.deltaTime;
            if (enemy.pursuitTimer >= enemy.pursuitTime)
            {
                enemy.stateMachine.ChangeState(new PatrolState(), enemy);
            }
        }
    }
}