# Voidovia — Writing Guide

Reference list of every city and named character currently in the game, for filling in with real flavor text/lore. Everything under "Current text" is what's live in the data files today (`Assets/StreamingAssets/Data/voidovia_map.json`, `factions.json`, `companions.json`) — mostly one-liners or nothing. Write in the blank space; when you're happy with a passage, it goes into the relevant JSON's flavor field (ask and I'll wire it in, or hand-edit the JSON directly — it's plain text).

Node ids and companion/faction ids are included so whatever you write can be matched back to the right entry without ambiguity.

**Status key**: `[DRAFT, not yet wired in]` = written but only living in this doc, not in the game's data files yet. No tag = already live in the JSON.

---

## World Lore & Recurring Terms

Background context that isn't tied to one specific node — referenced across multiple settlement entries.

| Term | Meaning |
|---|---|
| Ghkat Mip | A highly profitable, widely used recreational drug. Lord Void publicly condemns it but secretly profits from it. |
| Kibble | Lord Void's personal euphemism for the local women he picks from during his annual holiday in Voi-D-Nee. |
| Maard Beefcow | The specific cattle breed favored by Lord Void, explicitly bred for massive girth and low intelligence. |

---

## Cities & Settlements

### Voidovia (player's home kingdom) — ruled by Lord Void, The Wide Eyed Beast

| Name | id | Type | Current text | Your notes |
|---|---|---|---|---|
| **Lik-E-Leek** (was Greyledger) | `greyledger` | Capital | "Lord Void's capital. Renowned for its food, reviled for its women, and retarded by Ghkat Mip abuse. A beautiful city of chemically relaxed folk, protected by the iron paw of Lord Void." Has the book store and Act 1's advisor. | |
| **Voi-D-Nee** (was Saltmere) | `saltmere` | Town | "Lord Void's holiday town. Yes. A whole town to holiday. Once a year he spends the month here and enjoys his pick of the kibble. Kibble is what he calls... you guessed it. Excellent grain here by the way." Home to a companion (Old Maro, trade). | |
| **Al-Javvid** (was Red Knoll) | `red_knoll` | Town | "A homage to his parents' heritage. Now home to the Void Choir. A devout lot. Better at drinking than reciting scripture." Home to a companion (Fenn Willowtrack, scout). Parent city to the Ravine Hold bandit den. | |
| **Tails End** (was Millwright) | `millwright` | Town | "Not much to shout about. Premier spot for hunters but known to be the home of banditry for the region." Home to a companion (Widow Yara Cross, drillmaster). | |
| **Here** (was Bastion Holt) | `bastion_holt` | Castle | "'Hilariously' named this due to Lord Void. After storming this settlement to liberate it from the clutches of -them-, he had a little too much of the Mip and had a go at humour." One of three cities that can host the Lord Void audience scene. | |
| **Earl Walsall** (was Ironcauseway) | `ironcauseway` | Castle | "Famous for having two taverns and no hospitals. Haunt of every bard, comic and storyteller within miles." Border fortress — the only roads out to Butter Klan and Orthodoxy territory run through here. | |
| **Beef** (was Ashpond) | `ashpond` | Village | "Home of the Maard Beefcow. Lord Void's choice. Bred for girth and idiocy, both of which it has in spades. Interesting Town Leader too." Act 1: one of the two cities the advisor names (wrong-city path). | |
| Cinderfield | `cinderfield` | Village | No flavor text. | |
| Lowferry | `lowferry` | Village | No flavor text. | |
| Sheepgate | `sheepgate` | Village | Parent city to the Sootmarsh Camp bandit den. | |
| Marshend | `marshend` | Village | Parent city to the Fenmurk Den bandit den. | |
| Tollbar | `tollbar` | Village | Act 1: the *correct* city — the Buttery Lair spawns just outside it. | |
| Foxhollow | `foxhollow` | Village | New this pass, no flavor text yet. | |
| Nettlemarsh | `nettlemarsh` | Village | New this pass, no flavor text yet. | |
| Thistlewick | `thistlewick` | Village | New this pass, no flavor text yet. | |
| Ravensford | `ravensford` | Village | New this pass, no flavor text yet. | |

### Butter Klan Boys — ruled by Rendered Ronk, High Churn-Chief

| Name | id | Type | Current text | Your notes |
|---|---|---|---|---|
| Miregate | `butter_miregate` | Capital | Skeleton — no content beyond map presence. | |
| Butter Hollow | `butter_hollow` | Town | New this pass, no flavor text yet. | |

### Ra-Xael Dynasty — ruled by Regent Ixessa Ra-Xael

