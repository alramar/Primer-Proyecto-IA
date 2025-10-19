using System.Collections.Generic;
using UnityEngine;

public class MurcielagoFSM
{
    private enum Estado { Patrulla, Persecución }
    private Estado estadoActual;
    private Murcielago murcielago;

    public MurcielagoFSM(Murcielago murcielago)
    {
        this.murcielago = murcielago;
        estadoActual = Estado.Patrulla;
    }

    public void ActualizarEstado()
    {
        switch (estadoActual)
        {
            case Estado.Patrulla: Patrullar(); break;
            case Estado.Persecución: Perseguir(); break;
        }
    }

    private void CambiarEstado(Estado nuevoEstado) => estadoActual = nuevoEstado;

    private void Patrullar() { }
    private void Perseguir() { }

    // --- MinHeap interno para A* ---
    private class MinHeap
    {
        private class HeapNode
        {
            public NodoGrafoMurcielago node;
            public float priority;
            public HeapNode(NodoGrafoMurcielago n, float p) { node = n; priority = p; }
        }

        private List<HeapNode> heap = new List<HeapNode>();
        public int Count => heap.Count;

        public void Enqueue(NodoGrafoMurcielago node, float priority)
        {
            heap.Add(new HeapNode(node, priority));
            SiftUp(heap.Count - 1);
        }

        public NodoGrafoMurcielago Dequeue()
        {
            if (heap.Count == 0) return null;
            var result = heap[0].node;
            var last = heap[^1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0)
            {
                heap[0] = last;
                SiftDown(0);
            }
            return result;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (heap[i].priority >= heap[p].priority) break;
                (heap[i], heap[p]) = (heap[p], heap[i]);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            int n = heap.Count;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;
                if (left < n && heap[left].priority < heap[smallest].priority) smallest = left;
                if (right < n && heap[right].priority < heap[smallest].priority) smallest = right;
                if (smallest == i) break;
                (heap[i], heap[smallest]) = (heap[smallest], heap[i]);
                i = smallest;
            }
        }
    }

    // --- A* usando los nuevos vecinos ---
    public List<NodoGrafoMurcielago> BusquedaAEstrella(NodoGrafoMurcielago inicio, NodoGrafoMurcielago objetivo)
    {
        if (inicio == null || objetivo == null) return null;
        if (inicio == objetivo) return new List<NodoGrafoMurcielago> { inicio };

        System.Func<NodoGrafoMurcielago, float> Heuristica =
            n => Vector3.Distance(n.transform.position, objetivo.transform.position);

        var openHeap = new MinHeap();
        var cameFrom = new Dictionary<NodoGrafoMurcielago, NodoGrafoMurcielago>();
        var gScore = new Dictionary<NodoGrafoMurcielago, float>();
        var closed = new HashSet<NodoGrafoMurcielago>();

        gScore[inicio] = 0f;
        openHeap.Enqueue(inicio, Heuristica(inicio));

        while (openHeap.Count > 0)
        {
            NodoGrafoMurcielago current = openHeap.Dequeue();
            if (current == null || closed.Contains(current)) continue;

            if (current == objetivo)
            {
                var path = new List<NodoGrafoMurcielago>();
                NodoGrafoMurcielago cur = current;
                while (cur != null)
                {
                    path.Add(cur);
                    cameFrom.TryGetValue(cur, out cur);
                }
                path.Reverse();
                return path;
            }

            closed.Add(current);

            foreach (var vecino in current.Vecinos)
            {
                if (vecino == null || closed.Contains(vecino)) continue;

                float tentativeG = gScore[current] + 1f;

                if (!gScore.TryGetValue(vecino, out float prevG) || tentativeG < prevG)
                {
                    cameFrom[vecino] = current;
                    gScore[vecino] = tentativeG;
                    openHeap.Enqueue(vecino, tentativeG + Heuristica(vecino));
                }
            }
        }

        return null;
    }
}
