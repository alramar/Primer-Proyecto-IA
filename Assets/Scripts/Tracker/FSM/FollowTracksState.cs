using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using StealthGame;
using Assets.Scripts.Algorithms;
using System;

/// <summary>
/// Estado que hace que el enemigo siga los rastros del jugador.
/// Si los nodos del rastro desaparecen o terminan, vuelve a patrullar.
/// </summary>
public class FollowTracksState : IEnemyState
{
    private List<GameObject> trackPath;
    GameObject currentPathObject;
    private bool initialized;
    private Queue<GameObject> trackQueue = new();
    private GameObject currentTrackTarget;
    private TrackerEnemy enemy;
    private int currentPathIndex;
    private Vector3 future;
    private Vector3 targetPoint;
    private Vector3 normalPoint;

    public void Enter(TrackerEnemy enemy)
    {
        initialized = false;
        this.enemy = enemy;
        // Copiamos los nodos actuales del rastro del jugador
        trackPath = enemy.playerTracks.playerTracksGONodes?.ToList();

        if (trackPath == null || trackPath.Count < 2)
        {
            enemy.stateMachine.ChangeState(new PatrolState(), enemy);
            enemy.target = enemy.playerTracks.player.GetComponent<PlayerMovement>();
            return;
        }

        currentPathIndex = 0;
        currentPathObject = trackPath[currentPathIndex];
        initialized = true;
        enemy.isFollowingTracks = true;

        Debug.Log($"[FSM] Entra al estado FollowTracksState. Nodos en el rastro: {trackPath.Count}");
    }

    public void Update(TrackerEnemy enemy)
    {
        if(enemy.target != null)
        {
            enemy.stateMachine.ChangeState(new PursuitState(), enemy);
            return;
        }
        // Si por algún motivo no hay datos válidos, volvemos a patrullar
        if (!initialized || trackPath == null || trackPath.Count < 2)
        {
            ResetToPatrol(enemy, "Rastro inválido o sin inicializar.");
            return;
        }

        // Validar que los nodos actuales y siguientes existen (no destruidos)
        if (currentPathObject == null ||
            (currentPathIndex + 1 < trackPath.Count && trackPath[currentPathIndex + 1] == null))
        {
            ResetToPatrol(enemy, "Nodo actual o siguiente destruido.");
            return;
        }

        FollowPath(enemy);
    }

    private void FollowPath(TrackerEnemy enemy)
    {
        // Actualiza el trackPath por si ha cambiado
        currentPathIndex = trackPath.IndexOf(currentPathObject);

        if (trackPath == null || trackPath.Count < 2) return;
        targetPoint = Vector3.zero;
        float lookAhead = Math.Clamp(enemy.rb.linearVelocity.magnitude, 0.5f, 1f);
        future = enemy.transform.position + enemy.rb.linearVelocity.normalized * lookAhead;
        Vector3 a = trackPath[currentPathIndex].transform.position;
        Vector3 b = trackPath[currentPathIndex + 1].transform.position;
        float t;
        normalPoint = enemy.GetNormalPoint(future, a, b, out t);

        if (t >= 1f)
        {
            if (currentPathIndex < trackPath.Count - 2)
            {
                currentPathIndex++;
                currentPathObject = trackPath[currentPathIndex];

            }
            else
            {
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

    private void ResetToPatrol(TrackerEnemy enemy, string reason)
    {
        enemy.isFollowingTracks = false;
        Debug.Log($"[FSM] FollowTracksState - Cancelando seguimiento: {reason}");

        // Intento opcional de reengancharse si hay nuevos nodos
        // var newTracks = enemy.playerTracks.playerTracksGONodes?.ToList();
        // if (newTracks != null && newTracks.Count > 1)
        // {
        //     Debug.Log($"[FollowTracksState] Rastro actualizado, reintentando seguimiento ({reason})");
        //     trackPath = newTracks;
        //     currentTrackPathIndex = 0;
        //     initialized = true;
        //     return;
        // }

        enemy.stateMachine.ChangeState(new PatrolState(), enemy);
    }

    public void Exit(TrackerEnemy enemy)
    {
        enemy.isFollowingTracks = false;
        trackPath = null;
        initialized = false;
        Debug.Log("[FSM] Sale del estado FollowTracksState.");
    }

    // public void FollowPlayerTrsacks()
    // {
    //     // Si no hay rastros, terminamos
    //     if (enemy.playerTracks == null || enemy.playerTracks.playerTracksGONodes.Count == 0)
    //     {
    //         enemy.isFollowingTracks = false;
    //         return;
    //     }

    //     // Si la cola está vacía o desactualizada, la refrescamos
    //     if (trackQueue.Count == 0 || trackQueue.LastOrDefault() != enemy.playerTracks.playerTracksGONodes.LastOrDefault())
    //     {
    //         trackQueue = new Queue<GameObject>(enemy.playerTracks.playerTracksGONodes);
    //     }

    //     // Si no hay target actual, tomar el primero
    //     if (currentTrackTarget == null && trackQueue.Count > 0)
    //     {
    //         currentTrackTarget = trackQueue.Peek();
    //     }

    //     if (currentTrackTarget == null) return;

    //     Vector3 targetPos = currentTrackTarget.transform.position;
    //     enemy.Seek(targetPos); // usamos tu versión original de Seek()

    //     float dist = Vector3.Distance(enemy.transform.position, targetPos);
    //     if (dist <= 1.2f)
    //     {
    //         // Pasar al siguiente nodo
    //         trackQueue.Dequeue();
    //         currentTrackTarget = trackQueue.Count > 0 ? trackQueue.Peek() : null;
    //     }

    //     // Si se acaban los nodos
    //     if (trackQueue.Count == 0)
    //         enemy.isFollowingTracks = false;
    // }
}
