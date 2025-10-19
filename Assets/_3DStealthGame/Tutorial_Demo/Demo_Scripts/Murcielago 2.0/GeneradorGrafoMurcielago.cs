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

    private void Vaciar()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("No se puede vaciar el grafo en modo Play.");
            return;
        }

        NodoGrafoMurcielago[] nodosExistentes = GetComponentsInChildren<NodoGrafoMurcielago>();
        foreach (NodoGrafoMurcielago nodo in nodosExistentes)
        {
            if (nodo != null)
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

            if (dx <= distanciaEntreNodos * 1.1f && dz <= distanciaEntreNodos * 1.1f && otro != nodo)
            {
                nodo.Conectar(otro);
                otro.Conectar(nodo);
            }
        }
    }

    [ContextMenu("Crear Grafo")]
    public void CrearGrafo()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("No se puede crear el grafo durante el juego.");
            return;
        }

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
