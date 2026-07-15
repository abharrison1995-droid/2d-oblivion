using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Boots data, runs character creation (or load), then opens the map.
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
            if (!TryLoadAllData(out var error))
            {
                ShowBootError(error);
                return;
            }

            _battle = gameObject.AddComponent<BattleUI>();
            _map = gameObject.AddComponent<WorldMapUI>();
            _creation = gameObject.AddComponent<CharacterCreationUI>();
            _creation.Completed += OnCreationDone;
            _creation.LoadSaveRequested += OnLoadSaveRequested;
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
            _map.AppendLog("Tip: use Save / Load on the map. Smoke test: Docs/SMOKE_TEST.txt");
        }

        void OnLoadSaveRequested()
        {
            if (_started) return;
            if (!SaveLoadService.SaveExists())
            {
                Debug.LogWarning("[AppFlow] No save file yet.");
                return;
            }

            if (!SaveLoadService.TryLoad(GameState.Instance))
            {
                Debug.LogError("[AppFlow] Save load failed.");
                return;
            }

            _started = true;
            _creation.Hide();
            _map.Show();
            _map.AppendLog($"Loaded save for {GameState.Instance.Hero.name} at {GameState.Instance.Party.currentNodeId}.");
            _map.AppendLog($"Quest beat: {GameState.Instance.Act1Quest.Beat}");
        }

        static void EnsureGameState()
        {
            if (GameState.Instance != null) return;
            var go = new GameObject("GameState");
            go.AddComponent<GameState>();
        }

        static bool TryLoadAllData(out string error)
        {
            error = null;
            if (!DataLoader.TryLoadJson<WorldMapData>("voidovia_map.json", out var map, out error)) return false;
            if (!DataLoader.TryLoadJson<TroopRosterData>("troops.json", out var troops, out error)) return false;
            if (!DataLoader.TryLoadJson<EconomyCatalog>("economy.json", out var catalog, out error)) return false;
            if (!DataLoader.TryLoadJson<BattleCardCatalogData>("battle_cards.json", out var cards, out error)) return false;
            if (map?.nodes == null || map.nodes.Length == 0)
            {
                error = "voidovia_map.json loaded but has no nodes.";
                return false;
            }

            GameState.Instance.BindData(map, troops, catalog, cards);
            Debug.Log($"[AppFlow] OK — {map.nodes.Length} nodes, {cards.cards.Length} cards. StreamingAssets={Application.streamingAssetsPath}");
            return true;
        }

        static void ShowBootError(string error)
        {
            var canvas = UiFactory.CreateCanvas("BootErrorCanvas", 100);
            var root = UiFactory.Panel(canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.15f, 0.05f, 0.05f, 0.98f));
            var t = UiFactory.Label(root, "Title", "Voidovia — boot failed", 36, TextAnchor.UpperCenter, new Color(1f, 0.7f, 0.5f));
            var tr = t.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.05f, 0.75f);
            tr.anchorMax = new Vector2(0.95f, 0.95f);
            var b = UiFactory.Label(root, "Body", error + "\n\nSee README troubleshooting. Console has the same error.", 24, TextAnchor.UpperLeft, Color.white);
            var br = b.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.06f, 0.15f);
            br.anchorMax = new Vector2(0.94f, 0.72f);
        }
    }
}
