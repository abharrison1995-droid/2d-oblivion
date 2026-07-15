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
