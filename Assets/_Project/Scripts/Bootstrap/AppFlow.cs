using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Boots data, runs character creation, then opens the scrollable map + battle UI.
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        CharacterCreationUI _creation;
        WorldMapUI _map;
        BattleUI _battle;
        bool _started;

        void Start()
        {
            EnsureGameState();
            LoadAllData();

            _battle = gameObject.AddComponent<BattleUI>();
            _map = gameObject.AddComponent<WorldMapUI>();
            _creation = gameObject.AddComponent<CharacterCreationUI>();
            _creation.Completed += OnCreationDone;
            _creation.Show();
        }

        void OnCreationDone(CharacterCreationResult result)
        {
            if (_started) return;
            _started = true;
            GameState.Instance.ApplyCharacterCreation(result);
            GameState.Instance.Act1Quest.StartQuest();
            _map.Show();
            _map.AppendLog($"Welcome, {result.heroName}. {result.stolenItemFlavour}");
            _map.AppendLog("Butter rogues took it. Start by reaching Greyledger for advice.");
        }

        static void EnsureGameState()
        {
            if (GameState.Instance != null) return;
            var go = new GameObject("GameState");
            go.AddComponent<GameState>();
        }

        static void LoadAllData()
        {
            var map = DataLoader.LoadJson<WorldMapData>("voidovia_map.json");
            var troops = DataLoader.LoadJson<TroopRosterData>("troops.json");
            var catalog = DataLoader.LoadJson<EconomyCatalog>("economy.json");
            var cards = DataLoader.LoadJson<BattleCardCatalogData>("battle_cards.json");
            GameState.Instance.BindData(map, troops, catalog, cards);
            Debug.Log($"[AppFlow] Loaded {map.nodes.Length} nodes / {cards.cards.Length} battle cards");
        }
    }
}
