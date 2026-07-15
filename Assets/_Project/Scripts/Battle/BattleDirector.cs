using System;
using System.Collections.Generic;

namespace Voidovia
{
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
        public string sunTzuAside; // Bangkok Kuo flavour
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
        public int playerCasualties;
        public int enemyCasualties;
    }

    /// <summary>
    /// Football Manager mobile cadence: prep, then decision cards every phase.
    /// </summary>
    public class BattleDirector
    {
        public BattlePhase Phase { get; private set; } = BattlePhase.Prep;
        public int DecisionIndex { get; private set; }

        readonly List<BattleDecision> _script = new();
        BattleForce _player;
        BattleForce _enemy;
        bool _captureLordRequired;
        string _enemyLordId;

        public void Begin(BattleForce player, BattleForce enemy, bool captureLordRequired = false, string enemyLordId = null)
        {
            _player = player;
            _enemy = enemy;
            _captureLordRequired = captureLordRequired;
            _enemyLordId = enemyLordId;
            Phase = BattlePhase.Prep;
            DecisionIndex = 0;
            _script.Clear();
            BuildDefaultScript();
        }

        public BattleDecision CurrentDecision()
        {
            if (DecisionIndex < 0 || DecisionIndex >= _script.Count)
                return null;
            return _script[DecisionIndex];
        }

        public bool ApplyOrder(UnitOrder order, out string beatLog)
        {
            var decision = CurrentDecision();
            if (decision == null)
            {
                beatLog = "Battle already resolved.";
                return false;
            }

            beatLog = DescribeOrder(decision.targetCategory, order);
            DecisionIndex++;

            if (DecisionIndex >= _script.Count)
            {
                Phase = BattlePhase.Resolve;
                return true;
            }

            Phase = DecisionIndex switch
            {
                1 => BattlePhase.Opening,
                2 or 3 => BattlePhase.Mid,
                _ => BattlePhase.Crisis
            };
            return true;
        }

        public BattleOutcome Resolve(System.Random rng)
        {
            // Placeholder resolution — refine with real strength math later.
            var playerStrength = Estimate(_player);
            var enemyStrength = Estimate(_enemy);
            var swing = rng.Next(-8, 9);
            var playerWins = playerStrength + swing >= enemyStrength;

            var outcome = new BattleOutcome
            {
                playerVictory = playerWins,
                playerCasualties = Math.Max(0, _player.troops.Count > 0 ? rng.Next(0, 3) : 0),
                enemyCasualties = playerWins ? rng.Next(3, 10) : rng.Next(0, 4),
                summary = playerWins
                    ? "The line holds. Enemy breaks."
                    : "Your men buckle. Sound retreat."
            };

            if (playerWins)
            {
                outcome.loot.Add(new InventoryStack { itemId = "loot_butter_trinket", count = 1 });
                outcome.loot.Add(new InventoryStack { itemId = "grain", count = 2 });
                if (_captureLordRequired && !string.IsNullOrEmpty(_enemyLordId))
                    outcome.capturedLordIds.Add(_enemyLordId);
            }

            return outcome;
        }

        void BuildDefaultScript()
        {
            _script.Add(new BattleDecision
            {
                prompt = "Opening clash — where do the foot press?",
                sunTzuAside = "Kuo: \"Appear weak when you are strong — but do not actually be weak.\"",
                targetCategory = TroopCategory.Infantry,
                options = new[] { UnitOrder.Hold, UnitOrder.Push, UnitOrder.Shieldwall }
            });
            _script.Add(new BattleDecision
            {
                prompt = "Missile screen — how do bows answer?",
                sunTzuAside = "Kuo: \"Let your plans be dark and impenetrable as night.\"",
                targetCategory = TroopCategory.Archer,
                options = new[] { UnitOrder.LooseVolley, UnitOrder.FocusFire, UnitOrder.ConserveAmmo }
            });
            _script.Add(new BattleDecision
            {
                prompt = "Spears face a tip of riders.",
                sunTzuAside = "Kuo: \"The opportunity of defeating the enemy is provided by the enemy himself.\"",
                targetCategory = TroopCategory.Spear,
                options = new[] { UnitOrder.Brace, UnitOrder.Hold, UnitOrder.FallBack }
            });
            _script.Add(new BattleDecision
            {
                prompt = "Crisis — commit horse or keep the reserve?",
                sunTzuAside = "Kuo: \"In the midst of chaos, there is also opportunity.\"",
                targetCategory = TroopCategory.Mounted,
                options = new[] { UnitOrder.Flank, UnitOrder.Charge, UnitOrder.Screen }
            });
            _script.Add(new BattleDecision
            {
                prompt = "Enemy standard wavers. Finish or secure captives?",
                sunTzuAside = "Kuo: \"Victorious warriors win first and then go to war.\"",
                targetCategory = TroopCategory.Infantry,
                options = new[] { UnitOrder.Push, UnitOrder.Pursue, UnitOrder.Hold }
            });
        }

        static int Estimate(BattleForce force)
        {
            var n = 0;
            foreach (var s in force.troops)
                n += s.count * 3;
            return n;
        }

        static string DescribeOrder(TroopCategory cat, UnitOrder order) =>
            $"{cat}: {order}";
    }
}
