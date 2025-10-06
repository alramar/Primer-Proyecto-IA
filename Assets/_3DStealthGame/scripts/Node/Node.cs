using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [Header("Node Settings")]
    private float distanceToPlayer;
    public Transform player;
    public bool isObject = false;
    public List<Node> neighbors = new List<Node>();
    private float heuristics;
    public float cost;
    private ParticleSystem ps;
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate distance to player
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        cost = distanceToPlayer + heuristics;
    }
}
