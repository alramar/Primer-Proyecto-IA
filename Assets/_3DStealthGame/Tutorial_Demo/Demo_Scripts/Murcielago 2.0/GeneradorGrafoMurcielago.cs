using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GeneradorGrafoMurcielago : MonoBehaviour
{
    [Header("Parámetros del grafo")]
    [SerializeField] private float alturaGrafo = 1.0f;
    [SerializeField] private float distanciaEntreNodos = 1.0f;
    [SerializeField] private string tagSuperficie = "Grafo";

    private List<NodoGrafoMurcielago> nodos = new List<NodoGrafoMurcielago>();
    private NodoGrafoMurcielago nodoEntrada = null;
    private Vector3 origenGrid;

    private class Arco
    {
        public NodoGrafoMurcielago vecino;
        public Arco siguiente;

        public Arco(NodoGrafoMurcielago vecino, Arco siguiente)
        {
            this.vecino = vecino;
            this.siguiente = siguiente;
        }
    }

    private class NodoGrafoMurcielago : MonoBehaviour
    {
        public Arco primerArco;
        public int grado;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.1f);

            Gizmos.color = Color.red;
            for (Arco arco = primerArco; arco != null; arco = arco.siguiente)
            {
                if (arco.vecino != null)
                    Gizmos.DrawLine(transform.position, arco.vecino.transform.position);
            }
        }

        public void Conectar(NodoGrafoMurcielago otro)
        {
            if (otro == null || otro == this) return;
            for (Arco arco = primerArco; arco != null; arco = arco.siguiente)
            {
                if (arco.vecino == otro) return;
            }
            primerArco = new Arco(otro, primerArco);
            grado++;
        }
    }

    private void Vaciar()
    {
        NodoGrafoMurcielago[] nodosExistentes = GetComponentsInChildren<NodoGrafoMurcielago>();
        foreach (NodoGrafoMurcielago nodo in nodosExistentes)
        {
            DestroyImmediate(nodo.gameObject);
        }
        nodos.Clear();
        nodoEntrada = null;
    }

    private NodoGrafoMurcielago CrearNodo(Vector3 posicion)
    {
        GameObject nodoGO = new GameObject("NodoMurcielago");
        nodoGO.transform.SetParent(transform);
        nodoGO.transform.position = posicion;

        NodoGrafoMurcielago nodo = nodoGO.AddComponent<NodoGrafoMurcielago>();
        nodos.Add(nodo);

        return nodo;
    }

    private bool ExisteNodoCercano(Vector3 posicion)
    {
        foreach (var nodo in nodos)
        {
            if (Vector3.Distance(nodo.transform.position, posicion) <= distanciaEntreNodos / 2)
                return true;
        }
        return false;
    }

    private void ConectarGrid(NodoGrafoMurcielago nodo, float x, float z)
    {
        foreach (var otro in nodos)
        {
            float dx = Mathf.Abs(otro.transform.position.x - x);
            float dz = Mathf.Abs(otro.transform.position.z - z);

            // 🔹 Conexión con vecinos cercanos (hasta 8 direcciones)
            if (dx <= distanciaEntreNodos * 1.1f && dz <= distanciaEntreNodos * 1.1f)
            {
                if (otro != nodo)
                {
                    nodo.Conectar(otro);
                    otro.Conectar(nodo);
                }
            }
        }
    }

    // ===========================
    // Método principal
    // ===========================

    [ContextMenu("Crear Grafo")]
    public void CrearGrafo()
    {
        Vaciar();

        GameObject superficie = GameObject.FindWithTag(tagSuperficie);
        if (superficie == null)
        {
            Debug.LogWarning($"No se encontró ningún objeto con el tag '{tagSuperficie}'.");
            return;
        }

        bool primerNodoCreado = false;

        foreach (Transform hijo in superficie.transform)
        {
            Collider col = hijo.GetComponent<Collider>();
            Renderer rend = hijo.GetComponent<Renderer>();

            Bounds bounds;
            if (col != null) bounds = col.bounds;
            else if (rend != null) bounds = rend.bounds;
            else continue;

            for (float x = bounds.min.x + distanciaEntreNodos; x <= bounds.max.x; x += distanciaEntreNodos)
            {
                for (float z = bounds.min.z + distanciaEntreNodos; z <= bounds.max.z; z += distanciaEntreNodos)
                {
                    Vector3 pos = new Vector3(x, alturaGrafo, z);
                    if (!primerNodoCreado)
                    {
                        primerNodoCreado = true;
                        origenGrid = pos;
                    }
                    float offsetX = Mathf.Round((x - origenGrid.x) / distanciaEntreNodos) * distanciaEntreNodos;
                    float offsetZ = Mathf.Round((z - origenGrid.z) / distanciaEntreNodos) * distanciaEntreNodos;
                    pos = new Vector3(origenGrid.x + offsetX, alturaGrafo, origenGrid.z + offsetZ);

                    if (ExisteNodoCercano(pos)) continue;

                    NodoGrafoMurcielago nuevoNodo = CrearNodo(pos);
                    if (nodoEntrada == null) nodoEntrada = nuevoNodo;

                    ConectarGrid(nuevoNodo, pos.x, pos.z);
                }
            }
        }

        Debug.Log($"Grafo creado con {nodos.Count} nodos. Entrada: {nodoEntrada.name}");
    }
}
