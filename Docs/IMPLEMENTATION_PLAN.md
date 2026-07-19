# Voidovia — Implementation Plan (Mount & Blade / Kenshi / Oblivion)

A feature & mechanics roadmap that builds on what already exists rather than bolting on
parallel systems. Each item names the **inspiration**, the **hook** (the file/system it plugs
into), and a **first-cut mechanic** you can implement without an engine rewrite. Ordered into
phases so the game is playable and better at the end of each one.

Design north star: **you are not the chosen one.** You are one warband in a world that keeps
moving whether you win or lose. M&B gives the campaign loop, Kenshi gives the indifferent world
and consequences, Oblivion gives texture — guilds, growth-by-doing, dungeons, loot, crime.

---

## Phase 0 — Make the loop bite (mostly done)

The economy had no teeth: wages were computed for the HUD but only ever charged inside the debug
HUD, so in normal play troops were free and the mercenary contract paid nothing. Fixed this pass:

- **Weekly payday now fires in the live loop** (`GameState.PayWeeklyUpkeep`, called from
  `OnNewDay`). Missing payroll drains morale (`GameConstants.UnpaidWageMoralePenalty`).
- **Casualties now remove the men the sim actually killed** (weakest-first) via
  `BattleOutcome.playerLossesByTroop`, instead of stripping your biggest stack.
- **Food-days HUD** now measures real fill (`EconomyService.TotalFoodFill`).
- **Procedural combat quests survive save/load** (`QuestBoardService.RespawnTargetNode`).

- **Desertion now bites** (`GameState.RollDesertion`, payday-tied): low morale drives it, a missed
  payroll piles on, and deserters are the weakest men first (capped per week). This closes the
  wage-pressure loop — troops now cost you when you can't keep them paid and content.

**Remaining Phase 0 tuning (do before adding features):**
1. **Balance pass.** Wages, upgrade costs, loot rates, and market wallets are all flagged
   "tune in playtest." With wages now live, do one honest playthrough and re-tune so gold is a
   real constraint, not a rounding error.
3. **Clearer battle readout.** The win/loss check is a float-strength compare the player can't
   see. Surface a simple "advantage" bar so timeouts don't feel arbitrary.

---

## Phase 1 — Deepen the warband (Mount & Blade core)

The warband *is* the game. Right now troops are stacks with wages and an upgrade chain. Make the
roster feel like people you manage under pressure.

