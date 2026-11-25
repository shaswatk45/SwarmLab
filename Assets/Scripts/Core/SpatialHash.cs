using System.Collections.Generic;
using UnityEngine;

namespace SwarmLab
{
    /// <summary>
    /// A uniform spatial hash grid. It buckets agents into cells of a fixed size so
    /// that, to find an agent's neighbours, we only look at the 27 cells around it
    /// instead of comparing against every other agent. This is what lets the swarm
    /// scale to 1000+ agents in real time (O(n) instead of O(n^2) per frame).
    /// </summary>
    public class SpatialHash
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

        public SpatialHash(float cellSize)
        {
            _cellSize = Mathf.Max(0.01f, cellSize);
        }

        // Pack a 3D cell coordinate into a single long key.
        private long Key(int x, int y, int z)
        {
            // Offset so negative coords stay unique, then pack into 21-bit fields.
            const long B = 1L << 20;
            return ((x + B) & 0x1FFFFF)
                 | (((y + B) & 0x1FFFFF) << 21)
                 | (((z + B) & 0x1FFFFF) << 42);
        }

        private Vector3Int CellOf(Vector3 p)
        {
            return new Vector3Int(
                Mathf.FloorToInt(p.x / _cellSize),
                Mathf.FloorToInt(p.y / _cellSize),
                Mathf.FloorToInt(p.z / _cellSize));
        }

        /// <summary>
        /// Clear and refill the grid from the current agent positions. Agents whose
        /// <paramref name="active"/> flag is false (killed) are left out, so dead
        /// robots stop influencing their neighbours.
        /// </summary>
        public void Rebuild(Vector3[] positions, bool[] active, int count)
        {
            foreach (var list in _cells.Values) list.Clear();

            for (int i = 0; i < count; i++)
            {
                if (active != null && !active[i]) continue;
                Vector3Int c = CellOf(positions[i]);
                long k = Key(c.x, c.y, c.z);
                if (!_cells.TryGetValue(k, out var list))
                {
                    list = new List<int>(16);
                    _cells[k] = list;
                }
                list.Add(i);
            }
        }

        /// <summary>
        /// Fill <paramref name="results"/> with the indices of agents in the 3x3x3
        /// block of cells around <paramref name="position"/>. Caller filters by radius.
        /// </summary>
        public void QueryNeighbours(Vector3 position, List<int> results)
        {
            results.Clear();
            Vector3Int c = CellOf(position);
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        long k = Key(c.x + x, c.y + y, c.z + z);
                        if (_cells.TryGetValue(k, out var list))
                            results.AddRange(list);
                    }
        }
    }
}
