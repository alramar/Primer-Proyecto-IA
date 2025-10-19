using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Murcielago : MonoBehaviour
{
    private MurcielagoFSM fsm;
    private GameEnding gameEnding;
    private PlayerController playerController;
    private SphereCollider detectorSonido;

    public List<Transform> puntosPatrulla = new List<Transform>();
    private int indicePuntoPatrullaActual = 0;

    [Header("Stats de Murciélago")]
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private float radioDeteccionCaminar = 0.6f;
    [SerializeField] private float radioDeteccionCorrer = 1.5f;



    void Start()
    {
        fsm = new MurcielagoFSM(this);
        gameEnding = FindFirstObjectByType<GameEnding>();
        playerController = FindFirstObjectByType<PlayerController>();
        detectorSonido = GameObject.FindWithTag("SoundCollider")?.GetComponent<SphereCollider>();

        detectorSonido.radius = radioDeteccionCorrer;
    }

    void Update()
    {
        if (!playerController.isMoving) detectorSonido.radius = 0f;
        else detectorSonido.radius = playerController.isWalking ? radioDeteccionCaminar : radioDeteccionCorrer;

        
    }

    void FixedUpdate()
    {
        fsm.ActualizarEstado();
    }

    private void OnDrawGizmos()
    {
        if (puntosPatrulla == null || puntosPatrulla.Count == 0) return;

        Gizmos.color = Color.yellow;

        // 🔹 Dibujar conexiones entre puntos
        for (int i = 0; i < puntosPatrulla.Count; i++)
        {
            var nodo = puntosPatrulla[i];
            if (nodo == null) continue;

            // Redibujar el gizmo del nodo (más grande / diferente)
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(nodo.transform.position, 0.15f);

            // Línea al siguiente punto
            var siguiente = puntosPatrulla[(i + 1) % puntosPatrulla.Count];
            if (siguiente != null)
                Gizmos.DrawLine(nodo.transform.position, siguiente.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (puntosPatrulla == null) return;

        #if UNITY_EDITOR
        for (int i = 0; i < puntosPatrulla.Count; i++)
        {
            var nodo = puntosPatrulla[i];
            if (nodo == null) continue;

            Handles.Label(nodo.transform.position + Vector3.up * 0.25f, $"PuntoPatrulla {i + 1}");
        }
        #endif
    }
}
