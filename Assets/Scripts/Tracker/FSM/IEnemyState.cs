using UnityEngine;

public interface IEnemyState
{

    void Enter(TrackerEnemy enemy);
    void Update(TrackerEnemy enemy);
    void Exit(TrackerEnemy enemy);

}