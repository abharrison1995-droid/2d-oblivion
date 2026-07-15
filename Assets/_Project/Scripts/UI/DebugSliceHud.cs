using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Temporary debug HUD so the skeleton is interactive in Play Mode without full UI art.
    /// </summary>
    public class DebugSliceHud : MonoBehaviour
    {
        string _log = "Voidovia slice ready.";
        Vector2 _scroll;
        string _destId = "ashpond";

        void OnGUI()
        {
            if (GameState.Instance == null)
            {
                GUILayout.Label("No GameState");
                return;
            }

            var g = GameState.Instance;
            var p = g.Party;

            GUILayout.BeginArea(new Rect(12, 12, 460, Screen.height - 24));
            GUILayout.Label($"Day {p.day}  Hour {p.hours:0}  Gold {p.gold}  Men {p.TotalMen}");
            GUILayout.Label($"At: {p.currentNodeId}   Rel Voidovia: {p.GetRelation(FactionId.Voidovia)}");
            GUILayout.Label($"Quest beat: {g.Act1Quest.Beat}   Merc: {p.isVoidoviaMercenary}  Vassal: {p.isVoidoviaVassal}");

            GUILayout.Space(8);
            GUILayout.Label("Travel destination id:");
            _destId = GUILayout.TextField(_destId);
            if (GUILayout.Button("Travel (light encounters only)"))
                TravelTo(_destId);

            if (GUILayout.Button("Speak to advisor (Greyledger)"))
            {
                if (p.currentNodeId != "greyledger")
                    Append("Travel to Greyledger first.");
                else
                {
                    g.Act1Quest.SpeakToAdvisor();
                    Append("Advisor named Ashpond and Tollbar.");
                }
            }

            if (GUILayout.Button("Resolve city investigation fight"))
            {
                var hint = g.Act1Quest.OnArriveForInvestigation(p.currentNodeId, true);
                Append(hint ?? "No investigation here.");
            }

            if (GUILayout.Button("Raid Buttery Lair (capture chief)"))
                RaidLair();

            if (GUILayout.Button("Deliver Chief / meet Void"))
            {
                g.Act1Quest.DeliverChiefToVoid(p);
                if (g.TryOfferMercenaryContract(out var msg))
                    Append(msg);
                else
                    Append(msg);
            }

            if (GUILayout.Button("Cheat: +10 Voidovia relation"))
            {
                p.AddRelation(FactionId.Voidovia, 10);
                Append($"Relation now {p.GetRelation(FactionId.Voidovia)}");
            }

            if (GUILayout.Button("Try vassal offer"))
            {
                g.TryOfferVassalage(out var msg);
                Append(msg);
            }

            if (GUILayout.Button("Tick one day (food + if Monday wages)"))
                TickDay();

            GUILayout.Space(8);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
            GUILayout.Label(_log);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void TravelTo(string dest)
        {
            var g = GameState.Instance;
            var route = g.Map.GetRoute(g.Party.currentNodeId, dest);
            if (route.Count == 0)
            {
                Append($"No route to {dest}");
                return;
            }

            foreach (var edge in route)
            {
                g.Travel.ApplyTravelTime(g.Party, edge);
                var encounter = g.Travel.RollEncounter(edge, g.Rng);
                if (encounter.kind != TravelEncounterKind.None)
                    Append($"{encounter.title}: {encounter.body}");
            }

            g.Party.currentNodeId = dest;
            Append($"Arrived {dest} on day {g.Party.day}.");

            if (g.Economy.ConsumeFood(g.Party, route.Count * 0.35f, out var foodLog))
                Append(foodLog);
            else
                Append(foodLog);
        }

        void RaidLair()
        {
            var g = GameState.Instance;
            if (g.Party.currentNodeId != g.Act1Quest.LairNodeId)
            {
                Append("Travel to buttery_lair first.");
                return;
            }

            var player = new BattleForce { name = "Your warband", troops = new List<TroopStack>(g.Party.troops) };
            var enemy = new BattleForce
            {
                name = "Buttery Lair",
                troops = new List<TroopStack>
                {
                    new() { troopId = "void_militia", count = 8 },
                    new() { troopId = "voidovan_cattle_rustler", count = 3 }
                }
            };

            g.Battle.Begin(player, enemy, captureLordRequired: true, enemyLordId: StolenItemQuestController.ButterChiefId);
            while (g.Battle.Phase != BattlePhase.Resolve)
            {
                var d = g.Battle.CurrentDecision();
                if (d == null)
                    break;
                g.Battle.ApplyOrder(d.options[0], out var beat);
                Append(beat);
                if (!string.IsNullOrEmpty(d.sunTzuAside))
                    Append(d.sunTzuAside);
            }

            var outcome = g.Battle.Resolve(g.Rng);
            Append(outcome.summary);
            g.Act1Quest.TryCompleteLairRaid(outcome, g.Party);
        }

        void TickDay()
        {
            var g = GameState.Instance;
            g.Party.day++;
            g.Economy.ConsumeFood(g.Party, 1f, out var foodLog);
            Append(foodLog);
            if (g.Party.day % 7 == 0)
            {
                var wages = g.Economy.WeeklyWageBill(g.Party);
                g.Party.gold -= wages;
                Append($"Wages paid: -{wages}g (now {g.Party.gold})");
                if (g.Party.isVoidoviaMercenary)
                {
                    var purse = g.RollMercenaryPurse();
                    g.Party.gold += purse;
                    Append($"Lord Void's purse: +{purse}g");
                }
            }
        }

        void Append(string line)
        {
            _log = line + "\n" + _log;
            if (_log.Length > 4000)
                _log = _log.Substring(0, 4000);
            Debug.Log(line);
        }
    }
}
