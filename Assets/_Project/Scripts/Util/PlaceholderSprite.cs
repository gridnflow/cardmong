using UnityEngine;

namespace Cardmong.Battle
{
    // Generates simple circle sprites at runtime for use as placeholder monster art.
    public static class PlaceholderSprite
    {
        private const int Size = 128;
        private static readonly Sprite[] Cache = new Sprite[8];

        public static Sprite Get(int index)
        {
            index = Mathf.Abs(index) % Cache.Length;
            if (Cache[index] != null) return Cache[index];

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[Size * Size];
            float cx = Size / 2f, cy = Size / 2f, r = Size / 2f - 2;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < r - 4)        pixels[y * Size + x] = new Color32(200, 200, 200, 255); // body
                    else if (dist < r)       pixels[y * Size + x] = new Color32(80,  80,  80,  255); // outline
                    else                     pixels[y * Size + x] = new Color32(0,   0,   0,   0);   // transparent
                }
            }

            // Draw two eye dots
            DrawDot(pixels, (int)(cx - 18), (int)(cy + 20), 8, new Color32(40, 40, 40, 255));
            DrawDot(pixels, (int)(cx + 18), (int)(cy + 20), 8, new Color32(40, 40, 40, 255));

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex,
                new Rect(0, 0, Size, Size),
                new Vector2(0.5f, 0.5f),
                100f);

            Cache[index] = sprite;
            return sprite;
        }

        private static void DrawDot(Color32[] pixels, int cx, int cy, int radius, Color32 color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= Size || y < 0 || y >= Size) continue;
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius)
                        pixels[y * Size + x] = color;
                }
            }
        }
    }
}
