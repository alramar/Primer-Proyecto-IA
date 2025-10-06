using System.Collections.Generic;
using UnityEngine;

// Espectro inherits from Enemy
public class Espectro : Enemy
{
    [Header("Espectro Specific Settings")]
    public List<Object> objects_to_possess;
    public float possession_range = 5.0f;
    public Transform player;
    public new void Start()
    {

    }

    // Update is called once per frame
    public new void Update()
    {

    }

    public void Possess()
    {
        // Uses A* Pathfinding to move to the nearest object in objects_to_possess within possession_range
        // or goes close to the object nearest to the player jumping between objects


    }
}
