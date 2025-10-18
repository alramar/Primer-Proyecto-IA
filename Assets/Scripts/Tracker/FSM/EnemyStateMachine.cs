using UnityEngine;

public class EnemyStateMachine
{
    public IEnemyState currentState;

    public void Initialize(IEnemyState startingState, TrackerEnemy enemy)
    {
        currentState = startingState;
        currentState.Enter(enemy);
    }

    public void ChangeState(IEnemyState newState, TrackerEnemy enemy)
    {
        if (currentState == newState) return;

        currentState?.Exit(enemy);
        currentState = newState;
        currentState.Enter(enemy);
        Debug.Log($"[FSM] Cambiando a estado: {newState.GetType().Name}");
    }

    public void Update(TrackerEnemy enemy)
    {
        currentState?.Update(enemy);
    }
}