using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Gold / food / wages strip opener + dual inventory/equipment + power-card pouch submenu.
    /// </summary>
    public class PartyPanelUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _header;
        Text _body;
        RectTransform _list;
        bool _pouchMode;
        bool _prisonersMode;
        bool _troopsMode;
        bool _heroMode;
        bool _companionsMode;
        System.Action _onClose;

        public void Open(System.Action onClose = null, string initialTab = null)
        {
            _onClose = onClose;
            _pouchMode = initialTab == "pouch";
            _prisonersMode = initialTab == "prisoners";
            _troopsMode = initialTab == "troops";
            _heroMode = initialTab == "hero";
            _companionsMode = initialTab == "companions";
            Ensure();
            _canvas.gameObject.SetActive(true);
            Rebuild();
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("PartyPanelCanvas", 32);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, UiFactory.Theme.PanelBackground);
            _header = UiFactory.Label(root, "Header", "", 28, TextAnchor.UpperLeft, UiFactory.Theme.TextTitle);
            Stretch(_header, 0.05f, 0.88f, 0.95f, 0.98f);
            _body = UiFactory.Label(root, "Body", "", 22, TextAnchor.UpperLeft, UiFactory.Theme.TextDim);
            Stretch(_body, 0.05f, 0.74f, 0.95f, 0.87f);
            _list = UiFactory.Panel(root, "List", new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.72f), new Color(0, 0, 0, 0.18f));

            UiFactory.Button(root, "Hero", "Hero", new Vector2(0.05f, 0.02f), new Vector2(0.1772f, 0.11f), () =>
            {
                _heroMode = !_heroMode;
                _troopsMode = false;
                _pouchMode = false;
                _prisonersMode = false;
                _companionsMode = false;
                Rebuild();
            });
            UiFactory.Button(root, "Troops", "Troops", new Vector2(0.1972f, 0.02f), new Vector2(0.3244f, 0.11f), () =>
            {
                _troopsMode = !_troopsMode;
                _heroMode = false;
                _pouchMode = false;
                _prisonersMode = false;
                _companionsMode = false;
                Rebuild();
            });
            UiFactory.Button(root, "Pouch", "Pouch", new Vector2(0.3444f, 0.02f), new Vector2(0.4716f, 0.11f), () =>
            {
                _pouchMode = !_pouchMode;
                _heroMode = false;
                _troopsMode = false;
                _prisonersMode = false;
                _companionsMode = false;
                Rebuild();
            });
            UiFactory.Button(root, "Prisn", "Prisn", new Vector2(0.4916f, 0.02f), new Vector2(0.6188f, 0.11f), () =>
            {
                _prisonersMode = !_prisonersMode;
                _heroMode = false;
                _troopsMode = false;
                _pouchMode = false;
                _companionsMode = false;
                Rebuild();
            });
            UiFactory.Button(root, "Comp", "Comp", new Vector2(0.6388f, 0.02f), new Vector2(0.766f, 0.11f), () =>
            {
                _companionsMode = !_companionsMode;
                _heroMode = false;
                _troopsMode = false;
                _pouchMode = false;
                _prisonersMode = false;
                Rebuild();
            });
            UiFactory.Button(root, "Close", "Close", new Vector2(0.786f, 0.02f), new Vector2(0.95f, 0.11f), () =>
            {
                _canvas.gameObject.SetActive(false);
                _onClose?.Invoke();
            });
        }

        static void Stretch(Text t, float x0, float y0, float x1, float y1)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
        }

        void Rebuild()
        {
            foreach (Transform c in _list) Destroy(c.gameObject);
            var g = GameState.Instance;
            var p = g.Party;
            var foodNeed = g.Economy.DailyFoodNeed(p);
            var foodFill = 0f;
            foreach (var f in p.food)
            {
                // rough fill
                foodFill += f.count;
            }

            var daysLeft = foodNeed > 0.01f ? foodFill / foodNeed : 99f;
            _header.text = $"{g.Hero.name} · Lv{g.Hero.level} · {p.gold}g · WE {p.warbandExperience} · Food ~{daysLeft:0.0}d · {p.TotalMen} men";
            _body.text =
                $"Equipped weapon: {p.equippedWeaponId ?? "(none)"}\n" +
                $"Equipped armour: {p.equippedArmourId ?? "(none)"}\n" +
                (_heroMode ? "HERO — spend skill points"
                    : _troopsMode ? "TROOPS — train up when you can afford it"
                    : _prisonersMode ? "PRISONERS"
                    : _companionsMode ? "COMPANIONS"
                    : _pouchMode ? "POWER-CARD POUCH" : "INVENTORY — tap gear to equip");

            var y = 0.95f;
            if (_heroMode)
            {
                RebuildHero(g, ref y);
                return;
            }

            if (_troopsMode)
            {
                RebuildTroops(g, p, ref y);
                return;
            }

            if (_prisonersMode)
            {
                RebuildPrisoners(g, p, ref y);
                return;
            }

            if (_companionsMode)
            {
                RebuildCompanions(g, p, ref y);
                return;
            }

            if (_pouchMode)
            {
                if (p.powerCards.Count == 0)
                {
                    PlaceLabel("No treatises yet. Book stores, quests, and chiefs.", ref y);
                    return;
                }

                foreach (var c in p.powerCards)
                {
                    var name = c.itemId;
                    if (g.Battle.TryGetCard(c.itemId, out var def))
                        name = $"{def.displayName} — {def.description}";
                    PlaceLabel($"{name}  ×{c.count}", ref y);
                }

                return;
            }

            foreach (var stack in p.inventory)
            {
                var id = stack.itemId;
                var label = $"{id} ×{stack.count}";
                EquipSlot slot = EquipSlot.None;
                if (g.Market.TryGetItem(id, out var item))
                {
                    label = $"{item.displayName} ×{stack.count} [{item.quality}]";
                    slot = item.equipSlot;
                }

                var (top, bottom) = UiFactory.NextRow(ref y, 0.12f, 0.02f);
                if (slot != EquipSlot.None)
                {
                    var s = slot;
                    UiFactory.Button(_list, id, $"Equip: {label}", new Vector2(0f, bottom), new Vector2(1f, top), () =>
                    {
                        if (p.TryEquip(id, s, out _))
                            _body.text = $"Equipped {id}.";
                        Rebuild();
                    }, UiFactory.ButtonStyle.Primary);
                }
                else
                {
                    PlaceLabel(label, ref y, top, bottom);
                }
            }

            if (p.inventory.Count == 0)
                PlaceLabel("Bags empty aside from rations (food is separate).", ref y);
        }

        void RebuildHero(GameState g, ref float y)
        {
            var hero = g.Hero;
            PlaceLabel($"XP: {hero.xp}/{hero.XpForNextLevel()} to level {hero.level + 1}", ref y);
            PlaceLabel($"Unspent skill points: {hero.unspentSkillPoints}", ref y);
            PlaceLabel($"Warband Experience rank: {hero.warbandExperienceRank} (+{hero.WarbandExperienceDailyGain()}/day)", ref y);
            PlaceLabel($"Party: {g.PartyHeadcount}/{g.MaxPartySize} (Leadership sets the cap; +{GameConstants.PartySizePerLeadership} men per point)", ref y);

            StatRow("Combat", hero.combat, () => hero.combat++, hero, ref y);
            StatRow("Leadership", hero.leadership, () => hero.leadership++, hero, ref y);
            StatRow("Tactics", hero.tactics, () => hero.tactics++, hero, ref y);
            StatRow("Trade", hero.trade, () => hero.trade++, hero, ref y);
            StatRow("Scouting", hero.scouting, () => hero.scouting++, hero, ref y);
            StatRow("Warband Exp. rank", hero.warbandExperienceRank, () => hero.warbandExperienceRank++, hero, ref y);
        }

        void StatRow(string label, int value, System.Action apply, CharacterStats hero, ref float y)
        {
            var (top, bottom) = UiFactory.NextRow(ref y, 0.1f, 0.015f);

            var btn = UiFactory.Button(_list, label + "_stat", $"{label}: {value}  (+1)", new Vector2(0f, bottom), new Vector2(1f, top), () =>
            {
                if (hero.unspentSkillPoints <= 0) return;
                hero.unspentSkillPoints--;
                apply();
                Rebuild();
            }, UiFactory.ButtonStyle.Primary);
            btn.interactable = hero.unspentSkillPoints > 0;
        }

        void RebuildTroops(GameState g, PartyState p, ref float y)
        {
            if (p.trainingJobs.Count > 0)
            {
                foreach (var job in p.trainingJobs)
                {
                    var sourceName = g.TroopRoster.TryGet(job.sourceTroopId, out var srcDef) ? srcDef.displayName : job.sourceTroopId;
                    var targetName = g.TroopRoster.TryGet(job.targetTroopId, out var tgtDef) ? tgtDef.displayName : job.targetTroopId;
                    var daysLeft = Mathf.Max(0, job.completesOnDay - p.day);
                    PlaceLabel($"Training: {job.count}× {sourceName} → {targetName} (ready in {daysLeft}d)", ref y);
                }
            }

            if (p.wounded.Count > 0)
            {
                PlaceLabel($"— Wounded ({p.TotalWounded}) — recovering, faster resting in a settlement —", ref y);
                foreach (var stack in new List<TroopStack>(p.wounded))
                {
                    var name = g.TroopRoster.TryGet(stack.troopId, out var wdef) ? wdef.displayName : stack.troopId;
                    PlaceLabel($"  {name} ×{stack.count} (wounded)", ref y);
                }
            }

            if (p.troops.Count == 0)
            {
                PlaceLabel(p.wounded.Count > 0
                    ? "No fit troops — only wounded remain. Rest and let them mend."
                    : "No troops in the party.", ref y);
                return;
            }

            foreach (var stack in new List<TroopStack>(p.troops))
            {
                if (!g.TroopRoster.TryGet(stack.troopId, out var def))
                    continue;

                PlaceLabel($"{def.displayName} ×{stack.count} — T{def.tier}", ref y);

                if (string.IsNullOrEmpty(def.upgradesToId))
                    continue;

                var troopId = stack.troopId;

                var (top, bottom) = UiFactory.NextRow(ref y);
                var counts = new[] { 1, 5, 10 };
                var trainBtns = UiFactory.ButtonRow(_list, troopId + "_train", top, bottom, new (string, System.Action, UiFactory.ButtonStyle)[]
                {
                    ($"Train 1 ({def.upgradeGold}g)", () => PerformTrain(g, troopId, 1), UiFactory.ButtonStyle.Primary),
                    ($"Train 5 ({def.upgradeGold * 5}g)", () => PerformTrain(g, troopId, 5), UiFactory.ButtonStyle.Primary),
                    ($"Train 10 ({def.upgradeGold * 10}g)", () => PerformTrain(g, troopId, 10), UiFactory.ButtonStyle.Primary),
                });
                for (var i = 0; i < counts.Length; i++)
                {
                    var cost = def.upgradeGold * counts[i];
                    trainBtns[i].interactable = stack.count >= counts[i] && g.Party.gold >= cost && g.Party.warbandExperience >= cost;
                }

                var (top2, bottom2) = UiFactory.NextRow(ref y, 0.09f, 0.02f);
                var input = UiFactory.Input(_list, troopId + "_qty", "qty", new Vector2(0f, bottom2), new Vector2(0.28f, top2));
                UiFactory.Button(_list, troopId + "_trainX", $"Train X ({def.upgradeGold}g+WE/unit)", new Vector2(0.3f, bottom2), new Vector2(1f, top2), () =>
                {
                    if (!int.TryParse(input.text, out var n) || n <= 0) n = 1;
                    PerformTrain(g, troopId, n);
                }, UiFactory.ButtonStyle.Primary);
            }
        }

        void PerformTrain(GameState g, string troopId, int count)
        {
            g.TryStartTraining(troopId, count, out var log);
            _body.text = log;
            Rebuild();
        }

        void RebuildPrisoners(GameState g, PartyState p, ref float y)
        {
            if (p.prisoners.Count == 0)
            {
                PlaceLabel("No prisoners held.", ref y);
                return;
            }

            var atTavern = g.Map.TryGetNode(p.currentNodeId, out var here) && here.hasTavern;

            foreach (var prisoner in new List<PrisonerRecord>(p.prisoners))
            {
                var daysHeld = p.day - prisoner.capturedDay;
                var canRecruit = !prisoner.isLord && !string.IsNullOrEmpty(prisoner.recruitTroopId) &&
                                  daysHeld >= GameConstants.PrisonerRecruitDelayDays;
                var status = prisoner.isLord
                    ? "held for delivery to Lord Void"
                    : canRecruit
                        ? "trusts you enough to recruit"
                        : $"settling in ({Mathf.Max(0, GameConstants.PrisonerRecruitDelayDays - daysHeld)}d to recruit)";
                PlaceLabel($"{prisoner.displayName} — {status}", ref y);

                if (prisoner.isLord)
                    continue;

                var id = prisoner.id;
                var name = prisoner.displayName;
                var troopId = prisoner.recruitTroopId;
                var faction = prisoner.sourceFaction;
                var willRansom = p.GetRelation(faction) >= GameConstants.RansomMinRelationThreshold;
                var ransomPrice = RansomPrice(prisoner, p, g);

                var (top, bottom) = UiFactory.NextRow(ref y, 0.1f, 0.015f);
                var btns = UiFactory.ButtonRow(_list, id, top, bottom, new (string, System.Action, UiFactory.ButtonStyle)[]
                {
                    ("Recruit", () =>
                    {
                        if (!g.CanRecruit(1, out var capReason))
                        {
                            _body.text = capReason;
                            return;
                        }

                        g.AddTroop(troopId, 1);
                        p.RemovePrisoner(id);
                        _body.text = $"{name} joins your ranks.";
                        Rebuild();
                    }, UiFactory.ButtonStyle.Primary),
                    (willRansom ? $"Ransom ({ransomPrice}g)" : "Won't Ransom", () =>
                    {
                        p.gold += ransomPrice;
                        p.RemovePrisoner(id);
                        _body.text = $"{faction} pays {ransomPrice}g to ransom {name} back.";
                        Rebuild();
                    }, UiFactory.ButtonStyle.Secondary),
                    ("Release", () =>
                    {
                        p.RemovePrisoner(id);
                        p.AddMorale(2f);
                        p.AddRelation(faction, GameConstants.BanditReleaseRelationBoost);
                        _body.text = $"You release {name}. {faction} relations improve.";
                        Rebuild();
                    }, UiFactory.ButtonStyle.Danger),
                });
                btns[0].interactable = canRecruit;
                btns[1].interactable = atTavern && willRansom;
            }
        }

        static int RansomPrice(PrisonerRecord prisoner, PartyState party, GameState g)
        {
            var rankMultiplier = 1f;
            if (!string.IsNullOrEmpty(prisoner.recruitTroopId) && g.TroopRoster != null &&
                g.TroopRoster.TryGet(prisoner.recruitTroopId, out var def))
                rankMultiplier = 1f + (def.tier - 1) * 0.5f;

            var relation = party.GetRelation(prisoner.sourceFaction);
            var relationMultiplier = Mathf.Clamp(1f + relation / 100f, 0.5f, 2f);
            return Mathf.RoundToInt(GameConstants.PrisonerRansomBasePrice * rankMultiplier * relationMultiplier);
        }

        void RebuildCompanions(GameState g, PartyState p, ref float y)
        {
            var atTavern = g.Map.TryGetNode(p.currentNodeId, out var here) && here.hasTavern;

            if (p.companionIds.Count == 0)
                PlaceLabel("No companions yet.", ref y);

            foreach (var id in p.companionIds)
            {
                if (g.Companions == null || !g.Companions.TryGet(id, out var def))
                {
                    PlaceLabel(id, ref y);
                    continue;
                }

                PlaceLabel($"{def.displayName} — {def.role}. {DescribeBonus(def)}", ref y);
            }

            if (g.Companions == null)
                return;

            var offeredHere = new List<CompanionDefinition>();
            foreach (var def in g.Companions.companions)
            {
                if (def.isQuestReward) continue;
                if (p.companionIds.Contains(def.id)) continue;
                if (def.homeNodeId != p.currentNodeId) continue;
                offeredHere.Add(def);
            }

            if (offeredHere.Count == 0)
            {
                PlaceLabel(atTavern ? "No companions here today." : "Companions are found at settlement taverns.", ref y);
                return;
            }

            foreach (var def in offeredHere)
            {
                PlaceLabel($"{def.displayName} — {def.role}. {def.traitDescription} ({DescribeBonus(def)})", ref y);

                var id = def.id;
                var cost = def.recruitCost;
                var name = def.displayName;
                var (top, bottom) = UiFactory.NextRow(ref y, 0.1f, 0.015f);

                var recruitBtn = UiFactory.Button(_list, id + "_recruit_companion", $"Recruit ({cost}g)", new Vector2(0f, bottom), new Vector2(1f, top), () =>
                {
                    p.gold -= cost;
                    p.companionIds.Add(id);
                    _body.text = $"{name} joins your party.";
                    Rebuild();
                }, UiFactory.ButtonStyle.Primary);
                recruitBtn.interactable = atTavern && p.gold >= cost;
            }
        }

        static string DescribeBonus(CompanionDefinition def)
        {
            if (def.bonusType == CompanionBonusType.ScoutingBonus)
                return $"+{def.bonusValue:0.#} effective scouting";
            return $"+{def.bonusValue * 100f:0}% {BonusLabel(def.bonusType)}";
        }

        static string BonusLabel(CompanionBonusType type) => type switch
        {
            CompanionBonusType.BattlePower => "battle power",
            CompanionBonusType.TradeSellBonus => "sell prices",
            CompanionBonusType.WageDiscount => "wage discount",
            CompanionBonusType.CommandSwingBonus => "command swing",
            CompanionBonusType.LootBonus => "loot",
            CompanionBonusType.RecruitDiscount => "recruit discount",
            CompanionBonusType.FieldMedicine => "wounded saved & recovery",
            _ => "bonus"
        };

        void PlaceLabel(string text, ref float y, float top = -1, float bottom = -1)
        {
            if (top < 0)
            {
                top = y;
                bottom = y - 0.1f;
                y = bottom - 0.02f;
            }

            var lbl = UiFactory.Label(_list, "L" + y, text, 18, TextAnchor.MiddleLeft, Color.white);
            var rt = lbl.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, bottom);
            rt.anchorMax = new Vector2(1f, top);
        }
    }
}
