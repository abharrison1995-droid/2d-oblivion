using System.Collections.Generic;
using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Loads character portraits from Resources/Portraits/{portraitId}.
    /// Missing art is not an error — callers fall back to a placeholder.
    /// </summary>
    public static class PortraitLoader
    {
        static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Load(string portraitId) =>
            string.IsNullOrEmpty(portraitId) ? null : LoadRaw("Portraits/" + portraitId);

        /// <summary>Loads a sprite from any Resources path (e.g. "Encounters/weather"), not just Portraits/.</summary>
        public static Sprite LoadRaw(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (Cache.TryGetValue(resourcePath, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(resourcePath);
            Cache[resourcePath] = sprite; // cache misses too, so a missing file isn't re-queried every call
            return sprite;
        }

        public static Color PlaceholderColor(string portraitId)
        {
            var hash = 0;
            foreach (var c in portraitId)
                hash = hash * 31 + c;
            var hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.45f, 0.55f);
        }

        public static string Initials(string portraitId)
        {
            var parts = portraitId.Replace('_', ' ').Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, Mathf.Min(2, parts[0].Length)).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[1][0]).ToUpperInvariant();
        }
    }
}
