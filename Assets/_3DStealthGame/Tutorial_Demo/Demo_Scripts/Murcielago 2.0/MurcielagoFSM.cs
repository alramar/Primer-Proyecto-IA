#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

public class MurcielagoFSM
{
    public enum Estado { Patrulla, Persecucion }
    private Estado estadoActual;
    public Murcielago m { get; private set; }

    private int indicePuntoPatrullaSiguiente = 0;
    private List<NodoGrafoMurcielago> rutaActual;
    private int indiceNodoEnRuta = 0;
    private bool siguiendoRuta = false;

    public NodoGrafoMurcielago nodoInicial;
    private NodoGrafoMurcielago nodoObjetivoPersecucion;
    private Vector3 ultimaPosicionDetectada;

    public MurcielagoFSM(Murcielago murcielago)
    {
        m = murcielago;
        estadoActual = Estado.Patrulla;
    }

    public void ActualizarEstado()
    {
        switch (estadoActual)
        {
            case Estado.Patrulla: Patrullar(); break;
            case Estado.Persecucion: Perseguir(); break;
        }
    }

    public void CambiarEstado(Estado nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;
        estadoActual = nuevoEstado;
    }

    public void IniciarPatrulla()
    {
        if (nodoInicial == null || m.puntosPatrulla.Count == 0) return;
        CalcularRutaAlSiguienteWaypoint();
    }

    private void OrientarHacia(Vector3 objetivo)
    {
        Vector3 dir = objetivo - m.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(dir.normalized);
        m.transform.rotation = Quaternion.Slerp(m.transform.rotation, target, m.velocidadRotacion * Time.deltaTime);
    }

    private void Patrullar()
    {
        if (!siguiendoRuta || rutaActual == null || rutaActual.Count == 0) return;
        var nodoObjetivo = rutaActual[indiceNodoEnRuta];
        if (nodoObjetivo == null) { siguiendoRuta = false; return; }

        OrientarHacia(nodoObjetivo.transform.position);
        m.transform.position = Vector3.MoveTowards(m.transform.position, nodoObjetivo.transform.position, m.velocidad * Time.deltaTime);

        if (Vector3.Distance(m.transform.position, nodoObjetivo.transform.position) <= m.distanciaLlegadaNodo)
        {
            indiceNodoEnRuta++;
            if (indiceNodoEnRuta >= rutaActual.Count)
            {
                nodoInicial = rutaActual[^1];
                indicePuntoPatrullaSiguiente = (indicePuntoPatrullaSiguiente + 1) % m.puntosPatrulla.Count;
                CalcularRutaAlSiguienteWaypoint();
            }
        }

    }

    private void Perseguir()
    {
        if (!siguiendoRuta || rutaActual == null || rutaActual.Count == 0) return;
        var nodoObjetivo = rutaActual[indiceNodoEnRuta];
        if (nodoObjetivo == null) { siguiendoRuta = false; return; }

        OrientarHacia(nodoObjetivo.transform.position);
        m.transform.position = Vector3.MoveTowards(m.transform.position, nodoObjetivo.transform.position, m.velocidad * Time.deltaTime);

        if (Vector3.Distance(m.transform.position, nodoObjetivo.transform.position) <= m.distanciaLlegadaNodo)
        {
            indiceNodoEnRuta++;
            if (indiceNodoEnRuta >= rutaActual.Count)
            {
                nodoInicial = rutaActual[^1];
                CambiarEstado(Estado.Patrulla);
                IniciarPatrulla();
            }
        }
    }

    private void CalcularRutaAlSiguienteWaypoint()
    {
        siguiendoRuta = false;
        indiceNodoEnRuta = 0;
        rutaActual = null;

        var nodoInicio = nodoInicial;
        var nodoDestino = m.puntosPatrulla[indicePuntoPatrullaSiguiente];
        if (nodoInicio == null || nodoDestino == null) return;

        rutaActual = BusquedaAEstrella(nodoInicio, nodoDestino);
        if (rutaActual == null || rutaActual.Count == 0) { siguiendoRuta = false; rutaActual = null; }
        else { siguiendoRuta = true; indiceNodoEnRuta = 0; }
    }

    public void EmpezarPersecucionHacia(Vector3 posicionDetectada)
    {
        ultimaPosicionDetectada = posicionDetectada;

        var nodoDestino = m.EncontrarNodoMasCercano(posicionDetectada);
        if (nodoDestino == null) return;

        nodoObjetivoPersecucion = nodoDestino;
        nodoInicial = m.EncontrarNodoMasCercano(m.transform.position);
        if (nodoInicial == null) return;

        rutaActual = BusquedaAEstrella(nodoInicial, nodoObjetivoPersecucion);
        if (rutaActual == null || rutaActual.Count == 0)
        {
            siguiendoRuta = false;
            rutaActual = null;
            CambiarEstado(Estado.Persecucion);
            return;
        }

        indiceNodoEnRuta = 0;
        siguiendoRuta = true;
        CambiarEstado(Estado.Persecucion);
    }

