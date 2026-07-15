using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Text battle with unit bars + command cards (multi per turn) + rare power cards.
    /// Strength is derived from each fielded troop's real stats (melee/ranged/armour/morale),
    /// not just category and count. Randomness and command swings are percentage-based so
    /// they scale with force size instead of being tuned to one specific strength range.
    /// </summary>
    public class BattleDirector
    {
        public const int MaxTurns = 4;
        public const int MaxPowerCardsPerTurn = 1;

        public BattlePhase Phase { get; private set; } = BattlePhase.Prep;
        public int Turn { get; private set; }
        public BattleForce Player { get; private set; }
        public BattleForce Enemy { get; private set; }
        public string Narrative { get; private set; } = "";

        readonly Dictionary<string, BattleCardDef> _catalog = new();
        readonly List<ActiveModifier> _modifiers = new();
        readonly List<string> _playedPowerThisBattle = new();

        bool _captureLordRequired;
        string _enemyLordId;
        float _playerStr;
        float _enemyStr;

        // troopId -> remaining count, the source of truth for a side mid-battle
        readonly Dictionary<string, int> _playerCounts = new();
        readonly Dictionary<string, int> _enemyCounts = new();

        // category -> count, derived from the dictionaries above for command availability + display
        readonly Dictionary<TroopCategory, int> _playerByCat = new();
        readonly Dictionary<TroopCategory, int> _enemyByCat = new();

        public IReadOnlyDictionary<TroopCategory, int> PlayerByCategory => _playerByCat;
        public IReadOnlyDictionary<TroopCategory, int> EnemyByCategory => _enemyByCat;
        public IReadOnlyList<string> PlayedPowerThisBattle => _playedPowerThisBattle;

        public void LoadCatalog(IEnumerable<BattleCardDef> cards)
        {
            _catalog.Clear();
            foreach (var c in cards)
                _catalog[c.id] = c;
        }

        public bool TryGetCard(string id, out BattleCardDef def) => _catalog.TryGetValue(id, out def);

        public IEnumerable<BattleCardDef> AllCards => _catalog.Values;

        public void Begin(BattleForce player, BattleForce enemy, bool captureLordRequired = false, string enemyLordId = null)
        {
            Player = player;
            Enemy = enemy;
            _captureLordRequired = captureLordRequired;
            _enemyLordId = enemyLordId;
            Turn = 1;
            Phase = BattlePhase.Opening;
            _modifiers.Clear();
            _playedPowerThisBattle.Clear();
            FillCounts(Player, _playerCounts);
            FillCounts(Enemy, _enemyCounts);
            RecomputeCategories(_playerCounts, _playerByCat);
            RecomputeCategories(_enemyCounts, _enemyByCat);
            _playerStr = Strength(_playerCounts);
            _enemyStr = Strength(_enemyCounts);
            Narrative = "Standards lift. Bangkok Kuo: \"First seek the shape of their line.\"";
        }

        public List<BattleCardDef> AvailableCommands()
        {
            var list = new List<BattleCardDef>();
            foreach (var card in _catalog.Values)
            {
                if (card.kind != BattleCardKind.Command) continue;
                if (!_playerByCat.TryGetValue(card.forCategory, out var n) || n <= 0) continue;
                list.Add(card);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return list;
        }

        public List<BattleCardDef> OwnedPlayablePowers(PartyState party)
        {
            var list = new List<BattleCardDef>();
            foreach (var stack in party.powerCards)
            {
                if (stack.count <= 0) continue;
                if (_playedPowerThisBattle.Contains(stack.itemId)) continue;
                if (!_catalog.TryGetValue(stack.itemId, out var def)) continue;
                if (def.kind != BattleCardKind.Power) continue;
                list.Add(def);
            }

            return list;
        }

        public string ResolveTurn(IList<string> commandCardIds, IList<string> powerCardIds, PartyState party, System.Random rng)
        {
            if (Phase == BattlePhase.Resolve)
                return "Battle already over.";

            var usedCats = new HashSet<TroopCategory>();
            var commandSwingPct = 0f;
            var lines = new List<string> { $"— Turn {Turn} —" };

            foreach (var id in commandCardIds)
            {
                if (!_catalog.TryGetValue(id, out var card) || card.kind != BattleCardKind.Command)
                    continue;
                if (!_playerByCat.TryGetValue(card.forCategory, out var n) || n <= 0)
                    continue;
                if (!usedCats.Add(card.forCategory))
                {
                    lines.Add($"Ignored duplicate order for {card.forCategory}.");
                    continue;
                }

                var effect = EvaluateCommand(card, rng);
                commandSwingPct += effect.swingPct;
                lines.Add(effect.text);
            }

            var powersPlayed = 0;
            foreach (var id in powerCardIds)
            {
                if (powersPlayed >= MaxPowerCardsPerTurn) break;
                if (_playedPowerThisBattle.Contains(id)) continue;
                if (!_catalog.TryGetValue(id, out var card) || card.kind != BattleCardKind.Power) continue;
                if (!party.HasPowerCard(id)) continue;

                ApplyPower(card, lines);
                _playedPowerThisBattle.Add(id);
                powersPlayed++;
            }

            var pMul = 1f + SumPlayerAttackMod() + commandSwingPct;
            var eMul = 1f + SumEnemyAttackMod();
            var p = _playerStr * Mathf.Max(0.1f, pMul);
            var e = _enemyStr * Mathf.Max(0.1f, eMul);
            var chaosPct = (float)(rng.NextDouble() * 0.24 - 0.12); // +-12% of the average force strength
            var chaos = chaosPct * (p + e) * 0.5f;
            var delta = p - e + chaos;
            var deltaPct = delta / Mathf.Max(1f, (p + e) * 0.5f);

            int playerLoss;
            int enemyLoss;
            if (deltaPct >= 0.15f)
            {
                enemyLoss = Math.Max(2, rng.Next(2, 5));
                playerLoss = rng.Next(0, 2);
                lines.Add($"Clash favours you (swing {deltaPct:+0%;-0%}). Enemy recoils.");
            }
            else if (deltaPct <= -0.15f)
            {
                playerLoss = Math.Max(2, rng.Next(2, 5));
                enemyLoss = rng.Next(0, 2);
                lines.Add($"Clash goes badly (swing {deltaPct:+0%;-0%}). Your line buckles.");
            }
            else
            {
                playerLoss = rng.Next(1, 3);
                enemyLoss = rng.Next(1, 3);
                lines.Add($"Grinding melee (swing {deltaPct:+0%;-0%}). Blood for both banners.");
            }

            ApplyLosses(_playerCounts, playerLoss);
            ApplyLosses(_enemyCounts, enemyLoss);
            RecomputeCategories(_playerCounts, _playerByCat);
            RecomputeCategories(_enemyCounts, _enemyByCat);
            _playerStr = Strength(_playerCounts);
            _enemyStr = Strength(_enemyCounts);
            lines.Add(VisualLine());

            Narrative = string.Join("\n", lines);
            Turn++;

            if (_enemyStr <= 0 || _playerStr <= 0 || Turn > MaxTurns)
                Phase = BattlePhase.Resolve;
            else
                Phase = Turn <= 2 ? BattlePhase.Mid : BattlePhase.Crisis;

            return Narrative;
        }

        public BattleOutcome Finalize(System.Random rng)
        {
            Phase = BattlePhase.Resolve;
            var playerWins = _enemyStr <= 0 || (_playerStr > 0 && _playerStr >= _enemyStr);

            var outcome = new BattleOutcome
            {
                playerVictory = playerWins,
                playerCasualties = Math.Max(0, StrengthFromForce(Player) - TotalCount(_playerCounts)),
                enemyCasualties = Math.Max(0, StrengthFromForce(Enemy) - TotalCount(_enemyCounts)),
                summary = playerWins
                    ? "Enemy breaks. Captives and loot in the mud."
                    : "You are forced back. Rally what men remain."
            };

            if (playerWins)
            {
                outcome.loot.Add(new InventoryStack { itemId = "loot_butter_trinket", count = 1 });
                outcome.loot.Add(new InventoryStack { itemId = "grain", count = 2 });
                if (_captureLordRequired && !string.IsNullOrEmpty(_enemyLordId))
                {
                    outcome.capturedLordIds.Add(_enemyLordId);
                    outcome.rewardPowerCardIds.Add("card_butter_grease_curse");
                }
            }

            return outcome;
        }

        public BattleOutcome Resolve(System.Random rng) => Finalize(rng);

        void FillCounts(BattleForce force, Dictionary<string, int> counts)
        {
            counts.Clear();
            foreach (var stack in force.troops)
            {
                if (stack.count <= 0) continue;
                counts.TryGetValue(stack.troopId, out var cur);
                counts[stack.troopId] = cur + stack.count;
            }
        }

        static void RecomputeCategories(Dictionary<string, int> counts, Dictionary<TroopCategory, int> byCat)
        {
            byCat.Clear();
            foreach (var kv in counts)
            {
                var cat = CategoryOf(kv.Key);
                byCat.TryGetValue(cat, out var cur);
                byCat[cat] = cur + kv.Value;
            }
        }

        static TroopCategory CategoryOf(string troopId) =>
            GameState.Instance != null && GameState.Instance.TroopRoster != null &&
            GameState.Instance.TroopRoster.TryGet(troopId, out var def)
                ? def.category
                : TroopCategory.Infantry;

        /// <summary>
        /// Real per-unit combat power from troops.json stats, scaled by morale.
        /// Falls back to a flat estimate only if the troop id isn't in the roster.
        /// </summary>
        static float TroopPower(string troopId)
        {
            if (GameState.Instance != null && GameState.Instance.TroopRoster != null &&
                GameState.Instance.TroopRoster.TryGet(troopId, out var def))
            {
                var moraleMul = Mathf.Max(0.4f, def.morale / 50f);
                return (def.melee + def.ranged + def.armour) * moraleMul;
            }

            return 15f;
        }

        static float Strength(Dictionary<string, int> counts)
        {
            var s = 0f;
            foreach (var kv in counts)
                s += TroopPower(kv.Key) * kv.Value;
            return s;
        }

        static int TotalCount(Dictionary<string, int> counts)
        {
            var n = 0;
            foreach (var kv in counts) n += kv.Value;
            return n;
        }

        static int StrengthFromForce(BattleForce force)
        {
            var n = 0;
            foreach (var t in force.troops) n += t.count;
            return n;
        }

        public string VisualLine()
        {
            return $"You [{Bars(_playerByCat)}]  str {_playerStr:0}   |   Enemy [{Bars(_enemyByCat)}]  str {_enemyStr:0}";
        }

        static string Bars(Dictionary<TroopCategory, int> map)
        {
            string Slice(TroopCategory c, string tag)
            {
                map.TryGetValue(c, out var n);
                if (n <= 0) return "";
                return $"{tag}:{n} ";
            }

            return (Slice(TroopCategory.Infantry, "Inf") +
                    Slice(TroopCategory.Spear, "Spr") +
                    Slice(TroopCategory.Archer, "Bow") +
                    Slice(TroopCategory.Crossbow, "Xbow") +
                    Slice(TroopCategory.Mounted, "Cav") +
                    Slice(TroopCategory.Specialty, "Elt")).Trim();
        }

        /// <summary>Drains casualties from the weakest-per-unit stacks first, so elite troops survive longer.</summary>
        static void ApplyLosses(Dictionary<string, int> counts, int lossMen)
        {
            var remaining = lossMen;
            var keys = new List<string>(counts.Keys);
            keys.Sort((a, b) => TroopPower(a).CompareTo(TroopPower(b)));
            foreach (var k in keys)
            {
                if (remaining <= 0) break;
                var take = Math.Min(counts[k], remaining);
                counts[k] -= take;
                remaining -= take;
                if (counts[k] <= 0) counts.Remove(k);
            }
        }

        (float swingPct, string text) EvaluateCommand(BattleCardDef card, System.Random rng)
        {
            var jitter = rng.Next(-2, 3) * 0.01f;
            if (card.id == "cmd_inf_slow")
                return (0.08f + jitter, $"{card.displayName}: {card.forCategory} close the gap without breaking step.");

            return card.order switch
            {
                UnitOrder.Hold or UnitOrder.Shieldwall or UnitOrder.Brace =>
                    (0.06f + jitter, $"{card.displayName}: {card.forCategory} dig in."),
                UnitOrder.Charge or UnitOrder.Push =>
                    (0.12f + jitter, $"{card.displayName}: {card.forCategory} commit hard."),
                UnitOrder.Flank =>
                    (0.14f + jitter, $"{card.displayName}: horse seeks the edge."),
                UnitOrder.LooseVolley or UnitOrder.FocusFire =>
                    (0.10f + jitter, $"{card.displayName}: missiles streak."),
                UnitOrder.FallBack or UnitOrder.Screen =>
                    (0.04f + jitter, $"{card.displayName}: space bought with mud."),
                UnitOrder.Pursue =>
                    (0.08f + jitter, $"{card.displayName}: they press the flee."),
                _ => (0.06f + jitter, $"{card.displayName} played.")
            };
        }

        void ApplyPower(BattleCardDef card, List<string> lines)
        {
            _modifiers.Add(new ActiveModifier
            {
                effect = card.effect,
                category = card.targetCategory,
                magnitude = card.magnitude
            });
            lines.Add($"POWER — {card.displayName}: {card.description}");
        }

        float SumEnemyAttackMod()
        {
            float m = 0f;
            foreach (var mod in _modifiers)
            {
                if (mod.effect is PowerEffectId.EnemyCategoryAttackDown or PowerEffectId.EnemyCategoryAccuracyDown)
                    m -= mod.magnitude;
                if (mod.effect == PowerEffectId.EnemyMoraleDown)
                    m -= mod.magnitude * 0.5f;
            }

            return m;
        }

        float SumPlayerAttackMod()
        {
            float m = 0f;
            foreach (var mod in _modifiers)
            {
                if (mod.effect == PowerEffectId.PlayerCategoryAttackUp)
                    m += mod.magnitude;
                if (mod.effect == PowerEffectId.PlayerMoraleUp)
                    m += mod.magnitude * 0.5f;
                if (mod.effect == PowerEffectId.RevealWeakness)
                    m += mod.magnitude;
            }

            return m;
        }

        class ActiveModifier
        {
            public PowerEffectId effect;
            public TroopCategory category;
            public float magnitude;
        }
    }

    [Serializable]
    public class BattleForce
    {
        public string name;
        public List<TroopStack> troops = new();
    }

    [Serializable]
    public class BattleOutcome
    {
        public bool playerVictory;
        public string summary;
        public List<InventoryStack> loot = new();
        public List<string> capturedLordIds = new();
        public List<string> rewardPowerCardIds = new();
        public int playerCasualties;
        public int enemyCasualties;
    }
}
