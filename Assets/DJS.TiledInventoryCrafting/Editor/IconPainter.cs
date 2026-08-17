using System;
using UnityEngine;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Procedural icon renderer for the demo content. Paints anti-aliased SDF shapes
    /// (circles, capsules, rounded rects) with top-left directional lighting and an
    /// edge bevel over a rounded plate, so generated icons read like real game art
    /// instead of flat placeholder swatches. Editor-only: the demo content builder
    /// bakes the result to PNG.
    /// </summary>
    internal sealed class IconPainter
    {
        private readonly int size;
        private readonly Color[] pixels;

        public IconPainter(int size)
        {
            this.size = size;
            pixels = new Color[size * size];
        }

        /// <summary>Create the texture, flipping rows so screen space (y=0 top) maps
        /// to texture space (row 0 = rendered bottom).</summary>
        public Texture2D Build(string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = name;
            var flipped = new Color[size * size];
            for (int y = 0; y < size; y++)
                Array.Copy(pixels, y * size, flipped, (size - 1 - y) * size, size);
            tex.SetPixels(flipped);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------- SDF primitives

        private static float SdCircle(float px, float py, float cx, float cy, float r)
        {
            float dx = px - cx, dy = py - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        private static float SdRoundRectRel(float rx, float ry, float hw, float hh, float r)
        {
            float dx = Mathf.Abs(rx) - (hw - r);
            float dy = Mathf.Abs(ry) - (hh - r);
            float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        private static float SdRoundRect(float px, float py, float cx, float cy, float hw, float hh, float r)
            => SdRoundRectRel(px - cx, py - cy, hw, hh, r);

        private static float SdRoundRectRot(float px, float py, float cx, float cy, float hw, float hh, float r, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            float dx = px - cx, dy = py - cy;
            return SdRoundRectRel(dx * cos - dy * sin, dx * sin + dy * cos, hw, hh, r);
        }

        private static float SdCapsule(float px, float py, float ax, float ay, float bx, float by, float r)
        {
            float pax = px - ax, pay = py - ay;
            float bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        // ------------------------------------------------------------- painting

        private void Blend(int x, int y, Color src, float a)
        {
            if (a <= 0f) return;
            int i = y * size + x;
            pixels[i] = pixels[i] * (1f - a) + src * a;
        }

        /// <summary>Paint the background plate: rounded square, vertical gradient, edge
        /// vignette and a rim light along the top edge. All coordinates are screen space
        /// (y = 0 is the top of the icon).</summary>
        public void Plate(Color top, Color bottom, Color rim)
        {
            float half = size * 0.5f;
            float inset = size * 0.035f;
            float r = size * 0.16f;
            float edge = size * 0.06f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = SdRoundRect(x, y, half, half, half - inset, half - inset, r);
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov <= 0f) continue;

                    float t = y / (float)size;
                    Color c = Color.Lerp(bottom, top, t);
                    c *= Mathf.Lerp(0.86f, 1.08f, Mathf.Clamp01(-d / edge));
                    // soft rim light along the top edge
                    if (y < half)
                    {
                        float rimAmt = Mathf.Clamp01(-d / 2.5f);
                        c = Color.Lerp(c, rim, rimAmt * 0.4f);
                    }
                    Blend(x, y, c, cov);
                }
            }
        }

        /// <summary>
        /// Paint an anti-aliased shape given by its SDF, with top-left directional
        /// lighting and an interior bevel. <paramref name="alpha"/> lets shapes be
        /// translucent (e.g. glass).
        /// </summary>
        public void Fill(Func<float, float, float> sdf, Color color, float alpha = 1f, float bevel = 3.5f)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = sdf(x, y);
                    float cov = 0.5f - d;
                    if (cov <= 0f) continue;
                    if (cov > 1f) cov = 1f;

                    // finite-difference normal → top-left lighting
                    float light = 0.68f;
                    float nx = sdf(x + 1, y) - sdf(x - 1, y);
                    float ny = sdf(x, y + 1) - sdf(x, y - 1);
                    float len = Mathf.Sqrt(nx * nx + ny * ny);
                    if (len > 1e-4f)
                        light += 0.34f * Mathf.Max(0f, (nx * -0.55f + ny * 0.84f) / len);
                    // bevel: darker at the silhouette edge, brighter toward the interior
                    float shade = Mathf.Lerp(0.80f, 1.12f, Mathf.Clamp01(-d / bevel));

                    Blend(x, y, color * (light * shade), cov * alpha);
                }
            }
        }

        public void FillCircle(float cx, float cy, float r, Color c, float alpha = 1f)
            => Fill((px, py) => SdCircle(px, py, cx, cy, r), c, alpha);

        public void FillRoundRect(float cx, float cy, float hw, float hh, float r, Color c, float alpha = 1f)
            => Fill((px, py) => SdRoundRect(px, py, cx, cy, hw, hh, r), c, alpha);

        public void FillRoundRectRot(float cx, float cy, float hw, float hh, float r, float angle, Color c, float alpha = 1f)
            => Fill((px, py) => SdRoundRectRot(px, py, cx, cy, hw, hh, r, angle), c, alpha);

        public void FillCapsule(float ax, float ay, float bx, float by, float r, Color c, float alpha = 1f)
            => Fill((px, py) => SdCapsule(px, py, ax, ay, bx, by, r), c, alpha);

        public void FillRing(float cx, float cy, float r, float thickness, Color c, float alpha = 1f)
            => Fill((px, py) => Mathf.Abs(SdCircle(px, py, cx, cy, r)) - thickness * 0.5f, c, alpha);
    }
}
