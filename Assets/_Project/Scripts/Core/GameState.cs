using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Session singleton. Holds party, map, quest, and service directors.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public PartyState Party { get; private set; }
        public CharacterStats Hero { get; private set; }
        public WorldGraph Map { get; private set; }
        public TravelDirector Travel { get; private set; }
        public JourneyController Journey { get; private set; }
        public WorldPartyDirector WorldParties { get; private set; }
        public BattleDirector Battle { get; private set; }
        public EconomyService Economy { get; private set; }
        public MarketService Market { get; private set; }
        public StolenItemQuestController Act1Quest { get; private set; }
        public QuestBoardService QuestBoard { get; private set; }
        public FactionDiplomacy Diplomacy { get; private set; }
        public Dictionary<string, SettlementState> Settlements { get; } = new();
        public Dictionary<string, int> BanditCampLastRaidDay { get; } = new();
        public List<string> PendingNotifications { get; } = new();

        public TroopRosterData TroopRoster { get; private set; }
        public EconomyCatalog Catalog { get; private set; }
        public BattleCardCatalogData CardCatalog { get; private set; }
        public CompanionCatalog Companions { get; private set; }
        public System.Random Rng { get; private set; } = new();

        /// <summary>Monotonic counter backing prisoner ids — Party.prisoners.Count isn't safe to
        /// use for this since it can shrink (recruit/sell/release) between two captures on the
        /// same day, which risked two different prisoners sharing an id.</summary>
        public int PrisonerSequence { get; set; }

        public string NextPrisonerId() => $"bandit_captive_{Party.day}_{PrisonerSequence++}";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Party = new PartyState();
            Hero = new CharacterStats();
            Map = new WorldGraph();
            Travel = new TravelDirector();
            Journey = new JourneyController();
            WorldParties = new WorldPartyDirector();
            Battle = new BattleDirector();
            Economy = new EconomyService();
            Market = new MarketService();
            Act1Quest = new StolenItemQuestController();
            QuestBoard = new QuestBoardService();
            Diplomacy = new FactionDiplomacy();
        }

        public static string FactionName(FactionId f) => f switch
        {
            FactionId.ButterKlanBoys => "Butter Klan Boys",
            FactionId.RaXaelDynasty => "Ra-Xael Dynasty",
            FactionId.SmallSpine => "Small Spine",
            FactionId.LongSpines => "Long Spines",
            _ => f.ToString()
        };

        void OnWarStateChanged(FactionId a, FactionId b, bool atWar)
        {
            PendingNotifications.Add(atWar
                ? $"War! {FactionName(a)} and {FactionName(b)} take up arms against each other."
                : $"Peace: {FactionName(a)} and {FactionName(b)} lay down their arms.");
        }

        public void BindData(WorldMapData map, TroopRosterData troops, EconomyCatalog catalog, BattleCardCatalogData cards = null, CompanionCatalog companions = null, QuestTemplateCatalog questTemplates = null)
        {
            TroopRoster = troops;
            Catalog = catalog;
            CardCatalog = cards;
            Companions = companions;
            if (questTemplates != null)
                QuestBoard.LoadTemplates(questTemplates);
            Map.Load(map);
            Economy.LoadFood(catalog.foods);
            Economy.LoadTroops(troops.troops);
            Market.SetRng(Rng);
            Market.LoadItems(catalog.items);
            if (cards?.cards != null)
                Battle.LoadCatalog(cards.cards);

            foreach (var node in map.nodes)
            {
                if (node.hasRecruitment || node.hasStore || node.type == NodeType.Village)
                    Market.EnsureMarket(node);
            }

            WorldParties.SeedVoidovia(Map, Rng);
            QuestBoard.TickDay(Map, Rng, Party.day > 0 ? Party.day : 1);

            Act1Quest.Log += msg => Debug.Log($"[Quest] {msg}");
            Act1Quest.LairSpawnRequested += (node, road) =>
            {
                if (Map.TryGetNode(node.parentSettlementId, out var parent))
                    node.mapPosition = parent.mapPosition + new Vector2(0.6f, -0.4f);
                Map.AddTemporaryNode(node, road);
            };

            QuestBoard.QuestCompleted += OnQuestCompleted;

            // Seed starting wars before subscribing, so the opening Voidovia–Butter conflict is treated
            // as the status quo rather than breaking news the moment the game boots.
            Diplomacy.Seed();
            Diplomacy.WarStateChanged += OnWarStateChanged;
        }

        void OnQuestCompleted(QuestInstance inst)
        {
            if (inst.type == QuestTemplateType.BountyHunt && !Party.firstBountyHuntDone)
            {
                Party.firstBountyHuntDone = true;
                if (!Party.companionIds.Contains("kestrel"))
                {
                    Party.companionIds.Add("kestrel");
                    PendingNotifications.Add("Kestrel, a reformed bounty, asks to join your company.");
                }
            }
            else if (inst.type == QuestTemplateType.TroopLevy && !Party.firstTroopLevyDone)
            {
                Party.firstTroopLevyDone = true;
                if (!Party.companionIds.Contains("brother_ansel"))
                {
                    Party.companionIds.Add("brother_ansel");
                    PendingNotifications.Add("Brother Ansel, grateful for a levy well handled, asks to join your company.");
                }
            }
        }

        /// <summary>Current prosperity of a settlement (baseline if it has no state yet). Used by the
        /// market to scale purse depth and recruit stock without every node needing a SettlementState.</summary>
        public float ProsperityOf(string nodeId) =>
            Settlements.TryGetValue(nodeId, out var s) ? s.prosperity : GameConstants.ProsperityBaseline;

        void RecoverProsperityDaily()
        {
            foreach (var s in Settlements.Values)
                if (s.prosperity < GameConstants.ProsperityBaseline)
                    s.AddProsperity(GameConstants.ProsperityDailyRecovery);
        }

        public SettlementState GetOrCreateSettlement(MapNodeData node)
        {
            if (!Settlements.TryGetValue(node.id, out var settlement))
            {
                settlement = new SettlementState(node.id, node.displayName);
                Settlements[node.id] = settlement;
            }

            return settlement;
        }

        public void ApplyCharacterCreation(CharacterCreationResult creation)
        {
            Hero.name = creation.heroName;
            Hero.originId = $"{creation.family}_{creation.childhood}_{creation.moose}";
            Hero.combat = creation.combat;
            Hero.leadership = creation.leadership;
            Hero.tactics = creation.tactics;
            Hero.trade = creation.trade;
            Hero.scouting = creation.scouting;

            ApplyStartKit(creation.spawnNodeId, creation.startingGold, creation.startingTroopIds, creation.wantedLean);

            if (creation.moose == MooseChoice.KillForMeat)
                Party.food.Add(new InventoryStack { itemId = "meat", count = 3 });

            if (creation.healerAffinity)
                Party.AddRelation(FactionId.Healers, 5);
            if (creation.horseAffinity)
                Party.AddRelation(FactionId.Nomads, 5);

            Debug.Log($"[Origin] {creation.originSummary}");
        }

        void ApplyStartKit(string spawnNodeId, int gold, string[] troopIds, bool wantedLean)
        {
            Party.currentNodeId = spawnNodeId;
            Party.gold = gold;
            Party.troops.Clear();
            Party.food.Clear();
            Party.inventory.Clear();
            Party.powerCards.Clear();
            Party.companionIds.Clear();
            Party.equippedWeaponId = null;
            Party.equippedArmourId = null;
            foreach (var troopId in troopIds)
                AddTroop(troopId, 1);

            Party.food.Add(new InventoryStack { itemId = "grain", count = 8 });
            Party.food.Add(new InventoryStack { itemId = "bread", count = 4 });
            Party.AddInventory("serviceable_sword", 1);
            Party.AddInventory("leather_jack", 1);
            Party.companionIds.Add("bangkok_kuo");
            Party.relations[FactionId.Voidovia] = 0;
            Party.reputation = ReputationFlag.Good;
            if (wantedLean)
                Party.reputation |= ReputationFlag.WantedInVoidovia;
        }

        /// <summary>Max men the warband can hold: base + Leadership × per-point. Makes Leadership a
        /// real investment and turns "recruit more vs. upgrade what you have" into a live decision.</summary>
        public int MaxPartySize => GameConstants.BasePartySize + Hero.leadership * GameConstants.PartySizePerLeadership;

        /// <summary>Everyone the cap counts: fighting troops plus wounded (both occupy the warband).</summary>
        public int PartyHeadcount => Party.TotalMen + Party.TotalWounded;

        /// <summary>Gate for the recruitment paths (market, buildings, bandit hire, prisoner recruit).
        /// Fails when adding <paramref name="count"/> men would exceed MaxPartySize. Other inflows
        /// (training/wounded returning — men you already own) intentionally skip this and are caught
        /// instead by the over-cap morale bleed in OnNewDay.</summary>
        public bool CanRecruit(int count, out string reason)
        {
            if (PartyHeadcount + count <= MaxPartySize)
            {
                reason = "";
                return true;
            }

            reason = $"Warband full ({PartyHeadcount}/{MaxPartySize}). Raise Leadership or move men on before recruiting more.";
            return false;
        }

        public void AddTroop(string troopId, int count)
        {
            foreach (var stack in Party.troops)
            {
                if (stack.troopId != troopId)
                    continue;
                stack.count += count;
                return;
            }

            Party.troops.Add(new TroopStack { troopId = troopId, count = count });
        }

        /// <summary>
        /// Queues count units of troopId for training into their next tier. Costs gold and
        /// Warband Experience (same amount, both currencies) up front; the units leave the
        /// fighting roster until GameConstants.TroopTrainingDays pass and OnNewDay resolves them.
        /// </summary>
        public bool TryStartTraining(string troopId, int count, out string log)
        {
            log = "";
            if (!TroopRoster.TryGet(troopId, out var def) || string.IsNullOrEmpty(def.upgradesToId))
            {
                log = "Nothing to train that troop into.";
                return false;
            }

            var stack = Party.troops.Find(s => s.troopId == troopId);
            var available = stack?.count ?? 0;
            if (count <= 0 || available < count)
            {
                log = $"You don't have {count}× {def.displayName} to train.";
                return false;
            }

            var cost = def.upgradeGold * count;
            if (Party.gold < cost || Party.warbandExperience < cost)
            {
                log = $"Need {cost}g and {cost} Warband Experience to train {count}× {def.displayName} (have {Party.gold}g, {Party.warbandExperience} WE).";
                return false;
            }

            Party.gold -= cost;
            Party.warbandExperience -= cost;
            stack.count -= count;
            if (stack.count <= 0)
                Party.troops.Remove(stack);

            var trainDays = Mathf.CeilToInt(GameConstants.TroopTrainingDays);
            Party.trainingJobs.Add(new TrainingJob
            {
                sourceTroopId = troopId,
                targetTroopId = def.upgradesToId,
                count = count,
                completesOnDay = Party.day + trainDays
            });

            var targetName = TroopRoster.TryGet(def.upgradesToId, out var targetDef) ? targetDef.displayName : def.upgradesToId;
            log = $"{count}× {def.displayName} begin training into {targetName} ({cost}g, {cost} WE) — ready in {trainDays}d.";
            return true;
        }

        void ResolveTrainingCompletions()
        {
            for (var i = Party.trainingJobs.Count - 1; i >= 0; i--)
            {
                var job = Party.trainingJobs[i];
                if (job.completesOnDay > Party.day)
                    continue;

                AddTroop(job.targetTroopId, job.count);
                var name = TroopRoster.TryGet(job.targetTroopId, out var def) ? def.displayName : job.targetTroopId;
                PendingNotifications.Add($"Training complete: {job.count}× {name} ready for duty.");
                Party.trainingJobs.RemoveAt(i);
            }
        }

        public bool TryOfferMercenaryContract(out string message)
        {
            if (Party.isVoidoviaMercenary)
            {
                message = "You already ride for Voidovia's purse.";
                return false;
            }

            Party.isVoidoviaMercenary = true;
            Travel.SevereRaidsUnlocked = true;
            message =
                $"Lord Void offers a mercenary contract: {GameConstants.LordVoidMercenaryPurseMin}–{GameConstants.LordVoidMercenaryPurseMax} gold per week. Enough for some wages and food — not a deathball.";
            return true;
        }

        public bool TryOfferVassalage(out string message)
        {
            if (!Party.CanBecomeVassal())
            {
                message =
                    $"Need mercenary standing, relation ≥ {GameConstants.VassalRelationThreshold}, and good reputation.";
                return false;
            }

            Party.isVoidoviaVassal = true;
            message = "Lord Void names you vassal. Land talk comes later — but the dynasty door is open.";
            return true;
        }

        public int RollMercenaryPurse() =>
            Rng.Next(GameConstants.LordVoidMercenaryPurseMin, GameConstants.LordVoidMercenaryPurseMax + 1);

        public Vector2 PlayerMapPosition()
        {
            if (Journey.IsActive)
                return Journey.CurrentMapPos;
            if (Map.TryGetNode(Party.currentNodeId, out var n))
                return n.mapPosition;
            return Vector2.zero;
        }

        public bool CanRaidCamp(string campId, out int daysLeft)
        {
            daysLeft = 0;
            if (!BanditCampLastRaidDay.TryGetValue(campId, out var lastDay))
                return true;
            var nextDay = lastDay + GameConstants.BanditCampRaidCooldownDays;
            if (Party.day >= nextDay)
                return true;
            daysLeft = nextDay - Party.day;
            return false;
        }

        public void MarkCampRaided(string campId) => BanditCampLastRaidDay[campId] = Party.day;

        /// <summary>Call whenever Party.day is incremented, from wherever that happens.</summary>
        public void OnNewDay()
        {
            Market?.TickDay(Rng);
            QuestBoard?.TickDay(Map, Rng, Party.day);
            TickFactionDynamics();
            WorldParties.OnNewDay();
            RecoverProsperityDaily();
            Party.warbandExperience += Hero.WarbandExperienceDailyGain();
            ResolveTrainingCompletions();
            RecoverWoundedDaily();
            BleedOverCapMorale();
            PayWeeklyUpkeep();
        }

        /// <summary>An over-strength warband is unruly: past MaxPartySize, morale bleeds each day until
        /// you shed men or grow your Leadership cap. The universal backstop behind the recruit gate.</summary>
        void BleedOverCapMorale()
        {
            if (PartyHeadcount <= MaxPartySize) return;
            Party.AddMorale(-GameConstants.OverCapDailyMoraleBleed);
            PendingNotifications.Add(
                $"Warband over strength ({PartyHeadcount}/{MaxPartySize}) — the camp is unruly and morale slips.");
        }

        /// <summary>Each day a share of the wounded mend back into the fighting roster — faster while
        /// resting in a settlement than out on the road. Always at least one recovers so a lone
        /// wounded man never lingers forever.</summary>
        void RecoverWoundedDaily()
        {
            var totalWounded = Party.TotalWounded;
            if (totalWounded <= 0) return;

            var atSettlement = !Journey.IsActive
                               && Map.TryGetNode(Party.currentNodeId, out var node)
                               && node.type is not (NodeType.QuestLair or NodeType.BanditCamp or NodeType.Wilderness);
            var rate = GameConstants.WoundedDailyRecoveryRate
                       + (atSettlement ? GameConstants.WoundedSettlementRecoveryBonus : 0f)
                       + CompanionBonuses.FieldMedicineBonus();

            var toRecover = Mathf.Clamp(Mathf.CeilToInt(totalWounded * rate), 1, totalWounded);
            var recovered = 0;
            while (recovered < toRecover && Party.wounded.Count > 0)
            {
                var stack = Party.wounded[Party.wounded.Count - 1];
                stack.count--;
                AddTroop(stack.troopId, 1);
                recovered++;
                if (stack.count <= 0)
                    Party.wounded.RemoveAt(Party.wounded.Count - 1);
            }

            if (recovered > 0)
                PendingNotifications.Add(
                    $"{recovered} wounded {(recovered == 1 ? "man returns" : "men return")} to the ranks{(atSettlement ? " — rest speeds the mending." : ".")}");
        }

        /// <summary>
        /// Weekly payday, fired once per 7-day boundary from OnNewDay (previously this lived only
        /// in the debug HUD, so in normal play troops cost nothing and the mercenary contract paid
        /// nothing). Missed wages cost morale — the pressure that makes the economy a loop.
        /// </summary>
        void PayWeeklyUpkeep()
        {
            if (Party.day <= 0 || Party.day % 7 != 0)
                return;

            var wagesUnpaid = false;
            var wages = Economy.WeeklyWageBill(Party);
            if (wages > 0)
            {
                if (Party.gold >= wages)
                {
                    Party.gold -= wages;
                    PendingNotifications.Add($"Payday: wages of {wages}g settled ({Party.gold}g left).");
                }
                else
                {
                    var shortfall = wages - Party.gold;
                    Party.gold = 0;
                    wagesUnpaid = true;
                    Party.AddMorale(-GameConstants.UnpaidWageMoralePenalty);
                    PendingNotifications.Add($"Wages unpaid — short {shortfall}g. The men grumble; morale drops.");
                }
            }

            if (Party.isVoidoviaMercenary)
            {
                var purse = RollMercenaryPurse();
                Party.gold += purse;
                PendingNotifications.Add($"Lord Void's purse arrives: +{purse}g.");
            }

            RollDesertion(wagesUnpaid);
        }

        /// <summary>
        /// Weekly desertion check (payday-tied). Low morale is the driver; a missed payroll piles on.
        /// Deserters are the weakest men first — the rabble slips off before your veterans do — so
        /// this bites the roster you'd least want to lose last, mirroring how battle casualties work.
        /// </summary>
        void RollDesertion(bool wagesUnpaid)
        {
            if (Party.TotalMen <= 0) return;

            var moraleFactor = Mathf.Clamp01(
                (GameConstants.DesertionMoraleFloor - Party.morale) / GameConstants.DesertionMoraleFloor);
            var risk = moraleFactor * GameConstants.DesertionRiskAtZeroMorale;
            if (wagesUnpaid)
                risk += GameConstants.DesertionUnpaidBonus;

            if (risk <= 0f || Rng.NextDouble() >= risk)
                return;

            var count = Mathf.Min(
                GameConstants.DesertionMaxPerWeek,
                1 + Mathf.FloorToInt(Party.TotalMen / 20f));
            var deserted = RemoveWeakestMen(count);
            if (deserted > 0)
                PendingNotifications.Add(
                    $"Desertion: {deserted} {(deserted == 1 ? "man slips" : "men slip")} away in the night. Keep them paid and fed.");
        }

        /// <summary>Removes up to <paramref name="count"/> men, draining the weakest troop tiers first
        /// (ranked by combat power from the roster). Returns how many actually left.</summary>
        int RemoveWeakestMen(int count)
        {
            var removed = 0;
            while (removed < count && Party.TotalMen > 0)
            {
                TroopStack weakest = null;
                var weakestPower = float.MaxValue;
                foreach (var stack in Party.troops)
                {
                    if (stack.count <= 0) continue;
                    var power = TroopRoster != null && TroopRoster.TryGet(stack.troopId, out var def)
                        ? def.melee + def.ranged + def.armour
                        : 0;
                    if (power >= weakestPower) continue;
                    weakestPower = power;
                    weakest = stack;
                }

                if (weakest == null) break;
                weakest.count--;
                removed++;
                if (weakest.count <= 0)
                    Party.troops.Remove(weakest);
            }

            return removed;
        }

        static readonly FactionId[] IncidentFactions =
        {
            FactionId.Voidovia, FactionId.ButterKlanBoys, FactionId.RaXaelDynasty, FactionId.SmallSpine,
            FactionId.LongSpines, FactionId.Orthodoxy, FactionId.Nomads, FactionId.Healers, FactionId.Traders, FactionId.Bandits
        };

        void TickFactionDynamics()
        {
            if (Party.day % GameConstants.FactionDecayIntervalDays == 0)
                DecayAllRelations();

            if (Rng.NextDouble() < GameConstants.FactionIncidentChancePerDay)
                RollFactionIncident();

            Diplomacy.TickDay(Rng);
        }

        void DecayAllRelations()
        {
            var keys = new List<FactionId>(Party.relations.Keys);
            foreach (var f in keys)
            {
                var v = Party.relations[f];
                if (v > 0) Party.relations[f] = Mathf.Max(0, v - 1);
                else if (v < 0) Party.relations[f] = Mathf.Min(0, v + 1);
            }
        }

        void RollFactionIncident()
        {
            var faction = IncidentFactions[Rng.Next(IncidentFactions.Length)];
            var good = Rng.NextDouble() < 0.5;
            var delta = good ? GameConstants.FactionIncidentMagnitude : -GameConstants.FactionIncidentMagnitude;
            Party.AddRelation(faction, delta);
            PendingNotifications.Add(good
                ? $"Word arrives of {faction} goodwill toward your company (+{GameConstants.FactionIncidentMagnitude} relation)."
                : $"A border incident sours relations with {faction} ({-GameConstants.FactionIncidentMagnitude} relation).");
        }

        public MapNodeData FindNearestBanditCamp(Vector2 pos)
        {
            MapNodeData nearest = null;
            var bestDist = float.MaxValue;
            foreach (var node in Map.Nodes.Values)
            {
                if (node.type != NodeType.BanditCamp) continue;
                var d = (node.mapPosition - pos).sqrMagnitude;
                if (d >= bestDist) continue;
                bestDist = d;
                nearest = node;
            }

            return nearest;
        }
    }
}