| Feature | Inspiration | Hook | First-cut mechanic |
|---|---|---|---|
| **Wounded pool** ✅ | M&B + Kenshi | `PartyState.wounded`, `GameConstants.Wounded*` | **Done.** A share of casualties are wounded (recoverable) not dead — 50% on a win, 25% on a rout (`BattleUI.Finish`). They can't fight, don't draw wages, and mend back into the roster each day via `GameState.RecoverWoundedDaily` (faster resting in a settlement). Persisted (save v3), shown in the HUD and Party panel. *Next refinements: wounded should eat (currently don't), and a Surgeon companion role should scale recovery.* |
| **Party size cap** ✅ | M&B | `GameState.MaxPartySize / CanRecruit` | **Done.** Cap = `BasePartySize + Leadership × PartySizePerLeadership` (18 + 3/pt). Fighting troops *and* wounded count toward it, so a bloody win crowds out hiring until men mend. All four recruit paths (market, building-tier, bandit hire, prisoner recruit) are gated; over-cap bleeds morale daily (`BleedOverCapMorale`) as the universal backstop. Shown in the HUD (`12/33 men`) and Party panel. Spending a skill point on Leadership raises the cap immediately. |
| **Prisoner economy** ✅ | M&B | `BattleDirector.CaptureFromEnemy`, `PartyState.prisoners` | **Done.** The three verbs (recruit-after-delay / ransom-by-rank-&-relation / release-for-relation) were already wired in `PartyPanelUI`; the gap was *supply* — prisoners only dropped from camp raids via a sentinel. Now **any won battle** captures a share of the enemy's fallen (`PrisonerCaptureFraction`, capped per battle and by `PrisonerCapacity`), tagged with the enemy's faction (`BattleForce.faction`) so ransom pays from and release improves the right faction. Removed the old camp-only sentinel path. |
| **Morale sources** | M&B | `PartyState.AddMorale` | Recent victories, food variety (already partial), being paid, winning against odds → up. Starvation, defeats, overcrowding, unpaid wages → down. Show the breakdown in the Party panel. |
| **Troop trees per culture** ✅ | M&B | `troops.json`, `BattleForceTables`, `MarketService` | **Done.** Added a **Butter Klan** tree (brittle, aggressive, cheap — 4-tier infantry + slinger line) and a **Nomad** tree (mounted; horse-archer + lancer lines). All outlaw enemies now field Butter troops (lore-consistent "Butter rogues"), so fights look different *and* captured outlaws recruit into the raider tree. Settlements recruit their controlling faction's culture (`RecruitTroopForCulture` — butter_hollow now sells Butter troops; Nomad towns will once Phase 2 flips them). Nomad riders are hireable at any tavern once Nomads relation ≥ 10, so the horse tree is fielded today. |
| **Companion skills, not flavor** ◐ | M&B/Kenshi | `CompanionBonuses`, `companions.json` | Bonuses already sum into real hooks (battle, wages, trade, scouting, loot, recruiting). **Added the Surgeon** (`CompanionBonusType.FieldMedicine`, Sister Yelen @ saltmere): she saves more casualties as wounded-not-dead *and* speeds recovery — the wounded system's counterpart. *Remaining roles to add: Quartermaster → food drain, and a meaningful Trainer once training takes more than 1 day.* |

**Why first:** these reuse existing data shapes (`TroopStack`, `PrisonerRecord`, the bonus hooks)
and turn already-persisted state into decisions.

---

## Phase 2 — A world that doesn't wait for you (M&B + Kenshi)

Right now AI parties drift toward targets and factions decay/incident randomly. Make the map a
system you're inside, not a backdrop.

| Feature | Inspiration | Hook | First-cut mechanic |
|---|---|---|---|
| **Faction war state** ✅ | M&B | `FactionDiplomacy`, `WorldPartyDirector` | **Done.** A war/peace matrix between the seven belligerent factions (`FactionDiplomacy`), seeded with the Voidovia–Butter border war and flipping one random pair per day. AI warbands now **march on their faction's enemy settlements** and **skirmish on contact** (loser retreats, strength regens daily); two more belligerent columns added. War/peace and nearby clashes surface as notifications; the settlement inspect shows "At war with: …". Persisted (save v4). *Next: settlements actually change hands when a strong enough hostile band reaches them.* |
| **Settlements change hands** | M&B/Kenshi | `SettlementState`, `WorldGraph`, `controllingFaction` | A settlement besieged and taken flips `controllingFaction`, its market/recruits/buildings change, and quests there reflect the new owner. The world map's power balance is now readable and mutable. |
| **Raids with consequences** ✅ | Kenshi | `SettlementState.prosperity`, `MarketService` | **Done.** Settlements now carry **prosperity** (0–100). A war band too weak to hold a town sacks it (−8); a capture sacks it harder (−20); peace rebuilds it toward the baseline daily. Prosperity scales the market's **buyer purse and recruit stock**, so a raided town is visibly poorer and short of men — and the settlement inspect shows "Prosperity: 32% (struggling)". Persisted (save v5). *Next: let the player intervene/profit, and villages remember protection.* |
| **Infamy-scaled danger** | Kenshi | `ReputationFlag`, `TravelDirector` | `WantedInVoidovia`/`Infamous` already exist. Wire them: wanted → Voidovia patrols hunt you, towns charge entry tolls or bar you; friendly bandits (already implemented!) is the mirror image. The world *reacts* to who you've become. |
| **Caravans & trade routes** | M&B | `TravelDirector` encounters, `MarketService` | The `Trader` encounter becomes a real caravan traveling between markets, carrying priced goods. Raid it (loot + infamy) or escort it (the escort quest already exists — connect them). Regional price spreads make trade a viable career. |
| **Notables & recruitment** | M&B | `SettlementState`, `QuestBoardService` | Villages have a notable whose relation with you gates recruit quality/quantity. Quests raise it. Recruiting stops being a flat gold transaction. |

**Why second:** it needs Phase 1's warband to matter, and it's the biggest "the game is alive"
multiplier. Start with **faction war state** — everything else hangs off who's fighting whom.

---

## Phase 3 — Growth by doing (Oblivion)

You have five hero stats, hero XP/levels, skill points, and Warband Experience. Oblivion's lesson:
the character sheet should grow out of *how you play*, and reward specialization.

| Feature | Inspiration | Hook | First-cut mechanic |
|---|---|---|---|
| **Use-based skill nudges** | Oblivion | `CharacterStats`, `HeroStatBonuses` | Actions grant small progress to the relevant skill: winning battles → Combat/Tactics; profitable trades → Trade; clean travel → Scouting. Skill points from levels stay, but play *leans* your build. |
| **Perks at thresholds** | Oblivion/M&B | `HeroStatBonuses`, `GameConstants` | At skill 25/50/75 unlock a named perk (Trade 50: "Haggler" halves market spread; Tactics 75: "Feigned Retreat" lets you disengage without the rear-guard blood price — wires up the currently-dead `BattleDirector.Retreat`). |
| **Crime & bounty** | Oblivion/Kenshi | `ReputationFlag`, `PartyState.relations` | Raiding caravans, robbing villages, breaking prisoners out → bounty in that faction's region. Pay it off, serve it (captivity), or go outlaw. The `Infamous` flag becomes a lived status, not a save field. |
| **Faction questlines** | Oblivion guilds | `QuestBoardService`, new controllers | Beyond Act 1 and the procedural board, give 2–3 factions a *hand-authored* short questline (Healers, Orthodoxy, Traders) with a title/reward at the end — the Oblivion "join the guild, rise through ranks" arc, warband-scale. |
| **Dialogue trees** | Oblivion | `VoidAudienceUI` pattern | The Void audience is a one-shot branch. Generalize it into a small reusable dialogue node UI (text + 2–4 choices + consequences) so notables, prisoners, and questgivers can *talk*. Big texture-per-line-of-code win. |

**Why third:** it's the "reasons to keep playing one character" layer; it needs the reactive world
of Phase 2 to push against.

---

## Phase 4 — Consequences & survival (Kenshi)

Kenshi's signature: losing is content, not a game-over. You already have a captivity system
(`BanditCaptivity`) — that instinct is exactly right. Extend the world's willingness to hurt you
and let you crawl back.

| Feature | Inspiration | Hook | First-cut mechanic |
|---|---|---|---|
| **Captivity as a scene** | Kenshi | `BanditCaptivity`, new UI | Being taken isn't just gold theft + time skip. You're held at the camp: escape attempts (Scouting check), wait for rescue (a companion's band comes for you), or be ransomed. Turn the existing hook into a short survival beat. |
| **Wounds that linger** | Kenshi | wounded pool (Phase 1) | Heavy defeats leave a *scarred* count that heals slowly and caps party effectiveness until treated — the world remembers your bad week. |
| **Base / fief building** | Kenshi/M&B | `SettlementState`, `BuildingType` (already enumerated!) | The `BuildingType` enum and building tabs already exist. The Kenshi/M&B endgame: own a fief (vassalage path is stubbed → `ownsLand`), then build it up — barracks for better levies, walls for defense against raids, granary for food security, foundry for elite troops. |
| **Regional collapse & takeover** | Kenshi | Phase 2 war state | If a faction loses all settlements it's *gone* — its troops become bandits, its lord a wanderer you might capture or recruit. The map can genuinely reshape over a long game. |
| **Starvation spiral** | Kenshi | `EconomyService.ConsumeFood` | Prolonged hunger → not just morale, but wounded/desertion. Food logistics becomes a real campaign concern on long marches, not a soft warning. |

**Why fourth:** it's the highest-reward, highest-effort layer, and it only lands if Phases 1–2
made the warband and world matter enough that losing them stings.

---

## Phase 5 — Content & texture (Oblivion)

Breadth. Do this continuously alongside the others; it's what makes a mechanically-sound game feel
*full*. Current data is "enough to exercise each system once."

- **Dungeon/lair mini-crawls.** Quest lairs resolve in one battle. Make some multi-room: 2–3
  chained fights with a choice between them (press on wounded vs. withdraw), a boss, leveled loot.
  Hook: a `LairCrawlController` wrapping `BattleUI`.
- **Leveled loot & artifacts.** Power cards already are your "artifacts" (Book Store, quests, boss
  drops). Expand the pool, tie drop quality to enemy strength, and seed a few named legendary cards
  behind specific lairs. Hook: `battle_cards.json`, `BattleDirector.Finalize` loot.
- **Crafting / alchemy.** Trophies (`ItemType.Trophy`) and food into a light crafting bench at
  settlements: smoke meat (better fill), forge a weapon from trophies, brew a morale tonic. Hook:
  `EconomyCatalog`, a `CraftingUI`.
- **More factions, troops, items, nodes.** Each new region = a culture, a troop tree, a market
  profile, a notable, and a questline. This is the replay-value lever.
- **Portraits & speaker-tagged log.** Infrastructure exists (`PortraitLoader`, `UiFactory.Portrait`).
  Real art + a speaker concept in the log turns text into characters.

---

## Suggested order of attack

1. **Finish Phase 0** — desertion + one real balance pass. The loop must bite before more is added.
2. **Phase 1: wounded pool + party cap + prisoner verbs.** Biggest feel-per-effort; all reuse
   existing state.
3. **Phase 2: faction war state → settlements change hands.** The "world is alive" leap.
4. Interleave **Phase 5 content** throughout so each system has enough data to be fun.
5. **Phase 3 growth** and **Phase 4 consequences** as the long-game depth once the world reacts.

## Guardrails

- Keep respecting `Docs/DESIGN_LOCKS.txt` — Voidovia start, Lord Void, Bangkok Kuo, the Act 1 beat
  chain, inch-by-inch travel, text+bars battles.
- Everything data-driven where possible (`StreamingAssets/Data/*.json`) so content is addable
  without recompiling — the project already leans this way.
- Save format is hand-rolled and versioned (`SaveLoadService.CurrentSaveVersion`). Every new piece
  of persistent state needs an encode/decode pair and a version bump with a default-on-old-save
  story, exactly like the v1→v2 quest-board migration.
- One new system at a time, each playtested. The current gap wasn't missing features — it was an
  un-exercised loop. Don't recreate that at larger scale.
