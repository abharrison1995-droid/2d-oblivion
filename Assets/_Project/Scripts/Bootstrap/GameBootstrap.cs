using UnityEngine;
using UnityEngine.SceneManagement;

namespace Voidovia
{
    /// <summary>
    /// Entry point for the Greyledger→Lair vertical slice.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] string worldMapScene = "WorldMap";

        void Start()
        {
            EnsureGameState();
            LoadAllData();
            // Origin UI will replace this default; for skeleton, start as mill levy.
            if (string.IsNullOrEmpty(GameState.Instance.Hero.originId))
            {
                var origin = OriginCatalog.VoidoviaStarts[1];
                GameState.Instance.ApplyOrigin(origin);
                GameState.Instance.Act1Quest.StartQuest();
                Debug.Log($"[Bootstrap] Origin: {origin.displayName}. Stolen: {origin.stolenItemFlavour}");
            }

            if (SceneManager.GetActiveScene().name != worldMapScene)
                SceneManager.LoadScene(worldMapScene);
        }

        static void EnsureGameState()
        {
            if (GameState.Instance != null)
                return;

            var go = new GameObject("GameState");
            go.AddComponent<GameState>();
        }

        static void LoadAllData()
        {
            var map = DataLoader.LoadJson<WorldMapData>("voidovia_map.json");
            var troops = DataLoader.LoadJson<TroopRosterData>("troops.json");
            var catalog = DataLoader.LoadJson<EconomyCatalog>("economy.json");
            GameState.Instance.BindData(map, troops, catalog);
            Debug.Log($"[Bootstrap] Map nodes: {map.nodes.Length}, roads: {map.roads.Length}, troops: {troops.troops.Length}");
        }
    }
}
