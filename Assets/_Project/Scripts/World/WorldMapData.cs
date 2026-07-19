using System;
using UnityEngine;

namespace Voidovia
{
    [Serializable]
    public class MapNodeData
    {
        public string id;
        public string displayName;
        public NodeType type;
        public FactionId controllingFaction = FactionId.Voidovia;
        public Vector2 mapPosition;
        public bool isTemporary;
        public string parentSettlementId;
        public bool hasStore;
        public bool hasTavern;
        public bool hasRecruitment;
        public bool hasBookStore;
        /// <summary>
        /// Visible on the strategic map, but deep content comes later.
        /// </summary>
        public bool isSkeleton;
        /// <summary>Shown in SettlementMenuUI's description label. Empty until written.</summary>
        public string flavorText = "";
    }

    [Serializable]
    public class RoadEdgeData
    {
        public string id;
        public string fromNodeId;
        public string toNodeId;
        public float travelHours = GameConstants.BaseTravelHoursPerEdge;
        public float danger = 0.2f;
        public TerrainType terrain = TerrainType.Road;
        public bool allowSevereRaids;

        /// <summary>
        /// No maintained road — excluded from normal Dijkstra pathfinding and road-drawing.
        /// Reachable only via JourneyController's off-path travel, with its own speed penalty
        /// and hostile-weighted encounters.
        /// </summary>
        public bool isOffPath;
        public float offPathSpeedMultiplier = 1.7f;
    }

    [Serializable]
    public class WorldMapData
    {
        public string kingdomId = "voidovia";
        public string displayName = "Voidovia";
        public MapNodeData[] nodes = Array.Empty<MapNodeData>();
        public RoadEdgeData[] roads = Array.Empty<RoadEdgeData>();
    }
}
