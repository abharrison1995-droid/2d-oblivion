using System;
using System.Collections.Generic;

namespace Voidovia
{
    /// <summary>
    /// Hand-authored node graph. Pathfinding is Dijkstra over road edges.
    /// </summary>
    public class WorldGraph
    {
        readonly Dictionary<string, MapNodeData> _nodes = new();
        readonly Dictionary<string, List<RoadEdgeData>> _adjacency = new();
        readonly Dictionary<string, RoadEdgeData> _offPathEdges = new();
        readonly List<RoadEdgeData> _offPathCanonical = new();

        public IReadOnlyDictionary<string, MapNodeData> Nodes => _nodes;

        /// <summary>One entry per off-path connection (not doubled for both travel directions) — for drawing.</summary>
        public IReadOnlyList<RoadEdgeData> OffPathEdges => _offPathCanonical;

        public void Load(WorldMapData data)
        {
            _nodes.Clear();
            _adjacency.Clear();
            _offPathEdges.Clear();
            _offPathCanonical.Clear();

            foreach (var node in data.nodes)
            {
                _nodes[node.id] = node;
                _adjacency[node.id] = new List<RoadEdgeData>();
            }

            foreach (var road in data.roads)
            {
                if (!_adjacency.ContainsKey(road.fromNodeId) || !_adjacency.ContainsKey(road.toNodeId))
                    continue;

                if (road.isOffPath)
                {
                    _offPathCanonical.Add(road);
                    _offPathEdges[road.fromNodeId + "|" + road.toNodeId] = road;
                    _offPathEdges[road.toNodeId + "|" + road.fromNodeId] = new RoadEdgeData
                    {
                        id = road.id + "_rev",
                        fromNodeId = road.toNodeId,
                        toNodeId = road.fromNodeId,
                        travelHours = road.travelHours,
                        danger = road.danger,
                        terrain = road.terrain,
                        allowSevereRaids = false,
                        isOffPath = true,
                        offPathSpeedMultiplier = road.offPathSpeedMultiplier
                    };
                    continue; // not part of normal pathfinding or road-drawing
                }

                _adjacency[road.fromNodeId].Add(road);
                // Undirected travel for v1
                var reverse = new RoadEdgeData
                {
                    id = road.id + "_rev",
                    fromNodeId = road.toNodeId,
                    toNodeId = road.fromNodeId,
                    travelHours = road.travelHours,
                    danger = road.danger,
                    terrain = road.terrain,
                    allowSevereRaids = road.allowSevereRaids
                };
                _adjacency[road.toNodeId].Add(reverse);
            }
        }

        /// <summary>The unmaintained off-path connection between two nodes, if one exists (either direction).</summary>
        public bool TryGetOffPathEdge(string fromId, string toId, out RoadEdgeData edge) =>
            _offPathEdges.TryGetValue(fromId + "|" + toId, out edge);

        public bool TryGetNode(string id, out MapNodeData node) => _nodes.TryGetValue(id, out node);

        public List<RoadEdgeData> GetRoute(string fromId, string toId)
        {
            var path = new List<RoadEdgeData>();
            if (fromId == toId || !_nodes.ContainsKey(fromId) || !_nodes.ContainsKey(toId))
                return path;

            // A direct road always wins over a technically-cheaper multi-hop detour — clicking
            // a directly-connected neighbor should go straight there, not through a third city
            // Dijkstra found marginally faster by summed travelHours.
            if (_adjacency.TryGetValue(fromId, out var directEdges))
            {
                foreach (var edge in directEdges)
                {
                    if (edge.toNodeId != toId) continue;
                    path.Add(edge);
                    return path;
                }
            }

            var dist = new Dictionary<string, float>();
            var prev = new Dictionary<string, RoadEdgeData>();
            var queue = new SortedSet<(float d, string id)>(Comparer<(float d, string id)>.Create((a, b) =>
            {
                var cmp = a.d.CompareTo(b.d);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.id, b.id);
            }));

            foreach (var id in _nodes.Keys)
                dist[id] = float.PositiveInfinity;

            dist[fromId] = 0f;
            queue.Add((0f, fromId));

            while (queue.Count > 0)
            {
                var (d, current) = queue.Min;
                queue.Remove(queue.Min);

                if (current == toId)
                    break;

                if (d > dist[current])
                    continue;

                foreach (var edge in _adjacency[current])
                {
                    var nd = d + edge.travelHours;
                    if (nd >= dist[edge.toNodeId])
                        continue;

                    queue.Remove((dist[edge.toNodeId], edge.toNodeId));
                    dist[edge.toNodeId] = nd;
                    prev[edge.toNodeId] = edge;
                    queue.Add((nd, edge.toNodeId));
                }
            }

            if (!prev.ContainsKey(toId))
                return path;

            var stack = new Stack<RoadEdgeData>();
            for (var at = toId; at != fromId && prev.ContainsKey(at); at = prev[at].fromNodeId)
                stack.Push(prev[at]);

            while (stack.Count > 0)
                path.Add(stack.Pop());

            return path;
        }

        public void AddTemporaryNode(MapNodeData node, RoadEdgeData linkFromSettlement)
        {
            _nodes[node.id] = node;
            if (!_adjacency.ContainsKey(node.id))
                _adjacency[node.id] = new List<RoadEdgeData>();

            if (!_adjacency.ContainsKey(linkFromSettlement.fromNodeId))
                return;

            _adjacency[linkFromSettlement.fromNodeId].Add(linkFromSettlement);
            _adjacency[node.id].Add(new RoadEdgeData
            {
                id = linkFromSettlement.id + "_rev",
                fromNodeId = linkFromSettlement.toNodeId,
                toNodeId = linkFromSettlement.fromNodeId,
                travelHours = linkFromSettlement.travelHours,
                danger = linkFromSettlement.danger,
                terrain = linkFromSettlement.terrain,
                allowSevereRaids = false
            });
        }

        public void RemoveNode(string nodeId)
        {
            if (!_nodes.Remove(nodeId))
                return;

            _adjacency.Remove(nodeId);
            foreach (var list in _adjacency.Values)
                list.RemoveAll(e => e.toNodeId == nodeId);
        }
    }
}
