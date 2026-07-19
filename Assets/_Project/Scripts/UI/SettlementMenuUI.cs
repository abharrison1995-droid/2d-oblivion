using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Contextual hub for the settlement the party is standing in: market/recruit, book store,
    /// and whatever NPCs or quest beats are tied to this specific place. Image is a placeholder
    /// (PortraitLoader initials block) until real settlement art lands in Resources/Portraits/.
    /// </summary>
    public class SettlementMenuUI : MonoBehaviour
    {
        Canvas _canvas;
        Text _title;
        Text _subtitle;
        Text _description;
        RectTransform _actionsRoot;
        Action<string> _log;
        Action _onClosed;

        public void Open(string nodeId, Action<string> log, Action onClosed)
        {
            _log = log;
            _onClosed = onClosed;
            Ensure();
            _canvas.gameObject.SetActive(true);
            Rebuild(nodeId);
        }

        void Ensure()
        {
            if (_canvas != null) return;
            _canvas = UiFactory.CreateCanvas("SettlementMenuCanvas", 35);
            var root = UiFactory.Panel(_canvas.transform, "Root", Vector2.zero, Vector2.one, UiFactory.Theme.PanelBackground);

            UiFactory.Portrait(root, "Image", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.86f), "settlement_placeholder");

            _description = UiFactory.Label(root, "Description", "", 23, TextAnchor.MiddleCenter, UiFactory.Theme.TextDim);
            Stretch(_description, 0.05f, 0.55f, 0.95f, 0.62f);

            _title = UiFactory.Label(root, "Title", "", 34, TextAnchor.MiddleCenter, UiFactory.Theme.TextTitle);
            Stretch(_title, 0.05f, 0.48f, 0.95f, 0.55f);

            _subtitle = UiFactory.Label(root, "Subtitle", "", 20, TextAnchor.MiddleCenter, UiFactory.Theme.TextDim);
            Stretch(_subtitle, 0.05f, 0.42f, 0.95f, 0.48f);

            _actionsRoot = UiFactory.Panel(root, "Actions", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.4f), new Color(0, 0, 0, 0.15f));

            UiFactory.Button(root, "Leave", "Leave", new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.09f), Close);
        }

        void Rebuild(string nodeId)
        {
            foreach (Transform child in _actionsRoot)
                Destroy(child.gameObject);

            var g = GameState.Instance;
            if (!g.Map.TryGetNode(nodeId, out var node))
            {
                Close();
                return;
            }

            _title.text = node.displayName;
            _subtitle.text = $"{node.type} · {node.controllingFaction}";
            _description.text = node.flavorText;

            var actions = new List<(string label, Action onClick)>();

            if (node.hasStore || node.hasRecruitment || node.type is NodeType.Village or NodeType.Town or NodeType.Capital)
                actions.Add(("Market & Recruit", () => Launch(() =>
                    GetOrAdd<SettlementUI>().Open(_onClosed))));

            if (node.hasBookStore)
                actions.Add(("Book Store", () => Launch(() =>
                    GetOrAdd<BookStoreUI>().Open(_onClosed))));

            // Befriend the horse people (Nomads relation) and their riders will take your coin, even
            // far from the steppe — the only way to field the Nomad tree until you hold a Nomad town.
            if (node.hasTavern && g.Party.GetRelation(FactionId.Nomads) >= GameConstants.NomadHireRelationThreshold)
            {
                AddNomadHire(g, actions, "nomad_outrider");
                AddNomadHire(g, actions, "nomad_skirmisher");
            }

            if (nodeId == "greyledger")
                actions.Add(("Advisor", () =>
                {
                    g.Act1Quest.SpeakToAdvisor();
                    _log?.Invoke("Advisor: try Beef or Tollbar.");
                    Close();
                }));

            if (nodeId == g.Act1Quest.LairNodeId && g.Act1Quest.LairVisible &&
                g.Act1Quest.Beat == StolenItemQuestBeat.LairSpawned)
                actions.Add(("Raid the Buttery Lair", () => Launch(() =>
                    GetOrAdd<BattleUI>().BeginLairBattle(outcome =>
                    {
                        g.Act1Quest.TryCompleteLairRaid(outcome, g.Party);
                        foreach (var cardId in outcome.rewardPowerCardIds)
                            _log?.Invoke($"Chief's treatise: {cardId}");
                        _log?.Invoke(outcome.summary);
                        _onClosed?.Invoke();
                    }))));

            if (node.type == NodeType.BanditCamp)
            {
                var friendly = g.Party.GetRelation(FactionId.Bandits) >= GameConstants.BanditFriendlyRelationThreshold;
                if (friendly)
                {
                    actions.Add(($"Hire bandit mercenary ({GameConstants.BanditMercenaryHireCost}g)", () =>
                    {
                        if (!g.CanRecruit(1, out var capReason))
                        {
                            _log?.Invoke(capReason);
                            return;
                        }

                        if (g.Party.gold < GameConstants.BanditMercenaryHireCost)
                        {
                            _log?.Invoke("Not enough gold.");
                            return;
                        }

                        g.Party.gold -= GameConstants.BanditMercenaryHireCost;
                        g.AddTroop(GameConstants.RecruitedBanditTroopId, 1);
                        _log?.Invoke($"A bandit joins your warband for {GameConstants.BanditMercenaryHireCost}g. These raiders know you as an ally now.");
                        Close();
                    }));
                }
                else if (g.CanRaidCamp(node.id, out var daysLeft))
                {
                    var enemyCount = BattleUI.ForceCount(BattleUI.BanditCampForce(node.displayName));

                    if (BanditSurrender.CanOffer(enemyCount))
                    {
                        var chance = BanditSurrender.SuccessChance(enemyCount);
                        actions.Add(($"Demand Surrender (~{chance * 100f:0}%)", () =>
                        {
                            if (g.Rng.NextDouble() < chance)
                            {
                                g.MarkCampRaided(node.id);
                                _log?.Invoke(BanditSurrender.Resolve(enemyCount, node.displayName));
                            }
                            else
                            {
                                _log?.Invoke($"{node.displayName} refuses to yield — they attack.");
                                Launch(() => GetOrAdd<BattleUI>().BeginBanditCampBattle(node.displayName, outcome =>
                                    HandleCampRaidOutcome(g, node, outcome)));
                                return;
                            }

                            Close();
                        }));
                    }

                    actions.Add(($"Raid {node.displayName}", () => Launch(() =>
                        GetOrAdd<BattleUI>().BeginBanditCampBattle(node.displayName, outcome =>
                            HandleCampRaidOutcome(g, node, outcome)))));
                }
                else
                {
                    actions.Add(($"{node.displayName} — regrouping ({daysLeft}d)", null));
                }
            }

            if (g.Act1Quest.Beat == StolenItemQuestBeat.ChiefCaptured &&
                nodeId is "greyledger" or "bastion_holt" or "red_knoll")
                actions.Add(("Seek Audience with Lord Void", () => Launch(() =>
                    GetOrAdd<VoidAudienceUI>().Open(() =>
                    {
                        _log?.Invoke(g.Party.isVoidoviaMercenary
                            ? "You ride for Lord Void's purse."
                            : "You remain free company — for now.");
                        _onClosed?.Invoke();
                    }))));

            if (actions.Count == 0)
            {
                var empty = UiFactory.Label(_actionsRoot, "Empty", "Nothing to do here yet.", 20, TextAnchor.MiddleCenter, UiFactory.Theme.TextDim);
                Stretch(empty, 0f, 0.4f, 1f, 0.6f);
                return;
            }

            // Two columns per row instead of one long button per row.
            var rows = Mathf.CeilToInt(actions.Count / 2f);
            var rowStep = 1f / rows;
            for (var i = 0; i < actions.Count; i++)
            {
                var row = i / 2;
                var col = i % 2;
                var top = 1f - row * rowStep;
                var bottom = top - rowStep + 0.02f;
                var xLeft = col == 0 ? 0.02f : 0.51f;
                var xRight = col == 0 ? 0.49f : 0.98f;
                var action = actions[i];
                var btn = UiFactory.Button(_actionsRoot, "Act" + i, action.label, new Vector2(xLeft, bottom), new Vector2(xRight, top - 0.02f), action.onClick);
                btn.interactable = action.onClick != null;
            }
        }

        void AddNomadHire(GameState g, List<(string label, Action onClick)> actions, string troopId)
        {
            var name = g.TroopRoster != null && g.TroopRoster.TryGet(troopId, out var def) ? def.displayName : troopId;
            actions.Add(($"Hire {name} ({GameConstants.NomadHireCost}g)", () =>
            {
                if (!g.CanRecruit(1, out var capReason))
                {
                    _log?.Invoke(capReason);
                    return;
                }

                if (g.Party.gold < GameConstants.NomadHireCost)
                {
                    _log?.Invoke("Not enough gold to hire the nomads.");
                    return;
                }

                g.Party.gold -= GameConstants.NomadHireCost;
                g.AddTroop(troopId, 1);
                _log?.Invoke($"A {name} rides in to join your company.");
                Close();
            }));
        }

        void HandleCampRaidOutcome(GameState g, MapNodeData node, BattleOutcome outcome)
        {
            if (outcome.playerVictory)
            {
                g.MarkCampRaided(node.id);
                // Prisoners (including any bandits taken alive) are captured centrally in BattleUI.Finish
                // now, tagged with the enemy faction — no camp-specific sentinel handling needed here.
                if (outcome.goldLooted > 0)
                    _log?.Invoke($"You loot {outcome.goldLooted}g from the camp.");
            }
            else
            {
                _log?.Invoke(BanditCaptivity.Apply(g, node.parentSettlementId));
            }

            _log?.Invoke(outcome.summary);
            _onClosed?.Invoke();
        }

        T GetOrAdd<T>() where T : Component =>
            FindObjectOfType<T>() ?? new GameObject(typeof(T).Name).AddComponent<T>();

        void Launch(Action openOther)
        {
            _canvas.gameObject.SetActive(false);
            openOther();
        }

        static void Stretch(Text t, float x0, float y0, float x1, float y1)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void Close()
        {
            _canvas.gameObject.SetActive(false);
            _onClosed?.Invoke();
        }
    }
}
