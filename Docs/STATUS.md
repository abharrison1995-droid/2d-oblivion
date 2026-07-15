# Voidovia — current state (2026-07-15, updated)

Snapshot for planning purposes. Ratings: ✅ Playable/solid · ⚠️ Functional but rough · 🔲 Built but not wired (dead code) · ❌ Missing.

**Update:** the 4 priority gaps identified below (settlements wiring, battle stats, save/load gaps, dead code) are now closed — see each section for what changed. Not yet verified in the Editor (Play mode hadn't been confirmed working again as of this update); treat as "should work" until playtested.

`Docs/validate_prototype.py` passes clean: 14/14 checks, 0 errors, 0 warnings (JSON integrity, map connectivity, core types present).

## Core loop (Act 1: "What's Mine") — ✅ Playable end-to-end

Character creation → world map → advisor → investigate two cities → lair spawns → raid + capture chief → deliver to Lord Void → mercenary offer. Fully scripted and wired (`AppFlow`, `StolenItemQuestController`, `VoidAudienceUI`). This is the one thing you can hand someone and say "play it start to finish" — see `Docs/SMOKE_TEST.txt` for the 10-step pass.

## World map & travel — ✅ Solid

Inch-by-inch travel with mounts/scouting/terrain affecting time, highlighted routes, Dijkstra pathfinding (`WorldGraph`), 5 encounter types including bandit ambush (weather, minor thieves, refugees, healers, ambush), other major AI parties visible and drifting toward targets on the map. Severe raids correctly gated behind mercenary status.

## Character creation — ✅ Solid

3-step interview (family / childhood / moose) → 64 stat/kit combinations, live stats preview, player portrait now tied to family choice. The unused legacy origin system (`OriginChoice`/`OriginCatalog`/`GameState.ApplyOrigin`) has been deleted.

## Battle — ✅ Now stat-driven (was: category+count only)

`BattleDirector` now computes per-side strength from each fielded troop's real `melee`/`ranged`/`armour`/`morale` stats (scaled by morale), not just category and count — a stack of `void_peacekeeper` is meaningfully stronger than the same count of `void_militia` now. Command-card swings and RNG chaos are percentage-based (scale with force size instead of a flat number tuned to one range). Casualties are drawn from the weakest stacks first, so elite troops survive longer. Loot is still hardcoded to 2 items regardless of what you fought (unchanged — separate gap). Dead code removed: `BattleDecision`/`ApplyOrder`/`CurrentDecision`.

## Economy / market — ⚠️ Functional but shallow

Buy/sell/recruit works (`SettlementUI`, `BookStoreUI`) with village-vs-town pricing spread. The flat T1 market recruit is unchanged, but settlements now separately offer tiered recruiting through their buildings (see below). Data is still thin: only 7 non-food items (2 weapons, 2 armours) in `economy.json`.

## Settlements / buildings — ✅ Now wired

`SettlementUI` has a new **Buildings** tab (alongside the existing Market tab): upgrade Governor's Grotto, build/upgrade Barracks / Archery Range / Military Stables (unlocking tiered troop recruiting via `SettlementState.HighestBarracksRecruit()` etc.), and build the Church of the Black Fluffy Tail at Grotto tier 4 to unlock Void Knight recruitment. `GameState.GetOrCreateSettlement()` lazily creates settlement state per node, mirroring how `MarketService.EnsureMarket` already worked. Build/upgrade costs are new placeholder numbers (`SettlementUI.BuildCost`) — explicitly untuned, same as everything else marked "tune in playtest."

## Save / load — ✅ Gaps closed

Now also persists: faction relations, reputation flags, prisoners, companions (previously silently reset to just `bangkok_kuo` on every load), and the settlements/buildings dict. Fixed an inconsistency where reloading after Act 1 completion silently despawned the quest lair from the map (it now persists, matching live-session behavior) — and closed a related bug where the lair battle could be re-triggered indefinitely by revisiting the node after the quest was already done. Still not persisted: world-party (AI band) positions — left as-is since they're cosmetic/re-seeded deterministically on load, not progress. Save format is still hand-rolled semicolon-delimited strings with no version field — a future schema change will still need a migration story.

## Party / troops / companions / prisoners — mixed

- **Troops:** ✅ real per-tier stat model and upgrade chain (T1→T4 + T5 specialty), now actually consumed by battle.
- **Equipment:** ⚠️ single weapon/armour slot only, no loadout depth.
- **Companions:** 🔲 just a `List<string>`, now correctly persisted through save/load, but still only one companion (`bangkok_kuo`) is ever added anywhere, no stats/skills/dialogue.
- **Prisoners:** 🔲 just a `List<string>`, now correctly persisted; `BattleOutcome.capturedLordIds` exists but there's still no ransom/recruit/execute mechanic beyond the one hardcoded Act 1 chief-capture path.

## Character/conversation art — 🔲 Infrastructure built, no real art yet

`UiFactory.Portrait` + `PortraitLoader` (Resources-based, auto-import settings, colored-square-with-initials fallback) landed this session. Wired into character creation (player, by family) and the Void audience scene (Lord Void). No art files exist yet — everything currently shows as placeholder squares. Not wired into the scrolling log UI (`AppendLog`) since that's unstructured text with no per-speaker concept.

## Data content breadth — thin, enough to exercise systems once

17 map nodes, 14 troops, 17 battle cards, 7 factions, 15 economy items total. Enough for a single playthrough of Act 1, not enough variety for extended replay. All balance numbers (wages, upgrade costs, thresholds) are explicitly commented "tune in playtest" — i.e., nobody has validated them yet.

## Tooling / project health — ✅ Solid

First-open friction addressed (boot error screen, `ProjectSettings` completed and committed, `.vsconfig`), `Docs/SMOKE_TEST.txt` walkthrough exists, `validate_prototype.py` gives a fast offline sanity check, Cursor Cloud environment documented in `AGENTS.md`. One open item: last confirmed check had the Unity Editor sitting on no open scene ("Untitled") — worth confirming Play works cleanly from `Bootstrap.unity` next session.

---

## Where this leaves you

**You have one complete, playable vertical slice** (Act 1), and the four "looks done but isn't" gaps from the last review pass are now closed. **Not yet playtested in-editor** — the changes are internally consistent and pass the offline validator, but nobody has clicked through them in Unity yet. That's the immediate next step, not more building.

After that's confirmed working, the natural next layer (additive, not gap-closing):

1. **Balance pass** — battle power formula, building/upgrade costs, and market prices are all freshly-written or pre-existing placeholder numbers. Nothing is tuned.
2. **Content breadth** — more troops/items/cards/map nodes; still only enough data for one playthrough.
3. **Real portrait art** — infrastructure exists (`UiFactory.Portrait`, `Resources/Portraits/`), only placeholder squares exist so far.
4. **Loot variety in battle** — `BattleDirector.Finalize` still hands out the same 2 hardcoded loot items regardless of what you fought.
