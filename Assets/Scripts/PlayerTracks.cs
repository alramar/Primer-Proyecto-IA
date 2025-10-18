using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class PlayerTracks : MonoBehaviour
{
    [SerializeField]
    GameObject nodePrefab;
    GameObject latestPlayerNode;
    public Queue<GameObject> playerTracksGONodes;
    [SerializeField]
    public GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerTracksGONodes = new();
        latestPlayerNode = Instantiate(nodePrefab, player.transform.position + new Vector3(0,transform.position.y,0), Quaternion.identity, transform);
        playerTracksGONodes.Enqueue(latestPlayerNode);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Vector3.Distance(latestPlayerNode.transform.position, player.transform.position) >= 2f)
        {
            latestPlayerNode = Instantiate(nodePrefab, player.transform.position + new Vector3(0,transform.position.y,0), Quaternion.identity, transform);
            playerTracksGONodes.Enqueue(latestPlayerNode);
            if (playerTracksGONodes.Count >= 5)
            {
                GameObject aux = playerTracksGONodes.Dequeue();
                Destroy(aux);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (playerTracksGONodes == null || playerTracksGONodes.Count < 1) return;

        for (int i = 0; i < playerTracksGONodes.Count - 1; i++)
        {
            GameObject current = playerTracksGONodes.ElementAt(i);
            GameObject next = playerTracksGONodes.ElementAt(i + 1);
            Gizmos.DrawLine(current.transform.position, next.transform.position);
        }
        Gizmos.DrawLine(playerTracksGONodes.Last().transform.position, player.transform.position);
    }
}
