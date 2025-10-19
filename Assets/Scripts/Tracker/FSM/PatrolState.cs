using System;
using System.Collections.Generic;
using Assets.Scripts.Algorithms;
using UnityEngine;

public class PatrolState : IEnemyState
{
    private TrackerEnemy enemy;
    private bool objectiveReached;
    private List<GenericNode> path;
    private int currentPathIndex;
    private GenericNode nextPathObjective;
    private GenericNode lastPathNodeVisited;
    private GenericNode currentPathObjective;
    private List<GenericNode> visitedNodes;
    private bool isFar;
    private Vector3 future;
    private Vector3 targetPoint;
    private Vector3 normalPoint;


    public void Enter(TrackerEnemy enemy)
    {
        this.enemy = enemy;
        objectiveReached = true;
        path = new();
        currentPathIndex = 0;
        visitedNodes = new();
        isFar = true;
        Debug.Log("[FSM] Entrando en estado Patrol");

    }

    public void Exit(TrackerEnemy enemy)
    {
        Debug.Log("[FSM] Saliendo de Patrol");

    }

    public void Update(TrackerEnemy enemy)
    {
        Patrol();

        if (enemy.target != null)
        {
            enemy.stateMachine.ChangeState(new PursuitState(), enemy);
        }
        else if (enemy.isFollowingTracks && enemy.playerTracks.playerTracksGONodes.Count < 0)
        {
            enemy.stateMachine.ChangeState(new FollowTracksState(), enemy);
        }
    }

    public void Patrol()
    {
        int tries = 0;
        if (objectiveReached)
        {
            path = new();
            currentPathIndex = 0;
            while (path.Count == 0 && tries < 100)
            {
                if (isFar)
                {
                    nextPathObjective = enemy.graph.GetFurthestNodeInRadius(lastPathNodeVisited != null ? lastPathNodeVisited.transform : enemy.transform, enemy.farAwayPatrolRadius, visitedNodes);
                }
                else
                {
                    nextPathObjective = enemy.graph.GetFurthestNodeInRadius(lastPathNodeVisited != null ? lastPathNodeVisited.transform : enemy.transform, enemy.nearbyPatrolRadius, visitedNodes);
                }
                if (currentPathObjective)
                {
                    visitedNodes.Add(currentPathObjective);
                    lastPathNodeVisited = currentPathObjective;

                }
                currentPathObjective = nextPathObjective;
                objectiveReached = false;
                path = enemy.graph.TryPathing(enemy.transform, currentPathObjective);
                //Debug.Log(path.Count);
                tries++;
            }
        }
        else if (tries < 100)
        {
            FollowAStarPath();
        }
        else {
            Debug.Log("ASPAICO QUE LOOPEAMO");
            objectiveReached = false;
        }


    }

    public void FollowAStarPath()
    {
        if (path == null || path.Count < 2) return;
        targetPoint = Vector3.zero;
        float lookAhead = Math.Clamp(enemy.rb.linearVelocity.magnitude, 0.5f, 1f);
        future = enemy.transform.position + enemy.rb.linearVelocity.normalized * lookAhead;
        Vector3 a = path[currentPathIndex].transform.position;
        Vector3 b = path[currentPathIndex + 1].transform.position;
        float t;
        normalPoint = enemy.GetNormalPoint(future, a, b, out t);

        if (t >= 1f)
        {
            if (currentPathIndex < path.Count - 2)
            {
                currentPathIndex++;
            }
            else
            {
                objectiveReached = true;
                isFar = !isFar;
                currentPathIndex = 0;
            }
        }
        Vector3 direction = (b - a).normalized;
        targetPoint = normalPoint + direction * lookAhead;
        KeepOnPath();

    }

    public void KeepOnPath()
    {
        // Solo seguir si estamos fuera del radio
        float dist = Vector3.Distance(future, normalPoint);
        if (dist > enemy.pathRadius)
            enemy.Seek(targetPoint);

    }
}