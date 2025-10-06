using UnityEngine;

public class BatEnemy : MonoBehaviour
{
    private GameEnding gameEnding;
    private GameObject player;
    private PlayerController playerController;

    public enum Estado { Patrulla, Persecucion }
    private Estado estadoActual = Estado.Patrulla;

    [Header("Movimiento")]
    public float speed = 1f;
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    [Header("Detección")]
    private SphereCollider soundCollider;
    public float runDetectionRadius = 1.5f;
    public float walkDetectionRadius = 0.6f;

    private Collider playerCollider;
    private Vector3 ultimaPosicionConocida;
    private bool playerDetectado = false;

    void Start()
    {
        gameEnding = GameObject.FindFirstObjectByType<GameEnding>();

        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerCollider = player.GetComponent<Collider>();

        soundCollider = GameObject.FindGameObjectWithTag("SoundCollider").GetComponent<SphereCollider>();
        soundCollider.radius = runDetectionRadius;
    }

    void Update()
    {
        if (playerController.isWalking) soundCollider.radius = walkDetectionRadius;
        else soundCollider.radius = runDetectionRadius;

        switch (estadoActual)
        {
            case Estado.Patrulla:
                Patrullar();
                break;

            case Estado.Persecucion:
                Perseguir();
                break;
        }
    }

    void Patrullar()
    {
        if (playerDetectado)
        {
            CambiarEstado(Estado.Persecucion);
            return;
        }

        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        MoverseHacia(targetWaypoint.position);

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.2f)
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    void Perseguir()
    {
        if (playerDetectado)
        {
            ultimaPosicionConocida = player.transform.position;
            MoverseHacia(ultimaPosicionConocida);
        }
        else
        {
            MoverseHacia(ultimaPosicionConocida);

            if (Vector3.Distance(transform.position, ultimaPosicionConocida) < 0.2f)
                CambiarEstado(Estado.Patrulla);
        }
    }

    void MoverseHacia(Vector3 destino)
    {
        Vector3 direccion = (destino - transform.position).normalized;
        transform.position += direccion * speed * Time.deltaTime;
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * 5f);
        }
    }

    void CambiarEstado(Estado nuevoEstado)
    {
        estadoActual = nuevoEstado;
    }

    void OnTriggerStay(Collider other)
    {
        if (other == playerCollider)
            ultimaPosicionConocida = player.transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == playerCollider)
        {
            Debug.Log("Jugador detectado por murciélago");
            playerDetectado = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == playerCollider)
        {
            Debug.Log("Murciélago perdió al jugador");
            playerDetectado = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == playerCollider)
        {
            Debug.Log("Murciélago atrapó al jugador");
            if (gameEnding != null)
                gameEnding.CaughtPlayer();
        }
    }

    void OnDrawGizmos()
    {
        if (soundCollider != null)
        {
            Gizmos.color = Color.red;

            // Radio ajustado por la escala global
            float worldRadius = soundCollider.radius * Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.y,
                transform.lossyScale.z
            );

            // Dibujar el gizmo exactamente donde está el collider
            Gizmos.DrawWireSphere(soundCollider.transform.position, worldRadius);
        }
    }

}
