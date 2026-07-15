# Voidovia

Gritty low-fantasy warband / dynasty game (Mount & Blade campaign loop, KCD-style travel, Football Manager battle decisions). Built for **Unity 2022.3 LTS** → iOS later.

## Open in Unity

1. Install **Unity Hub** + **Unity 2022.3.x LTS** (version in `ProjectSettings/ProjectVersion.txt`).
2. Hub → **Add** → select this repo root.
3. Open; let packages resolve.
4. Press Play on `Assets/_Project/Scenes/WorldMap.unity` (or `Bootstrap.unity`).

First import may recreate `.meta` files and ask to upgrade packages — accept defaults.

## Vertical slice (skeleton target)

Debug OnGUI on the WorldMap scene can drive:

1. Origin spawn (default: Mill Levy) with stolen heirloom quest active  
2. Travel node → node with light road encounters  
3. Advisor in Greyledger → Ashpond / Tollbar investigation  
4. Temporary **Buttery Lair** spawn  
5. FM-style battle + **capture** Buttery Chief  
6. Deliver chief → Lord Void **mercenary** offer  
7. Relation ≥ 30 + good rep → **vassal** gate  
8. Food ticks + weekly wages (+ mercenary purse)

## Layout

```
Assets/_Project/Scripts/     Core systems (namespace Voidovia)
Assets/StreamingAssets/Data/ JSON: map, troops, food/items, factions
Assets/_Project/Scenes/      Bootstrap, WorldMap, Battle
Packages/                    UGUI, Input System, Newtonsoft
```

### Systems stubbed

| Area | Classes |
|------|---------|
| Map | `WorldMapData`, `WorldGraph` |
| Travel | `TravelDirector` |
| Battle | `BattleDirector` |
| Quest | `StolenItemQuestController` |
| Party / economy | `PartyState`, `EconomyService` |
| Settlements | `SettlementState` (Grotto gate, Church of the Black Fluffy Tail) |
| Session | `GameState`, `GameBootstrap` |

## Design locks (v1)

- Start: **Voidovia** only  
- Ruler: **Lord Void, The Wide Eyed Beast**  
- Companion: **Bangkok Kuo**  
- Capture lords; intentional execution only after you own land  
- Food: grain / fish / meat etc. (M&B-style)  
- Hire fee + wages + food  
- Void Knight via **Church of the Black Fluffy Tail** (Grotto T4); mounted or foot choice  
- Vassal at relation **+30** and good reputation  

## Next build passes

- Real map UI + route animation  
- Proper battle UI decision cards  
- Origin select screen  
- Stores / tavern / recruitment UI  
- Spear + crossbow lines  
- Multi-kingdom skeleton nodes (non-Voidovia stubs)  

## Note

This environment has no Unity Editor — scenes and scripts are hand-authored for you to open locally. If a MonoBehaviour shows "missing script", reassign `GameBootstrap`, `WorldMapEntry`, or `DebugSliceHud` once in the Inspector.
