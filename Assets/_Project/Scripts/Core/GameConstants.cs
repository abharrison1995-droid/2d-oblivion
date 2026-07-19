namespace Voidovia
{
    /// <summary>
    /// Shared balance knobs. Tune in playtest; M&amp;B-like denar scale.
    /// </summary>
    public static class GameConstants
    {
        public const int HoursPerDay = 24;
        public const float BaseTravelHoursPerEdge = 8f;

        public const int LordVoidMercenaryPurseMin = 200;
        public const int LordVoidMercenaryPurseMax = 400;

        public const int VassalRelationThreshold = 30;

        // Party size cap (M&B-style): how many men you can hold is base + Leadership × per-point.
        // Fighting troops AND wounded both count toward it, so a bloody win that leaves many wounded
        // temporarily crowds out recruiting until they mend. Over the cap bleeds morale each day —
        // a soft wall that always bites even when men arrive by paths that skip the recruit check
        // (prisoner recruit, quest rewards). Tune in playtest.
        public const int BasePartySize = 18;
        public const int PartySizePerLeadership = 3;
        public const float OverCapDailyMoraleBleed = 3f;

        // Reaching this relation (or lower) with any faction marks the party Infamous,
        // blocking vassalage via PartyState.CanBecomeVassal.
        public const int InfamyRelationThreshold = -50;

        // Wanted status / bounty. Falling to WantedRelationThreshold with Voidovia outlaws you: a bounty
        // lands on your head, patrols hunt you on the road (chance scales with the bounty), and you must
        // pay it off at a Voidovia settlement to clear your name. Infamy also raises general road danger.
        public const int WantedRelationThreshold = -20;
        public const int WantedStartingBounty = 120;
        public const float WantedPatrolBaseChance = 0.12f;
        public const float WantedPatrolChancePerBounty = 0.0006f; // ×bounty, added to the base
        public const int PatrolBribeCost = 45;
        public const float InfamousDangerBonus = 0.12f; // added to road event chance while Infamous

        public const int OriginBackgroundCount = 4;

        // Upgrade cost bands — spent in BOTH gold and Warband Experience (same numbers,
        // two different currencies). T1 recruits ~10g; a mounted T3→T4 upgrade also needs
        // a stronger mount, priced in as MountUpgradePremium on top of the flat T3→T4 cost.
        public const int UpgradeT1ToT2 = 20;
        public const int UpgradeT2ToT3 = 50;
        public const int UpgradeT3ToT4 = 120;
        public const int MountUpgradePremium = 100;

        // Training queue
        public const float TroopTrainingDays = 1f;

        // Wounded pool: a share of battle casualties are wounded (recoverable), not dead. Winning
        // leaves more men merely wounded; a rout kills a larger share. Wounded heal back into the
        // fighting roster a little each day, faster resting in a settlement. Tune in playtest.
        public const float WoundedFractionVictory = 0.5f;
        public const float WoundedFractionDefeat = 0.25f;
        public const float WoundedDailyRecoveryRate = 0.15f; // fraction of current wounded who mend per day on the road
        public const float WoundedSettlementRecoveryBonus = 0.25f; // added to the rate while resting at a settlement

        // Weekly upkeep: morale hit when the payday wage bill can't be covered.
        public const float UnpaidWageMoralePenalty = 12f;

        // Weekly desertion (payday-tied). Morale below the floor starts the risk climbing toward
        // DesertionRiskAtZeroMorale at morale 0; a missed payroll adds a flat bonus on top. Deserters
        // are the weakest men first, capped per week so a bad week bleeds men without instantly
        // gutting the warband. Tune in playtest alongside wages/morale sources.
        public const float DesertionMoraleFloor = 30f;
        public const float DesertionRiskAtZeroMorale = 0.5f;
        public const float DesertionUnpaidBonus = 0.25f;
        public const int DesertionMaxPerWeek = 3;

        // Bandit captivity + camp raiding
        public const float BanditGoldTheftFraction = 0.2f;
        public const int BanditGoldTheftCap = 512;
        public const float BanditCaptivityHours = 30f;
        public const int BanditCampRaidCooldownDays = 4;
        public const int PrisonerRecruitDelayDays = 2;
        public const string RecruitedBanditTroopId = "void_militia";

        // Prisoner ransom — priced by rank (troop tier) and current relation with the
        // prisoner's own faction; below the threshold they refuse ransom outright.
        public const int PrisonerRansomBasePrice = 30;
        public const int RansomMinRelationThreshold = -20;

        // Generic prisoner capture from any won battle (not just camp raids). A share of the enemy's
        // fallen are taken alive, capped per battle, up to an overall holding capacity. These feed the
        // recruit/ransom/release verbs in the Party panel.
        public const float PrisonerCaptureFraction = 0.25f;
        public const int PrisonersPerBattleCap = 2;
        public const int PrisonerCapacity = 8;

        // Hero leveling. Curve climbs steeply (slow at first) — XpForNextLevel = XpCurveBase * level^XpCurveExponent.
        public const float XpCurveBase = 80f;
        public const float XpCurveExponent = 1.6f;
        public const int SkillPointsPerLevel = 1;
        public const float BattleXpPerEnemyStrength = 0.5f;

        // Warband Experience: a spendable pool (PartyState.warbandExperience) that accrues
        // daily and from battle wins, faster the more skill points you've sunk into it
        // (CharacterStats.warbandExperienceRank).
        public const int BaseWarbandExperiencePerDay = 5;
        public const int WarbandExperiencePerRankBonus = 2;
        public const float WarbandExperiencePerEnemyStrength = 0.3f;

        // Demand Surrender: bandits only. Needs a headcount advantage; Warband Experience
        // (current banked amount) sweetens the odds on top of that.
        public const float SurrenderOutnumberRatio = 1.5f;
        public const float SurrenderBaseChance = 0.3f;
        public const float SurrenderOutnumberBonusScale = 0.25f;
        public const float SurrenderWarbandExperienceBonusCap = 0.2f;
        public const float SurrenderWarbandExperienceSoftCap = 500f;
        public const int SurrenderPrisonerCap = 3;
        public const float SurrenderStrengthPerHead = 15f; // rough per-unit power estimate for XP/WE payout

        // Bandit reputation arc: release captives enough and camps stop being hostile.
        public const int BanditFriendlyRelationThreshold = 30;
        public const int BanditReleaseRelationBoost = 8;
        public const int BanditMercenaryHireCost = 15;

        // Nomad riders: hireable at any tavern once Nomads relation clears the threshold — the sole way
        // to field the Nomad troop tree until you hold a Nomad-controlled settlement.
        public const int NomadHireRelationThreshold = 10;
        public const int NomadHireCost = 40;

        // Personal stat effects — Combat/Leadership/Tactics/Trade were decorative until now.
        // All bonuses are relative to the level-1 baseline of 5 in each stat.
        public const float CombatBattlePowerBonusPerPoint = 0.02f;
        public const float WarbandExperienceBattlePowerBonusCap = 0.15f;
        public const float TacticsCasualtyReductionPerPoint = 0.03f;
        public const float TacticsCasualtyReductionCap = 0.3f;
        public const float LeadershipWageDiscountPerPoint = 0.015f;
        public const float LeadershipWageDiscountCap = 0.3f;
        public const float TradePriceBonusPerPoint = 0.015f;
        public const float TradePriceBonusCap = 0.25f;

        // Loot now scales with the strength of what you actually fought instead of a flat
        // hardcoded drop.
        public const float LootGoldPerEnemyStrength = 0.3f;
        public const float LootGrainPerEnemyStrength = 0.04f;
        public const float LootTrinketChance = 0.5f;

        // Faction war state (faction-vs-faction, not the player's relations). One random belligerent
        // pair may flip per day; the seeded Voidovia–Butter war is always on at the start. AI warbands
        // march on their faction's enemies and skirmish on contact.
        public const float WarStateChangeChancePerDay = 0.14f;
        public const float DeclareWarChance = 0.4f;
        public const float MakePeaceChance = 0.5f;
        public const float WorldPartyContactRange = 0.35f; // in scaled map units, ×MapScaleFactor
        public const int WorldPartySkirmishLoserDamage = 5;
        public const int WorldPartySkirmishWinnerDamage = 2;
        public const int WorldPartyRetreatStrength = 8;   // at/below this a battered band retreats home
        public const int WorldPartyDailyStrengthRegen = 2;
        public const int WorldPartyMaxStrength = 40;

        // Settlement capture: a war band must out-muscle the garrison (by settlement type, hardened by
        // Walls) to take a town rather than just raid it. Taking it costs strength (leaving a garrison).
        public const int GarrisonCapital = 32;
        public const int GarrisonCastle = 28;
        public const int GarrisonTown = 20;
        public const int GarrisonVillage = 10;
        public const int GarrisonWallsPerTier = 5;
        public const int SettlementCaptureStrengthCost = 8;

        // Settlement prosperity (0–100). Raids sack it, a capture sacks it harder, and peace rebuilds it
        // toward the baseline each day. Drives market purse depth and recruit availability.
        public const float ProsperityBaseline = 50f;
        public const float ProsperityDailyRecovery = 2f;
        public const float RaidProsperityHit = 8f;
        public const float SackProsperityHit = 20f;

        // Faction relation dynamics: slow decay toward 0 plus rare random incidents (good or bad)
        // so the world doesn't sit static once you stop actively working a relation.
        public const int FactionDecayIntervalDays = 3;
        public const float FactionIncidentChancePerDay = 0.08f;
        public const int FactionIncidentMagnitude = 3;

        // Quest board: procedural quests offered per settlement, refreshed daily.
        public const int QuestBoardOffersPerSettlement = 2;
        public const float QuestBoardDailyOfferChance = 0.35f; // per empty offer slot, per day
        public const int QuestLevyCountMin = 2;
        public const int QuestLevyCountMax = 5;
        public const float QuestEscortAmbushChance = 0.35f;
        public const int QuestRelationReward = 4; // relation bump with the giver's controlling faction on completion

        // Settlement notables: each settlement's headman/elder has a regard for you (0–100). Higher regard
        // means more willing recruits, a price discount, and — past the upgrade threshold — the settlement
        // fields its culture's next tier. Quests for that settlement raise it; raiding it drops it.
        public const int NotableRelationBaseline = 25;
        public const int QuestNotableReward = 12;
        public const int NotableUpgradeThreshold = 60; // regard at/above which the settlement offers its T2 recruit
        public const float NotablePriceDiscountMax = 0.3f; // up to 30% off recruits at full regard
        public const float NotableStockFactorMin = 0.4f; // recruit-stock multiplier at zero regard (→ ~1.4 at full)
        public const int NotableDispleasureHit = 15; // regard lost when a quest for the settlement fails/expires
    }
}