| Name | id | Type | Current text | Your notes |
|---|---|---|---|---|
| Ra-Xael Crownhold | `raxael_crown` | Capital | Skeleton — "notes" field literally says "Skeleton only." | |
| Ra-Xael Reach | `raxael_reach` | Town | New this pass, no flavor text yet. | |

### Small Spine — ruled by Elder Vask Cromspine

| Name | id | Type | Current text | Your notes |
|---|---|---|---|---|
| Small Spine Hub | `smallspine_hub` | Town | "Half-kingdom; mountain and culture." | |
| Small Spine Watch | `smallspine_watch` | Village | New this pass, no flavor text yet. | |

### Long Spines — ruled by Elder-Marshal Renna Farlow

| Name | id | Type | Current text | Your notes |
|---|---|---|---|---|
| Long Spine Hub | `longspine_hub` | Town | "Other half; tolerate Small Spine, self-segregated." | |
| Long Spine Reach | `longspine_reach` | Village | New this pass, no flavor text yet. | |

### The Orthodoxy — ruled by High Templar Aurelio Bask

| Name | id | Type | Current text | Your notes |
|---|---|---|---|---|
| Orthodox Bastion | `orthodoxy_bastion` | Capital | "Templars and crusades." | |
| Orthodoxy Chapterhouse | `orthodoxy_chapterhouse` | Town | New this pass, no flavor text yet. | |

### Bandit dens (off-path, not real settlements — no culture/ruler)

| Name | id | Parent city | Your notes |
|---|---|---|---|
| Ravine Hold | `ravine_hold` | Red Knoll | |
| Fenmurk Den | `fenmurk_den` | Marshend | |
| Sootmarsh Camp | `sootmarsh_camp` | Sheepgate | |

---

## Characters

### Companions (recruitable into the player's party)

| Name | id | Role | Current trait text | Your notes |
|---|---|---|---|---|
| Bangkok Kuo | `bangkok_kuo` | Eastern general (free starting companion) | "Loyal; loves the art of war; quotes Sun Tzu in battle pauses." | |
| Sergeant Dell Hoskin | `dell_hoskin` | Quartermaster (found at Greyledger) | "Ex-Voidovia logistics sergeant. Squeezes a wage bill dry and hates waste." | |
| Old Maro, the Ledger-Keeper | `old_maro` | Merchant (found at Saltmere) | "Retired trader who still knows every buyer's soft spot." | |
| Fenn Willowtrack | `fenn_willowtrack` | Scout (found at Red Knoll) | "Reads a road better than most read a book." | |
| Widow Yara Cross | `yara_cross` | Drillmaster (found at Millwright) | "Buried two husbands and a war band. Still barks orders like she means it." | |
| Kestrel | `kestrel` | Reformed bounty (reward for first Bounty Hunt quest) | "Knows where the coin is buried better than most — a reward for running down her old crew." | |
| Brother Ansel | `brother_ansel` | Noble's steward (reward for first Troop Levy quest) | "Knows every recruiter in the region by name — grateful after a levy well handled." | |

### Faction rulers

| Name | Faction | Current text | Your notes |
|---|---|---|---|
| Lord Void, The Wide Eyed Beast | Voidovia | "Brutal reputation, sweet protector. Butter pressure on the border." Central to Act 1 (the player delivers the Buttery Chief to him, gets the mercenary offer). | |
| Rendered Ronk, High Churn-Chief | Butter Klan Boys | Newly named this pass — no other text. | |
| Regent Ixessa Ra-Xael | Ra-Xael Dynasty | Newly named this pass — no other text. | |
| Elder Vask Cromspine | Small Spine | Newly named this pass — no other text. | |
| Elder-Marshal Renna Farlow | Long Spines | Newly named this pass — no other text. | |
| High Templar Aurelio Bask | The Orthodoxy | Newly named this pass — no other text. | |

### Act 1 unique

| Name | id | Role | Current text | Your notes |
|---|---|---|---|---|
| Buttery Chief | `buttery_chief` | Act 1's quest lord — captured at the Buttery Lair, delivered to Lord Void | Only referred to by title, no personal name or backstory written. | |

---

## Notes on what's *not* here

- The player's hero has no fixed name/backstory (player-created at the start of every game) — not a writing target.
- Generic troop-line flavor (militia, archers, etc.) has display names but no lore — could be a future pass if you want unit-level flavor text too, just say so.
- Companions only have the one `traitDescription` field today — if you want richer per-companion bios/backstory beyond a single line, that's a small schema change (`Assets/_Project/Scripts/Party/CompanionDefinition.cs`) I can make whenever you're ready to write more than a line each.
