# Voidovia

Gritty low-fantasy warband / dynasty game (Mount & Blade campaign loop, KCD-style travel, Football Manager battle decisions). **Unity 2022.3 LTS** → iOS later.

## Open & play (current build)

1. Install Unity Hub + **Unity 2022.3.x LTS**
2. Hub → **Add** this repo root (folder with `Assets` / `Packages` / `ProjectSettings`)
3. Open `Assets/_Project/Scenes/WorldMap.unity`
4. Press **Play**

### What you should see
1. **Character creation** — name + 3 questions (family / childhood / moose)
2. **Scrollable world map** — drag to pan; tap nodes; inspect routes; Travel
3. Far-realm capitals tagged **far realm** (skeleton only)
4. At **Buttery Lair**, **Quest action** opens a real **battle decision** UI

### Act 1 path
Greyledger → Ask advisor → Ashpond or Tollbar → lair appears → travel `buttery_lair` → Quest action (battle) → deliver chief (Quest action again in a Voidovia hub after capture)

## Character creation → stats

| Beat | Choices | Main lean |
|------|---------|-----------|
| Family | Diplomats / Traders / Nomads / Healers | Spawn + gold + base stats |
| Child | Horses / Trading toys / Organizing teams / Stealing-fighting | Fine-tune + troop seed |
| Moose | Nurse-release / Heal-sell / Kill-meat / Leave alone | Soft moral/trade/combat nudge |

Exact modifiers live in `CharacterCreation.cs`.

## Map UI intent
Clear strategic map you can **comfortably drag/scroll**, tap towns to read faction/services/route time, then travel. Voidovia is fully wired; other kingdoms show as **skeleton nodes**.

## Multi-kingdom “skeleton nodes” means
Other factions’ capitals are **on the map now** (names, positions, border roads) so the world feels big — but they are flagged `isSkeleton`. No deep quests/recruit trees there yet. Full Voidovia content first; flesh those later without remaking the map.

Skeleton places right now: Miregate (Butter), Ra-Xael Crownhold, Small/Long Spine hubs, Orthodox Bastion.

## Layout
```
Assets/_Project/Scripts/   systems + runtime UI
Assets/StreamingAssets/Data/  map, troops, economy JSON
Assets/_Project/Scenes/WorldMap.unity
```
