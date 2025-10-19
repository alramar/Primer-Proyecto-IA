using System;
using System.Collections.Generic;
using Assets.Scripts.Algorithms;
using StealthGame;
using UnityEngine;

public class PursuitState : IEnemyState
{
    private TrackerEnemy enemy;
    private int currentPathIndex;
    private List<GenericNode> path;
    private Vector3 future;
    private Vector3 targetPoint;
    private Vector3 normalPoint;
    private bool objectiveReached;
    public void Enter(TrackerEnemy enemy)
    {
        path = new();
        objectiveReached = false;
        this.enemy = enemy;
        enemy.pursuitTimer = 0f;
        currentPathIndex = 0;
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

        Pursuit();
        
        enemy.pursuitTimer += Time.deltaTime;
        if (enemy.pursuitTimer >= enemy.pursuitTime)
        {
            enemy.stateMachine.ChangeState(new FollowTracksState(), enemy);
        }
        
    }

    public void Pursuit()
    {
        Vector3 pursuitTargetPos;

        if (enemy.target != null)
        {
            // Se ve al jugador
            enemy.lastKnownTargetPosition = enemy.target.transform.position;
            pursuitTargetPos = enemy.lastKnownTargetPosition;
        }
        else
        {
            // No se ve, ir hacia la última posición conocida
            pursuitTargetPos = enemy.lastKnownTargetPosition;
        }
        // enemy.Seek(pursuitTargetPos); // usa el Seek ya existente

        if(!objectiveReached) path = enemy.graph.TryPathing(enemy.transform, enemy.target.transform);

        if (path == null || path.Count <= 2)
        {
            path.Clear();
            currentPathIndex = 0;
            enemy.Seek(pursuitTargetPos); // usa el Seek ya existente
        }
        else
        {
            Debug.Log("AVER " + path.Count + " / " + currentPathIndex);
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
                    currentPathIndex = 0;
                }
            }
            Vector3 direction = (b - a).normalized;
            targetPoint = normalPoint + direction * lookAhead;
            KeepOnPath();

        }
    }
    public void KeepOnPath()
    {
        // Solo seguir si estamos fuera del radio
        float dist = Vector3.Distance(future, normalPoint);
        if (dist > enemy.pathRadius)
            enemy.Seek(targetPoint);

    }
}