    public void ActualizarPosicionDetectada(Vector3 posicionDetectada)
    {
        ultimaPosicionDetectada = posicionDetectada;
        var nuevoNodo = m.EncontrarNodoMasCercano(posicionDetectada);
        if (nuevoNodo == null) return;

        if (nodoObjetivoPersecucion == null || nuevoNodo != nodoObjetivoPersecucion)
        {
            nodoObjetivoPersecucion = nuevoNodo;
            nodoInicial = m.EncontrarNodoMasCercano(m.transform.position);
            if (nodoInicial == null) return;

            rutaActual = BusquedaAEstrella(nodoInicial, nodoObjetivoPersecucion);
            if (rutaActual == null || rutaActual.Count == 0) { siguiendoRuta = false; rutaActual = null; }
            else { siguiendoRuta = true; indiceNodoEnRuta = 0; }
        }
    }

    private class MinHeap
    {
        private class HeapNode { public NodoGrafoMurcielago n; public float p; public HeapNode(NodoGrafoMurcielago n, float p) { this.n = n; this.p = p; } }
        private List<HeapNode> heap = new List<HeapNode>();
        public int Count => heap.Count;
        public void Enqueue(NodoGrafoMurcielago n, float p) { heap.Add(new HeapNode(n, p)); SiftUp(heap.Count - 1); }
        public NodoGrafoMurcielago Dequeue() { if (heap.Count == 0) return null; var r = heap[0].n; var l = heap[^1]; heap.RemoveAt(heap.Count - 1); if (heap.Count > 0) { heap[0] = l; SiftDown(0); } return r; }
        private void SiftUp(int i) { while (i > 0) { int p = (i - 1) / 2; if (heap[i].p >= heap[p].p) break; (heap[i], heap[p]) = (heap[p], heap[i]); i = p; } }
        private void SiftDown(int i) { int n = heap.Count; while (true) { int l = 2 * i + 1, r = 2 * i + 2, s = i; if (l < n && heap[l].p < heap[s].p) s = l; if (r < n && heap[r].p < heap[s].p) s = r; if (s == i) break; (heap[i], heap[s]) = (heap[s], heap[i]); i = s; } }
    }

    public List<NodoGrafoMurcielago> BusquedaAEstrella(NodoGrafoMurcielago inicio, NodoGrafoMurcielago objetivo)
    {
        if (inicio == null || objetivo == null) return null;
        if (inicio == objetivo) return new List<NodoGrafoMurcielago> { inicio };

        System.Func<NodoGrafoMurcielago, float> H = n => Vector3.Distance(n.transform.position, objetivo.transform.position);
        var open = new MinHeap();
        var cameFrom = new Dictionary<NodoGrafoMurcielago, NodoGrafoMurcielago>();
        var g = new Dictionary<NodoGrafoMurcielago, float>();
        var closed = new HashSet<NodoGrafoMurcielago>();

        g[inicio] = 0f;
        open.Enqueue(inicio, H(inicio));

        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (current == null || closed.Contains(current)) continue;
            if (current == objetivo)
            {
                var path = new List<NodoGrafoMurcielago>();
                var c = current;
                while (c != null)
                {
                    path.Add(c);
                    cameFrom.TryGetValue(c, out c);
                }
                path.Reverse();
                return path;
            }

            closed.Add(current);
            foreach (var v in current.Vecinos)
            {
                if (v == null || closed.Contains(v)) continue;
                float t = g[current] + 1f;
                if (!g.TryGetValue(v, out float prev) || t < prev)
                {
                    cameFrom[v] = current;
                    g[v] = t;
                    open.Enqueue(v, t + H(v));
                }
            }
        }
        return null;
    }

    #if UNITY_EDITOR
    public void DibujarGizmosRuta()
    {
        if (rutaActual == null || rutaActual.Count == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < rutaActual.Count; i++)
        {
            var nodo = rutaActual[i];
            if (nodo == null) continue;
            Gizmos.DrawSphere(nodo.transform.position, 0.2f);

            if (i < rutaActual.Count - 1 && rutaActual[i + 1] != null)
                Gizmos.DrawLine(nodo.transform.position, rutaActual[i + 1].transform.position);
        }

        if (indiceNodoEnRuta < rutaActual.Count && rutaActual[indiceNodoEnRuta] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rutaActual[indiceNodoEnRuta].transform.position, 0.3f);
        }
    }
    #endif
}
