using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Murcielago : MonoBehaviour
{
    private MurcielagoFSM fsm;
    private GameEnding gameEnding;
    private GameObject player;
    private PlayerController playerController;
    public SphereCollider detectorSonido;

    [Header("Waypoints del Murciélago")]
    public List<NodoGrafoMurcielago> puntosPatrulla = new List<NodoGrafoMurcielago>();

    [Header("Stats de Murciélago")]
    public float velocidad = 2f;
    public float radioDeteccionCaminar = 0.6f;
    public float radioDeteccionCorrer = 1.5f;
    public float distanciaLlegadaNodo = 0.2f;

    void Start()
    {
        fsm = new MurcielagoFSM(this);
        gameEnding = FindFirstObjectByType<GameEnding>();
        player = FindFirstObjectByType<PlayerController>().gameObject;
        playerController = player.GetComponent<PlayerController>();
        detectorSonido = GameObject.FindWithTag("SoundCollider")?.GetComponent<SphereCollider>();
        detectorSonido.GetComponent<DetectorSonido>().playerCollider = player.GetComponent<Collider>();
        detectorSonido.GetComponent<DetectorSonido>().fsm = fsm;


        if (detectorSonido != null) detectorSonido.radius = radioDeteccionCorrer;

        BuscarNodoInicial();

        fsm.IniciarPatrulla();
    }

    void Update()
    {
        if (!playerController.isMoving) detectorSonido.radius = 0f;
        else
        {
            if (playerController.isWalking) detectorSonido.radius = radioDeteccionCaminar;
            else detectorSonido.radius = radioDeteccionCorrer;
        }
        
    }

    void FixedUpdate()
    {
        fsm.ActualizarEstado();
    }

    private List<NodoGrafoMurcielago> ObtenerTodosLosNodosDesde(NodoGrafoMurcielago inicio)
{
    List<NodoGrafoMurcielago> nodosEncontrados = new List<NodoGrafoMurcielago>();
    HashSet<NodoGrafoMurcielago> visitados = new HashSet<NodoGrafoMurcielago>();
    Queue<NodoGrafoMurcielago> cola = new Queue<NodoGrafoMurcielago>();

    cola.Enqueue(inicio);
    visitados.Add(inicio);

    while (cola.Count > 0)
    {
        NodoGrafoMurcielago nodo = cola.Dequeue();
        nodosEncontrados.Add(nodo);

        foreach (var vecino in nodo.Vecinos)
        {
            if (vecino != null && !visitados.Contains(vecino))
            {
                visitados.Add(vecino);
                cola.Enqueue(vecino);
            }
        }
    }

    return nodosEncontrados;
}


    private void BuscarNodoInicial()
    {
        if (fsm.nodoInicial != null || puntosPatrulla.Count == 0) return;

        NodoGrafoMurcielago primerWaypoint = puntosPatrulla[0];
        if (primerWaypoint == null) return;

        List<NodoGrafoMurcielago> todosNodos = ObtenerTodosLosNodosDesde(primerWaypoint);

        NodoGrafoMurcielago nodoMasCercano = null;
        float distanciaMinima = float.MaxValue;

        foreach (var nodo in todosNodos)
        {
            float distancia = Vector3.Distance(transform.position, nodo.transform.position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                nodoMasCercano = nodo;
            }
        }

        if (nodoMasCercano != null)
        {
            fsm.nodoInicial = nodoMasCercano;
            Debug.Log($"Nodo inicial asignado: {fsm.nodoInicial.name} (distancia {distanciaMinima:F2})");
        }
    }

    private void OnDrawGizmos()
    {
        if (puntosPatrulla != null && puntosPatrulla.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < puntosPatrulla.Count; i++)
            {
                var nodo = puntosPatrulla[i];
                if (nodo == null) continue;
                Gizmos.DrawSphere(nodo.transform.position, 0.15f);
                var siguiente = puntosPatrulla[(i + 1) % puntosPatrulla.Count];
                if (siguiente != null) Gizmos.DrawLine(nodo.transform.position, siguiente.transform.position);
            }
        }
        #if UNITY_EDITOR
        fsm?.DibujarGizmosRuta();
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        if (puntosPatrulla == null) return;
        for (int i = 0; i < puntosPatrulla.Count; i++)
        {
            var nodo = puntosPatrulla[i];
            if (nodo == null) continue;
            Handles.Label(nodo.transform.position + Vector3.up * 0.25f, $"PuntoPatrulla {i + 1}");
        }
        #endif
    }
}
