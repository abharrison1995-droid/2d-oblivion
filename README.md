# Voidovia

Gritty low-fantasy warband / dynasty game (Mount & Blade campaign loop, KCD-style travel, card-driven text battles). **Unity 2022.3 LTS** → iOS later.

## Open & play (current build)

1. Install Unity Hub + **Unity 2022.3.x LTS**
2. Hub → **Add** this repo root (folder with `Assets` / `Packages` / `ProjectSettings`)
3. Open `Assets/_Project/Scenes/WorldMap.unity`
4. Press **Play**

### What you should see
1. **Character creation** — name + 3 questions (family / childhood / moose)
2. **Scrollable world map** — drag to pan; tap nodes; inspect routes; Travel
3. Far-realm capitals tagged **far realm** (skeleton only)
4. **Greyledger Book Store** — expensive power cards
5. At **Buttery Lair**, **Quest action** opens **card battle** UI

### Act 1 path
Greyledger → Ask advisor → Ashpond or Tollbar → lair appears → travel to lair → Quest action (card battle) → deliver chief

## Battle cards (prototype)

Text battle + unit counts/bars.

- **Command cards** each turn (Hold the Line, Charge, Slowly Engage, Mounts Flank…)  
  Pick **one per unit type** you field (infantry + mounts in the same turn is intended).
- **Power cards** (rare): e.g. Spearmen Sickness (−20% enemy spears). Once each per battle.
- Clash has **RNG swing** every turn.
- Buy powers at **Greyledger Book Store** for ~1100–2000g.
- **Buttery Chief** drops a boss treatise on capture.

Data: `Assets/StreamingAssets/Data/battle_cards.json`

## Character creation → stats

| Beat | Choices | Main lean |
|------|---------|-----------|
| Family | Diplomats / Traders / Nomads / Healers | Spawn + gold + base stats |
| Child | Horses / Trading toys / Organizing teams / Stealing-fighting | Fine-tune + troop seed |
| Moose | Nurse-release / Heal-sell / Kill-meat / Leave alone | Soft moral/trade/combat nudge |

## Multi-kingdom skeleton nodes
Other faction capitals are on the map (`isSkeleton`) with roads, but deep content comes later. Voidovia is the playable kingdom for Act 1.

## Layout
```
Assets/_Project/Scripts/   systems + runtime UI
Assets/StreamingAssets/Data/  map, troops, economy, battle_cards
Assets/_Project/Scenes/WorldMap.unity
```
