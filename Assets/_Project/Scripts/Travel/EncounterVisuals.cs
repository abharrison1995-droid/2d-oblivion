using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Per-encounter-kind color + placeholder art hook, so each travel event reads
    /// distinctly even before real art exists in Resources/Encounters/.
    /// </summary>
    public static class EncounterVisuals
    {
        public static Color AccentColor(TravelEncounterKind kind) => kind switch
        {
            TravelEncounterKind.Rumour => new Color(0.5f, 0.58f, 0.78f),
            TravelEncounterKind.Trader => new Color(0.82f, 0.68f, 0.28f),
            TravelEncounterKind.Healers => new Color(0.42f, 0.72f, 0.48f),
            TravelEncounterKind.MinorThieves => new Color(0.78f, 0.55f, 0.22f),
            TravelEncounterKind.Refugees => new Color(0.68f, 0.62f, 0.5f),
            TravelEncounterKind.Weather => new Color(0.42f, 0.55f, 0.68f),
            TravelEncounterKind.BanditAmbush => new Color(0.72f, 0.24f, 0.2f),
            TravelEncounterKind.ButterRaid => new Color(0.5f, 0.12f, 0.14f),
            _ => new Color(0.45f, 0.45f, 0.45f)
        };

        public static string ResourcePath(TravelEncounterKind kind) => "Encounters/" + kind.ToString().ToLowerInvariant();

        public static string Abbrev(TravelEncounterKind kind) => kind switch
        {
            TravelEncounterKind.Rumour => "RM",
            TravelEncounterKind.Trader => "TR",
            TravelEncounterKind.Healers => "HL",
            TravelEncounterKind.MinorThieves => "TH",
            TravelEncounterKind.Refugees => "RF",
            TravelEncounterKind.Weather => "WX",
            TravelEncounterKind.BanditAmbush => "AM",
            TravelEncounterKind.ButterRaid => "BR",
            _ => "??"
        };
    }
}
