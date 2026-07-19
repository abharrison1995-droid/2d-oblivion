using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Text battle with unit bars; multi-select command cards + optional power card.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _title;
        Text _visual;
        Text _kuo;
        Text _log;
        RectTransform _optionsRoot;
        Action<BattleOutcome> _onFinished;
        bool _active;

        readonly HashSet<string> _selectedCommands = new();
        string _selectedPower;

        public void BeginLairBattle(Action<BattleOutcome> onFinished)
        {
            var g = GameState.Instance;
            var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
            var enemy = BattleForceTables.Lair(g.Rng);
            Begin(player, enemy, true, StolenItemQuestController.ButterChiefId, onFinished);
        }

        public void BeginEncounterBattle(TravelEncounter encounter, Action<BattleOutcome> onFinished)
        {
            var g = GameState.Instance;
            var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
            var enemy = encounter.cachedForce ?? EncounterForce(encounter.kind);
            Begin(player, enemy, false, null, onFinished);
        }

        public void BeginBanditCampBattle(string campDisplayName, Action<BattleOutcome> onFinished)
        {
            var g = GameState.Instance;
            var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
            Begin(player, BattleForceTables.BanditCamp(campDisplayName, g.Rng), false, null, onFinished, isCampRaid: true);
        }

        /// <summary>Fallback flat composition, used only if an encounter somehow reaches battle without a
        /// pre-rolled cachedForce (e.g. an older TravelEncounter created before that field existed).</summary>
        public static BattleForce BanditCampForce(string displayName) => new()
        {
            name = displayName,
            faction = FactionId.ButterKlanBoys,
            troops = new List<TroopStack>
            {
                new() { troopId = "butter_thug", count = 10 },
                new() { troopId = "butter_raider", count = 4 }
            }
        };

        public static int ForceCount(BattleForce force)
        {
            var total = 0;
            foreach (var t in force.troops) total += t.count;
            return total;
        }

        public static BattleForce EncounterForce(TravelEncounterKind kind) => kind switch
        {
            TravelEncounterKind.MinorThieves => new BattleForce
            {
                name = "Footpads",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack> { new() { troopId = "butter_thug", count = 3 } }
            },
            TravelEncounterKind.BanditAmbush => new BattleForce
            {
                name = "Bandit ambush",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack>
                {
                    new() { troopId = "butter_thug", count = 5 },
                    new() { troopId = "butter_slinger", count = 2 }
                }
            },
            TravelEncounterKind.ButterRaid => new BattleForce
            {
                name = "Butter warband",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack>
                {
                    new() { troopId = "butter_raider", count = 5 },
                    new() { troopId = "butter_potthrower", count = 3 }
                }
            },
            _ => new BattleForce
            {
                name = "Hostiles",
                faction = FactionId.ButterKlanBoys,
                troops = new List<TroopStack> { new() { troopId = "butter_thug", count = 2 } }
            }
        };

        public void Begin(BattleForce player, BattleForce enemy, bool captureLord, string lordId, Action<BattleOutcome> onFinished, bool isCampRaid = false)
        {
            _onFinished = onFinished;
            _active = true;
            _selectedCommands.Clear();
            _selectedPower = null;
            EnsureUi();
            _canvas.gameObject.SetActive(true);
            GameState.Instance.Battle.Begin(player, enemy, captureLord, lordId, isCampRaid);
            _title.text = $"{player.name} vs {enemy.name}";
            _kuo.text = "Kuo: \"Orders by banner — infantry and mounts may move as one mind, or two.\"";
            _log.text = "Select one command per unit type you field, optional power card, then Commit Turn.";
            RefreshTurn();
        }

        void EnsureUi()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("BattleCanvas", 40);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, new Color(0.06f, 0.07f, 0.09f, 0.97f));

            _title = UiFactory.Label(root, "Title", "Battle", 34, TextAnchor.UpperCenter, new Color(0.93f, 0.86f, 0.7f));
            Stretch(_title, 0.05f, 0.92f, 0.95f, 0.99f);

            _visual = UiFactory.Label(root, "Visual", "", 22, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 0.82f));
            Stretch(_visual, 0.05f, 0.82f, 0.95f, 0.91f);

            _kuo = UiFactory.Label(root, "Kuo", "", 20, TextAnchor.UpperLeft, new Color(0.7f, 0.82f, 0.9f));
            Stretch(_kuo, 0.05f, 0.74f, 0.95f, 0.81f);

            _optionsRoot = UiFactory.Panel(root, "Options", new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.73f), new Color(0, 0, 0, 0.2f));

            _log = UiFactory.Label(root, "Log", "", 18, TextAnchor.LowerLeft, new Color(0.7f, 0.72f, 0.68f));
            Stretch(_log, 0.05f, 0.02f, 0.95f, 0.17f);
        }

        static void Stretch(Text t, float x0, float y0, float x1, float y1)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void RefreshTurn()
        {
            foreach (Transform child in _optionsRoot)
                Destroy(child.gameObject);

            var battle = GameState.Instance.Battle;
            if (battle.Phase == BattlePhase.Resolve)
            {
                Finish();
                return;
            }

            _visual.text = $"Turn {battle.Turn}/{BattleDirector.MaxTurns}\n{battle.VisualLine()}";
            _selectedCommands.Clear();
            _selectedPower = null;

            var y = 0.98f;
            UiFactory.Label(_optionsRoot, "CmdHeader", "COMMAND CARDS (pick one per unit type)", 18, TextAnchor.MiddleLeft, new Color(0.9f, 0.85f, 0.7f));
            var hdr = _optionsRoot.Find("CmdHeader") as RectTransform;
            if (hdr != null)
            {
                hdr.anchorMin = new Vector2(0f, y - 0.08f);
                hdr.anchorMax = new Vector2(1f, y);
            }

            y -= 0.09f;
            var commands = battle.AvailableCommands();
            foreach (var card in commands)
            {
                var id = card.id;
                var top = y;
                var bottom = y - 0.09f;
                y = bottom - 0.01f;
                var label = $"{card.displayName} [{card.forCategory}]\n{card.description}";
                UiFactory.Button(_optionsRoot, id, label, new Vector2(0f, bottom), new Vector2(1f, top), () =>
                {
                    ToggleCommand(id, card.forCategory);
                    RefreshSelectionLabels();
                });
            }

            y -= 0.02f;
            var ph = UiFactory.Label(_optionsRoot, "PowHeader", "POWER CARD (optional, once per battle each)", 18, TextAnchor.MiddleLeft, new Color(0.85f, 0.7f, 0.9f));
            var phr = ph.GetComponent<RectTransform>();
            phr.anchorMin = new Vector2(0f, y - 0.07f);
            phr.anchorMax = new Vector2(1f, y);
            y -= 0.08f;

            var powers = battle.OwnedPlayablePowers(GameState.Instance.Party);
            if (powers.Count == 0)
            {
                var empty = UiFactory.Label(_optionsRoot, "NoPow", "No power cards in your treatise pouch.", 16, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f));
                var er = empty.GetComponent<RectTransform>();
                er.anchorMin = new Vector2(0f, y - 0.07f);
                er.anchorMax = new Vector2(1f, y);
                y -= 0.08f;
            }
            else
            {
                foreach (var card in powers)
                {
                    var id = card.id;
                    var top = y;
                    var bottom = y - 0.09f;
                    y = bottom - 0.01f;
                    UiFactory.Button(_optionsRoot, id, $"{card.displayName}\n{card.description}", new Vector2(0f, bottom), new Vector2(1f, top), () =>
                    {
                        _selectedPower = _selectedPower == id ? null : id;
                        RefreshSelectionLabels();
                    });
                }
            }

            UiFactory.Button(_optionsRoot, "Commit", "Commit Turn", new Vector2(0.15f, 0.01f), new Vector2(0.85f, 0.1f), CommitTurn);
            RefreshSelectionLabels();
        }

        void ToggleCommand(string cardId, TroopCategory category)
        {
            var battle = GameState.Instance.Battle;
            var wasSelected = _selectedCommands.Contains(cardId);
            var toRemove = new List<string>();
            foreach (var id in _selectedCommands)
            {
                if (battle.TryGetCard(id, out var c) && c.forCategory == category)
                    toRemove.Add(id);
            }

            foreach (var id in toRemove)
                _selectedCommands.Remove(id);

            if (!wasSelected)
                _selectedCommands.Add(cardId);
        }

        void RefreshSelectionLabels()
        {
            foreach (Transform child in _optionsRoot)
            {
                var btn = child.GetComponent<Button>();
                if (btn == null) continue;
                var img = child.GetComponent<Image>();
                if (img == null) continue;
                var id = child.name;
                var selected = _selectedCommands.Contains(id) || _selectedPower == id || id == "Commit";
                if (id == "Commit")
                    img.color = new Color(0.25f, 0.4f, 0.28f, 0.95f);
                else if (selected)
                    img.color = new Color(0.45f, 0.38f, 0.18f, 0.95f);
                else
                    img.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
            }

            var picks = _selectedCommands.Count;
            _log.text = $"Selected commands: {picks} · Power: {(_selectedPower ?? "none")} · Tap Commit Turn when ready.";
        }

        void CommitTurn()
        {
            if (_selectedCommands.Count == 0)
            {
                _log.text = "Pick at least one command card.";
                return;
            }

            var powers = new List<string>();
            if (!string.IsNullOrEmpty(_selectedPower))
                powers.Add(_selectedPower);

            var commands = new List<string>(_selectedCommands);
            var text = GameState.Instance.Battle.ResolveTurn(commands, powers, GameState.Instance.Party, GameState.Instance.Rng);
            _log.text = text;
            _kuo.text = GameState.Instance.Battle.Phase == BattlePhase.Crisis
                ? "Kuo: \"The hour is late — wager only what you can lose.\""
                : "Kuo: \"Again — reshape the field.\"";

            if (GameState.Instance.Battle.Phase == BattlePhase.Resolve)
                Finish();
            else
                RefreshTurn();
        }

        /// <summary>Turns the battle's generic captures into recruit/ransom/release-able prisoners, tagged
        /// with the enemy's faction, up to the overall holding capacity. Named lords are handled elsewhere.</summary>
        static void TakeGenericPrisoners(BattleOutcome outcome)
        {
            if (outcome.capturedTroops == null || outcome.capturedTroops.Count == 0) return;
            var g = GameState.Instance;
            var enemyFaction = g.Battle.Enemy?.faction ?? FactionId.Bandits;
            var taken = 0;

            foreach (var cap in outcome.capturedTroops)
            {
                var name = g.TroopRoster != null && g.TroopRoster.TryGet(cap.troopId, out var def)
                    ? $"{def.displayName} Captive"
                    : "Captive";
                for (var i = 0; i < cap.count; i++)
                {
                    if (g.Party.prisoners.Count >= GameConstants.PrisonerCapacity) break;
                    g.Party.AddPrisoner(g.NextPrisonerId(), name, cap.troopId, isLord: false, sourceFaction: enemyFaction);
                    taken++;
                }
            }

            if (taken > 0)
                outcome.summary += $"\n{taken} captured — held for ransom, recruiting, or release (see Party ▸ Prisoners).";
        }

        void Finish()
        {
            if (!_active) return;
            _active = false;
            var outcome = GameState.Instance.Battle.Finalize(GameState.Instance.Rng);

            // Apply the exact troops the sim killed (weakest-first) rather than stripping the
            // biggest stacks blind. A share of each loss is merely wounded (recoverable), not dead —
            // more so when you win. Fall back to the old count-based removal only if the detailed
            // breakdown is somehow empty (e.g. an outcome from before this field existed).
            if (outcome.playerLossesByTroop != null && outcome.playerLossesByTroop.Count > 0)
            {
                var woundedFraction = (outcome.playerVictory
                    ? GameConstants.WoundedFractionVictory
                    : GameConstants.WoundedFractionDefeat) + CompanionBonuses.FieldMedicineBonus();
                woundedFraction = Mathf.Clamp01(woundedFraction);
                var totalWounded = 0;
                foreach (var loss in outcome.playerLossesByTroop)
                {
                    GameState.Instance.Party.RemoveTroops(loss.troopId, loss.count);
                    var wounded = Mathf.RoundToInt(loss.count * woundedFraction);
                    if (wounded > 0)
                    {
                        GameState.Instance.Party.AddWounded(loss.troopId, wounded);
                        totalWounded += wounded;
                    }
                }

                if (totalWounded > 0)
                    outcome.summary += $"\n{totalWounded} wounded — they'll mend if you keep them alive.";
            }
            else if (outcome.playerCasualties > 0)
            {
                GameState.Instance.Party.RemoveMen(outcome.playerCasualties);
            }

            if (outcome.goldLooted > 0)
                GameState.Instance.Party.gold += outcome.goldLooted;

            TakeGenericPrisoners(outcome);

            if (outcome.xpGained > 0)
                outcome.summary += $"\n+{outcome.xpGained} XP, +{outcome.warbandExperienceGained} Warband Experience";
            foreach (var levelUp in outcome.levelUpLogs)
                outcome.summary += $"\n{levelUp}";

            foreach (var cardId in outcome.rewardPowerCardIds)
            {
                GameState.Instance.Party.AddPowerCard(cardId);
                outcome.summary += $"\nSeized treatise: {cardId}";
            }

            foreach (var loot in outcome.loot)
            {
                if (loot.itemId is "grain" or "bread" or "fish" or "meat" or "smoked_meat" or "cheese" or "butter" or "dried_fruit")
                    GameState.Instance.Party.food.Add(loot);
                else
                    GameState.Instance.Party.inventory.Add(loot);
            }

            _visual.text = outcome.playerVictory ? "VICTORY" : "DEFEAT";
            _kuo.text = outcome.summary;
            foreach (Transform child in _optionsRoot)
                Destroy(child.gameObject);

            UiFactory.Button(_optionsRoot, "Continue", "Continue", new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f), () =>
            {
                _canvas.gameObject.SetActive(false);
                _onFinished?.Invoke(outcome);
            });
        }
    }
}
