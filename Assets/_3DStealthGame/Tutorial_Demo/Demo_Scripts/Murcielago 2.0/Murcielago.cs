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

    [Header("Waypoints del Murciélago")]
    public List<NodoGrafoMurcielago> puntosPatrulla = new List<NodoGrafoMurcielago>();
    private int indicePuntoPatrullaSiguiente = 0;

    [Header("Stats de Murciélago")]
    [SerializeField] private float velocidad = 2f;
    [SerializeField] private float radioDeteccionCaminar = 0.6f;
    [SerializeField] private float radioDeteccionCorrer = 1.5f;

    public NodoGrafoMurcielago nodoInicial;
    public List<NodoGrafoMurcielago> rutaActual;

    private bool siguiendoRuta = false;
    private int indiceNodoEnRuta = 0;
    [SerializeField] private float distanciaLlegadaNodo = 0.2f;

    void Start()
    {
        fsm = new MurcielagoFSM(this);
        gameEnding = FindFirstObjectByType<GameEnding>();
        playerController = FindFirstObjectByType<PlayerController>();
        detectorSonido = GameObject.FindWithTag("SoundCollider")?.GetComponent<SphereCollider>();

        if (detectorSonido != null)
            detectorSonido.radius = radioDeteccionCorrer;

        BuscarNodoInicial();

        if (nodoInicial != null && puntosPatrulla.Count > 0)
            CalcularRutaAlSiguienteWaypoint();
    }

    void Update()
    {
        if (playerController != null && detectorSonido != null)
        {
            detectorSonido.radius = playerController.isMoving
                ? (playerController.isWalking ? radioDeteccionCaminar : radioDeteccionCorrer)
                : 0f;
        }

        if (!siguiendoRuta || rutaActual == null || rutaActual.Count == 0) return;

        NodoGrafoMurcielago nodoObjetivo = rutaActual[indiceNodoEnRuta];
        if (nodoObjetivo == null)
        {
            siguiendoRuta = false;
            return;
        }

        // Moverse hacia el nodo objetivo
        transform.position = Vector3.MoveTowards(transform.position, nodoObjetivo.transform.position, velocidad * Time.deltaTime);

        // Comprobar si ha llegado al nodo
        if (Vector3.Distance(transform.position, nodoObjetivo.transform.position) <= distanciaLlegadaNodo)
        {
            indiceNodoEnRuta++;

            // Si llegó al último nodo de la ruta
            if (indiceNodoEnRuta >= rutaActual.Count)
            {
                nodoInicial = rutaActual[rutaActual.Count - 1]; // actualizar nodo inicial
                indicePuntoPatrullaSiguiente = (indicePuntoPatrullaSiguiente + 1) % puntosPatrulla.Count;
                CalcularRutaAlSiguienteWaypoint(); // recalcular ruta
            }
        }
    }


    void FixedUpdate()
    {
        fsm.ActualizarEstado();
    }

    private void CalcularRutaAlSiguienteWaypoint()
    {
        siguiendoRuta = false;
        indiceNodoEnRuta = 0;
        rutaActual = null;

        if (nodoInicial == null || puntosPatrulla.Count == 0) return;

        NodoGrafoMurcielago nodoDestino = puntosPatrulla[indicePuntoPatrullaSiguiente];
        if (nodoDestino == null) return;

        rutaActual = fsm.BusquedaAEstrella(nodoInicial, nodoDestino);
        if (rutaActual == null || rutaActual.Count == 0)
        {
            siguiendoRuta = false;
            rutaActual = null;
            Debug.Log("CalcularRuta: no se encontró ruta hacia el waypoint.");
        }
        else
        {
            siguiendoRuta = true;
            indiceNodoEnRuta = 0;
            Debug.Log($"CalcularRuta: ruta con {rutaActual.Count} nodos calculada.");
        }
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
        if (nodoInicial != null || puntosPatrulla.Count == 0) return;

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
            nodoInicial = nodoMasCercano;
            Debug.Log($"Nodo inicial asignado: {nodoInicial.name} (distancia {distanciaMinima:F2})");
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

        if (rutaActual != null && rutaActual.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < rutaActual.Count; i++)
            {
                var n = rutaActual[i];
                if (n == null) continue;
                Gizmos.DrawSphere(n.transform.position, 0.12f);
                if (i < rutaActual.Count - 1 && rutaActual[i + 1] != null)
                    Gizmos.DrawLine(n.transform.position, rutaActual[i + 1].transform.position);
            }
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
