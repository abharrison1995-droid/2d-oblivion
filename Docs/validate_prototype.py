#!/usr/bin/env python3
"""Pre-Unity validation for Voidovia prototype. Run: python3 Docs/validate_prototype.py"""
import json, re, sys
from pathlib import Path
from collections import defaultdict, deque

root = Path(__file__).resolve().parents[1]
errors, warns, oks = [], [], []

def ok(m): oks.append(m)
def err(m): errors.append(m)
def warn(m): warns.append(m)

data_dir = root / "Assets/StreamingAssets/Data"
loaded = {}
for name in ["voidovia_map.json", "troops.json", "economy.json", "battle_cards.json"]:
    p = data_dir / name
    if not p.exists():
        err(f"MISSING {p}"); continue
    try:
        loaded[name] = json.loads(p.read_text()); ok(f"JSON OK: {name}")
    except Exception as e:
        err(f"JSON FAIL {name}: {e}")

if "voidovia_map.json" in loaded:
    m = loaded["voidovia_map.json"]
    nodes = {n["id"]: n for n in m["nodes"]}
    for r in m["roads"]:
        if r["fromNodeId"] not in nodes: err(f"Road from missing: {r['fromNodeId']}")
        if r["toNodeId"] not in nodes: err(f"Road to missing: {r['toNodeId']}")
    if not nodes.get("greyledger", {}).get("hasBookStore"):
        err("Greyledger missing hasBookStore")
    adj = defaultdict(list)
    for r in m["roads"]:
        adj[r["fromNodeId"]].append(r["toNodeId"])
        adj[r["toNodeId"]].append(r["fromNodeId"])
    seen, q = set(), deque(["greyledger"])
    while q:
        cur = q.popleft()
        if cur in seen: continue
        seen.add(cur)
        q.extend(adj[cur])
    bad = [i for i in nodes if i not in seen]
    if bad: err(f"Unreachable: {bad}")
    else: ok(f"Map OK ({len(nodes)} nodes)")

if "troops.json" in loaded:
    troops = {t["id"] for t in loaded["troops.json"]["troops"]}
    for tid in ["void_militia", "void_archer", "voidovan_cattle_rustler"]:
        if tid not in troops: err(f"Missing troop {tid}")
    ok(f"Troops OK ({len(troops)})")

if "battle_cards.json" in loaded:
    cards = loaded["battle_cards.json"]["cards"]
    if not any(c["kind"] == 0 for c in cards): err("No command cards")
    if not any(c["id"] == "card_butter_grease_curse" for c in cards): err("Missing boss card")
    ok(f"Cards OK ({len(cards)})")

scene = (root / "Assets/_Project/Scenes/WorldMap.unity").read_text()
meta = (root / "Assets/_Project/Scripts/Bootstrap/WorldMapEntry.cs.meta").read_text()
guid = re.search(r"guid: ([a-f0-9]+)", meta).group(1)
if guid not in scene: err("WorldMapEntry guid not wired in WorldMap.unity")
else: ok("WorldMapEntry wired")
if "AppRoot" not in scene: err("AppRoot missing")
else: ok("AppRoot present")

scripts = list((root / "Assets/_Project/Scripts").rglob("*.cs"))
types = defaultdict(list)
for sp in scripts:
    t = sp.read_text()
    if t.count("{") != t.count("}"):
        err(f"Brace mismatch {sp.relative_to(root)}")
    for m in re.finditer(r"\b(class|enum|struct)\s+(\w+)", t):
        types[m.group(2)].append(str(sp.relative_to(root)))
for name, paths in types.items():
    if len(paths) > 1: err(f"Duplicate type {name}: {paths}")
for need in ["GameState", "AppFlow", "WorldMapUI", "BattleUI", "JourneyController"]:
    if need not in types: err(f"Missing {need}")
    else: ok(f"Type {need}")

print("=== PASS ===")
for x in oks: print("+", x)
print("=== WARN ===")
for x in warns: print("!", x)
print("=== FAIL ===")
for x in errors: print("X", x)
print(f"\n{len(errors)} errors / {len(warns)} warns / {len(oks)} ok")
sys.exit(1 if errors else 0)
