using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Lets you press Play on WorldMap without opening Bootstrap first.
    /// </summary>
    public class WorldMapEntry : MonoBehaviour
    {
        void Awake()
        {
            if (GameState.Instance != null)
                return;

            var go = new GameObject("GameState");
            go.AddComponent<GameState>();

            var map = DataLoader.LoadJson<WorldMapData>("voidovia_map.json");
            var troops = DataLoader.LoadJson<TroopRosterData>("troops.json");
            var catalog = DataLoader.LoadJson<EconomyCatalog>("economy.json");
            GameState.Instance.BindData(map, troops, catalog);

            var origin = OriginCatalog.VoidoviaStarts[1];
            GameState.Instance.ApplyOrigin(origin);
            GameState.Instance.Act1Quest.StartQuest();
            Debug.Log($"[WorldMapEntry] Auto-booted as {origin.displayName}");
        }
    }
}
