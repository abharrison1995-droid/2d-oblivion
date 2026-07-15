using System;
using System.Collections.Generic;

namespace Voidovia
{
    /// <summary>
    /// Text battle with unit bars + command cards (multi per turn) + rare power cards.
    /// Randomness lives in turn resolution.
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
        int _playerStr;
        int _enemyStr;
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
            Recount();
            _playerStr = Strength(_playerByCat);
            _enemyStr = Strength(_enemyByCat);
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
            var commandSwing = 0f;
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
                commandSwing += effect.swing;
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

            var pMul = 1f + SumPlayerAttackMod();
            var eMul = 1f + SumEnemyAttackMod();
            var p = _playerStr * pMul + commandSwing;
            var e = _enemyStr * eMul;
            var chaos = rng.Next(-12, 13);
            var delta = p - e + chaos;

            int playerLoss;
            int enemyLoss;
            if (delta >= 8)
            {
                enemyLoss = Math.Max(2, rng.Next(2, 5));
                playerLoss = rng.Next(0, 2);
                lines.Add($"Clash favours you (swing {delta:+0;-0}). Enemy recoils.");
            }
            else if (delta <= -8)
            {
                playerLoss = Math.Max(2, rng.Next(2, 5));
                enemyLoss = rng.Next(0, 2);
                lines.Add($"Clash goes badly (swing {delta:+0;-0}). Your line buckles.");
            }
            else
            {
                playerLoss = rng.Next(1, 3);
                enemyLoss = rng.Next(1, 3);
                lines.Add($"Grinding melee (swing {delta:+0;-0}). Blood for both banners.");
            }

            ApplyLosses(_playerByCat, ref _playerStr, playerLoss);
            ApplyLosses(_enemyByCat, ref _enemyStr, enemyLoss);
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
                playerCasualties = Math.Max(0, StrengthFromForce(Player) - Math.Max(1, _playerStr / 3)),
                enemyCasualties = Math.Max(0, StrengthFromForce(Enemy) - Math.Max(1, _enemyStr / 3)),
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

        public int DecisionIndex => Turn;
        public BattleDecision CurrentDecision() => null;

        public bool ApplyOrder(UnitOrder order, out string beatLog)
        {
            beatLog = "Use card battle ResolveTurn.";
            return false;
        }

        public BattleOutcome Resolve(System.Random rng) => Finalize(rng);

        void Recount()
        {
            FillCats(Player, _playerByCat);
            FillCats(Enemy, _enemyByCat);
        }

        void FillCats(BattleForce force, Dictionary<TroopCategory, int> map)
        {
            map.Clear();
            if (GameState.Instance?.TroopRoster?.troops == null)
            {
                foreach (var t in force.troops)
                    AddCat(map, TroopCategory.Infantry, t.count);
                return;
            }

            foreach (var stack in force.troops)
            {
                var cat = TroopCategory.Infantry;
                foreach (var def in GameState.Instance.TroopRoster.troops)
                {
                    if (def.id != stack.troopId) continue;
                    cat = def.category;
                    break;
                }

                AddCat(map, cat, stack.count);
            }
        }

        static void AddCat(Dictionary<TroopCategory, int> map, TroopCategory cat, int n)
        {
            map.TryGetValue(cat, out var cur);
            map[cat] = cur + n;
        }

        static int Strength(Dictionary<TroopCategory, int> map)
        {
            var s = 0;
            foreach (var kv in map)
            {
                var mult = kv.Key switch
                {
                    TroopCategory.Mounted => 4,
                    TroopCategory.Specialty => 5,
                    TroopCategory.Archer or TroopCategory.Crossbow => 3,
                    TroopCategory.Spear => 3,
                    _ => 3
                };
                s += kv.Value * mult;
            }

            return s;
        }

        static int StrengthFromForce(BattleForce force)
        {
            var n = 0;
            foreach (var t in force.troops) n += t.count;
            return n;
        }

        public string VisualLine()
        {
            return $"You [{Bars(_playerByCat)}]  str {_playerStr}   |   Enemy [{Bars(_enemyByCat)}]  str {_enemyStr}";
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

        static void ApplyLosses(Dictionary<TroopCategory, int> map, ref int str, int lossMen)
        {
            var remaining = lossMen;
            var keys = new List<TroopCategory>(map.Keys);
            keys.Sort((a, b) => map[b].CompareTo(map[a]));
            foreach (var k in keys)
            {
                if (remaining <= 0) break;
                var take = Math.Min(map[k], remaining);
                map[k] -= take;
                remaining -= take;
                if (map[k] <= 0) map.Remove(k);
            }

            str = Strength(map);
        }

        (float swing, string text) EvaluateCommand(BattleCardDef card, System.Random rng)
        {
            var jitter = rng.Next(-2, 3);
            if (card.id == "cmd_inf_slow")
                return (4 + jitter, $"{card.displayName}: {card.forCategory} close the gap without breaking step.");

            return card.order switch
            {
                UnitOrder.Hold or UnitOrder.Shieldwall or UnitOrder.Brace =>
                    (3 + jitter, $"{card.displayName}: {card.forCategory} dig in."),
                UnitOrder.Charge or UnitOrder.Push =>
                    (6 + jitter, $"{card.displayName}: {card.forCategory} commit hard."),
                UnitOrder.Flank =>
                    (7 + jitter, $"{card.displayName}: horse seeks the edge."),
                UnitOrder.LooseVolley or UnitOrder.FocusFire =>
                    (5 + jitter, $"{card.displayName}: missiles streak."),
                UnitOrder.FallBack or UnitOrder.Screen =>
                    (2 + jitter, $"{card.displayName}: space bought with mud."),
                UnitOrder.Pursue =>
                    (4 + jitter, $"{card.displayName}: they press the flee."),
                _ => (3 + jitter, $"{card.displayName} played.")
            };
        }

        void ApplyPower(BattleCardDef card, List<string> lines)
        {
            _modifiers.Add(new ActiveModifier
            {
                effect = card.effect,
                category = card.targetCategory,
                magnitude = card.magnitude,
                turnsLeft = 99
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
            public int turnsLeft;
        }
    }

    [Serializable]
    public class BattleForce
    {
        public string name;
        public List<TroopStack> troops = new();
    }

    [Serializable]
    public class BattleDecision
    {
        public string prompt;
        public string sunTzuAside;
        public TroopCategory targetCategory;
        public UnitOrder[] options;
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
