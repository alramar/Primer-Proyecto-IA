using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Estado que hace que el enemigo siga los rastros del jugador.
/// Si los nodos del rastro desaparecen o terminan, vuelve a patrullar.
/// </summary>
public class FollowTracksState : IEnemyState
{
    private List<GameObject> trackPath;
    private int currentTrackPathIndex;
    private bool initialized;

    public void Enter(TrackerEnemy enemy)
    {
        initialized = false;

        // Copiamos los nodos actuales del rastro del jugador
        trackPath = enemy.playerTracks.playerTracksGONodes?.ToList();

        if (trackPath == null || trackPath.Count < 2)
        {
            ResetToPatrol(enemy, "No hay suficientes nodos para seguir el rastro.");
            return;
        }

        currentTrackPathIndex = 0;
        initialized = true;
        enemy.isFollowingTracks = true;

        Debug.Log($"[FollowTracksState] Entra al estado. Nodos en el rastro: {trackPath.Count}");
    }

    public void Update(TrackerEnemy enemy)
    {
        // Si por algún motivo no hay datos válidos, volvemos a patrullar
        if (!initialized || trackPath == null || trackPath.Count < 2)
        {
            ResetToPatrol(enemy, "Rastro inválido o sin inicializar.");
            return;
        }

        // Validar que los nodos actuales y siguientes existen (no destruidos)
        if (trackPath[currentTrackPathIndex] == null ||
            (currentTrackPathIndex + 1 < trackPath.Count && trackPath[currentTrackPathIndex + 1] == null))
        {
            ResetToPatrol(enemy, "Nodo actual o siguiente destruido.");
            return;
        }

        FollowPath(enemy);
        enemy.Move();
    }

    private void FollowPath(TrackerEnemy enemy)
    {
        float lookAhead = Mathf.Clamp(enemy.rb.linearVelocity.magnitude, 0.5f, 1f);
        Vector3 future = enemy.transform.position + enemy.rb.linearVelocity.normalized * lookAhead;

        GameObject aObj = trackPath[currentTrackPathIndex];
        GameObject bObj = trackPath[Mathf.Min(currentTrackPathIndex + 1, trackPath.Count - 1)];

        if (aObj == null || bObj == null)
        {
            ResetToPatrol(enemy, "Nodo destruido durante el seguimiento.");
            return;
        }

        Vector3 a = aObj.transform.position;
        Vector3 b = bObj.transform.position;

        float t;
        Vector3 normalPoint = enemy.GetNormalPoint(future, a, b, out t);

        // Si el punto futuro sobrepasa el tramo, avanzamos al siguiente nodo
        if (t >= 1f)
        {
            if (currentTrackPathIndex < trackPath.Count - 2)
            {
                currentTrackPathIndex++;
            }
            else
            {
                ResetToPatrol(enemy, "Final del rastro alcanzado.");
                return;
            }
        }

        Vector3 direction = (b - a).normalized;
        Vector3 targetPoint = normalPoint + direction * lookAhead;

        float dist = Vector3.Distance(future, normalPoint);
        if (dist > enemy.pathRadius)
            enemy.Seek(targetPoint);
    }

    private void ResetToPatrol(TrackerEnemy enemy, string reason)
    {
        enemy.isFollowingTracks = false;
        Debug.Log($"[FollowTracksState] Cancelando seguimiento: {reason}");

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
        Debug.Log("[FollowTracksState] Sale del estado.");
    }
}
