# Voidovia

Gritty low-fantasy warband / dynasty game. **Unity 2022.3 LTS**.

## Play
Open `Assets/_Project/Scenes/WorldMap.unity` → Play.

1. Character creation (family / childhood / moose)  
2. Scrollable map — **Travel** highlights route; **Advance inch** moves your marker step-by-step (time + events from mounts/scouting/terrain)  
3. **Nearby major parties** show when close  
4. **Market/Recruit** — T1 hires ~15g; merchants vs peasants (see their gold)  
5. **Party / Inv** — gold, food days, wages, inventory+equip, power-card pouch  
6. Act 1 → capture chief → **Lord Void audience** → mercenary offer  
7. **More…** saves the game (persistentDataPath)

## Travel (your design)
Route highlight → party symbol inches along the road. Each inch: time cost from speed (scouting, mount ratio, party size, terrain) and a chance of random event. Other warbands visible when near.

## Layout
`Assets/_Project/Scripts/` · `Assets/StreamingAssets/Data/`
