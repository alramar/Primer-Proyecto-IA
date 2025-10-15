using UnityEngine;

public class EnemyGhostVisor : MonoBehaviour
{
    public GameObject player;
    public bool seePlayer;

    void Start()
    {
        seePlayer = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player) { seePlayer = true; }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player) { seePlayer = false; }
    }
}
