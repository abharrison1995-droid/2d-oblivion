using UnityEngine;
using UnityEngine.UI;

namespace Voidovia
{
    /// <summary>
    /// Generates a small seamless-tiling noise texture for the world map's ground fill, so it
    /// reads as mottled terrain instead of a flat color. One texture, generated once and cached,
    /// then repeated via Image.Type.Tiled — scales to any map size for free, no regeneration
    /// needed if the map area grows later.
    /// </summary>
    public static class ProceduralGround
    {
        const int TileResolution = 128;
        const float TileWorldSize = 300f; // UI units per tile repeat

        // Faint cartographer's grid baked into the tile — lines at the tile edges and midpoints, so it
        // repeats seamlessly (two lines per tile → a grid cell every ~150 UI units). Subtle, for texture.
        const int GridSubdivisions = 2;
        const float GridLineWidthPx = 1.4f;
        const float GridStrength = 0.22f;
        static readonly Color GridColor = new(0.10f, 0.13f, 0.11f);

        static Sprite _cached;

        public static Sprite GetGroundTile(int seed = 1337)
        {
            if (_cached != null) return _cached;

            var tex = new Texture2D(TileResolution, TileResolution, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            var rng = new System.Random(seed);
            var offsetX = (float)rng.NextDouble() * 1000f;
            var offsetY = (float)rng.NextDouble() * 1000f;

            var dark = new Color(0.14f, 0.19f, 0.15f);
            var light = new Color(0.24f, 0.30f, 0.25f);

            for (var y = 0; y < TileResolution; y++)
            {
                for (var x = 0; x < TileResolution; x++)
                {
                    var u = x / (float)TileResolution;
                    var v = y / (float)TileResolution;
                    var n = LayeredTileableNoise(u, v, offsetX, offsetY);
                    var terrain = Color.Lerp(dark, light, n);
                    var grid = Mathf.Max(GridFactor(x), GridFactor(y));
                    tex.SetPixel(x, y, Color.Lerp(terrain, GridColor, grid * GridStrength));
                }
            }

            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, TileResolution, TileResolution),
                new Vector2(0.5f, 0.5f), TileResolution / TileWorldSize);
            return _cached;
        }

        /// <summary>Applies the ground tile sprite to an already-created panel Image (tiled, untinted).</summary>
        public static void Apply(Image image, int seed = 1337)
        {
            image.sprite = GetGroundTile(seed);
            image.type = Image.Type.Tiled;
            image.color = Color.white;
        }

        /// <summary>Soft-edged grid-line strength (0..1) for one axis coordinate: peaks at each grid line
        /// (tile edge + midpoints) and fades over GridLineWidthPx pixels, so lines read as thin and clean.
        /// Using modulo against a divisor of TileResolution keeps the pattern seamless when tiled.</summary>
        static float GridFactor(int coord)
        {
            var cell = TileResolution / GridSubdivisions; // 64px
            var m = coord % cell;
            var d = Mathf.Min(m, cell - m); // pixels to the nearest grid line
            return d < GridLineWidthPx ? 1f - d / GridLineWidthPx : 0f;
        }

        static float LayeredTileableNoise(float u, float v, float offsetX, float offsetY)
        {
            var total = 0f;
            var amplitude = 0.55f;
            var period = 2f;
            var maxAmplitude = 0f;

            for (var octave = 0; octave < 3; octave++)
            {
                total += TileableNoise(u, v, period, offsetX, offsetY) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= 0.5f;
                period *= 2.2f;
            }

            return Mathf.Clamp01(total / maxAmplitude);
        }

        /// <summary>
        /// Blends noise sampled near the tile's opposite edges so it repeats with no visible
        /// seam — the standard "four corner" trick for making 2D Perlin noise tileable.
        /// </summary>
        static float TileableNoise(float u, float v, float period, float offsetX, float offsetY)
        {
            var x = u * period;
            var y = v * period;

            var n00 = Mathf.PerlinNoise(x + offsetX, y + offsetY);
            var n10 = Mathf.PerlinNoise(x - period + offsetX, y + offsetY);
            var n01 = Mathf.PerlinNoise(x + offsetX, y - period + offsetY);
            var n11 = Mathf.PerlinNoise(x - period + offsetX, y - period + offsetY);

            var top = Mathf.Lerp(n00, n10, u);
            var bottom = Mathf.Lerp(n01, n11, u);
            return Mathf.Lerp(top, bottom, v);
        }
    }
}
