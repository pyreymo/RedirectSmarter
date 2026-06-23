using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RedirectSmarter.UI
{
    // Visual-only HUD primitives parked for a possible future overlay.
    internal static class MonitorOverlayStyle
    {
        public const int NoiseDotCount = 20;
        public static readonly Vector2 DefaultSize = new(440, 236);

        public static void DrawFrame(ImDrawListPtr dl, Vector2 p0, Vector2 size, float pulse)
        {
            var p1 = p0 + size;
            var rounding = 16f;
            var glowAlpha = 0.16f + Clamp01(pulse) * 0.18f;

            dl.AddRectFilled(p0, p1, Col(0.02f, 0.07f, 0.12f, 0.55f), rounding);
            dl.AddRectFilled(
                p0 + new Vector2(1, 1),
                new Vector2(p1.X - 1, p0.Y + 52),
                Col(0.25f, 0.75f, 1.0f, 0.10f),
                rounding,
                ImDrawFlags.RoundCornersTop
            );

            dl.AddRect(
                p0 - new Vector2(2),
                p1 + new Vector2(2),
                Col(0.0f, 0.75f, 1.0f, glowAlpha),
                rounding,
                ImDrawFlags.RoundCornersAll,
                2.0f
            );
            dl.AddRect(
                p0,
                p1,
                Col(0.1f, 0.85f, 1.0f, 0.85f),
                rounding,
                ImDrawFlags.RoundCornersAll,
                1.5f
            );
            dl.AddRect(
                p0 + new Vector2(5),
                p1 - new Vector2(5),
                Col(0.5f, 0.95f, 1.0f, 0.18f),
                rounding - 4,
                ImDrawFlags.RoundCornersAll,
                1.0f
            );
        }

        public static void DrawTechDecorations(
            ImDrawListPtr dl,
            Vector2 p0,
            Vector2 size,
            float time,
            ReadOnlySpan<Vector2> noiseDots,
            ReadOnlySpan<float> noisePhases
        )
        {
            var p1 = p0 + size;
            var scanY = p0.Y + 18 + ((time * 42f) % (size.Y - 36));

            dl.AddLine(
                p0 + new Vector2(22, 8),
                new Vector2(p1.X - 22, p0.Y + 8),
                Col(0.55f, 0.95f, 1.0f, 0.35f),
                1.0f
            );
            dl.AddLine(
                new Vector2(p1.X - 58, p0.Y),
                new Vector2(p1.X, p0.Y + 58),
                Col(0.1f, 0.85f, 1.0f, 0.45f),
                1.2f
            );
            dl.AddLine(
                new Vector2(p0.X, p1.Y - 46),
                new Vector2(p0.X + 46, p1.Y),
                Col(0.1f, 0.85f, 1.0f, 0.35f),
                1.2f
            );
            dl.AddRectFilled(
                new Vector2(p0.X + 8, scanY),
                new Vector2(p1.X - 8, scanY + 14),
                Col(0.0f, 0.75f, 1.0f, 0.055f),
                4f
            );

            DrawTickMarks(dl, p0);
            DrawCornerBrackets(dl, p0, p1);
            DrawStatusDot(dl, p0, p1, time);
            DrawNoiseDots(dl, p0, time, noiseDots, noisePhases);
        }

        public static void DrawGlitchText(ImDrawListPtr dl, Vector2 pos, string text)
        {
            dl.AddText(pos + new Vector2(-1, 0), Col(0.0f, 0.35f, 1.0f, 0.55f), text);
            dl.AddText(pos + new Vector2(1, 0), Col(0.0f, 1.0f, 1.0f, 0.45f), text);
            dl.AddText(pos, Col(0.75f, 0.96f, 1.0f, 1.0f), text);
        }

        public static void DrawModuleBox(
            ImDrawListPtr dl,
            Vector2 p0,
            Vector2 size,
            string label,
            bool active
        )
        {
            var p1 = p0 + size;

            dl.AddRectFilled(p0, p1, Col(0.0f, 0.08f, 0.14f, 0.28f), 6f);
            dl.AddRect(p0, p1, Col(0.1f, 0.7f, 1.0f, 0.30f), 6f);
            dl.AddLine(
                p0 + new Vector2(0, 8),
                p0 + new Vector2(0, size.Y - 8),
                active ? Col(0.0f, 1.0f, 0.85f, 0.85f) : Col(0.25f, 0.55f, 0.75f, 0.45f),
                2f
            );
            dl.AddLine(
                p0 + new Vector2(12, size.Y - 8),
                p0 + new Vector2(54, size.Y - 8),
                active ? Col(0.0f, 1.0f, 0.85f, 0.55f) : Col(0.25f, 0.55f, 0.75f, 0.26f),
                1f
            );
            dl.AddText(p0 + new Vector2(10, 7), Col(0.55f, 0.9f, 1.0f, 0.9f), label);
            dl.AddCircleFilled(
                new Vector2(p1.X - 14, p0.Y + 15),
                3f,
                active ? Col(0.0f, 1.0f, 0.65f, 0.9f) : Col(0.7f, 0.7f, 0.7f, 0.45f),
                10
            );
        }

        public static void InitializeNoiseDots(Vector2 size, Span<Vector2> dots, Span<float> phases)
        {
            var random = new Random(0x52534d);

            for (var i = 0; i < dots.Length; i++)
            {
                var edge = random.Next(4);
                var x = 18f + (float)random.NextDouble() * (size.X - 36f);
                var y = 18f + (float)random.NextDouble() * (size.Y - 36f);

                dots[i] = edge switch
                {
                    0 => new Vector2(x, 10f),
                    1 => new Vector2(size.X - 10f, y),
                    2 => new Vector2(x, size.Y - 10f),
                    _ => new Vector2(10f, y),
                };
                phases[i] = (float)random.NextDouble() * MathF.Tau;
            }
        }

        private static void DrawTickMarks(ImDrawListPtr dl, Vector2 p0)
        {
            for (var i = 0; i < 12; i++)
            {
                var x = p0.X + 44 + i * 22;
                var h = i % 3 == 0 ? 8f : 4f;
                dl.AddLine(
                    new Vector2(x, p0.Y + 16),
                    new Vector2(x, p0.Y + 16 + h),
                    Col(0.2f, 0.85f, 1.0f, 0.28f),
                    1f
                );
            }
        }

        private static void DrawCornerBrackets(ImDrawListPtr dl, Vector2 p0, Vector2 p1)
        {
            var c = Col(0.1f, 0.9f, 1.0f, 0.95f);
            var l = 26f;
            var m = 12f;

            dl.AddLine(p0 + new Vector2(m, m), p0 + new Vector2(m + l, m), c, 2f);
            dl.AddLine(p0 + new Vector2(m, m), p0 + new Vector2(m, m + l), c, 2f);
            dl.AddLine(new Vector2(p1.X - m, p0.Y + m), new Vector2(p1.X - m - l, p0.Y + m), c, 2f);
            dl.AddLine(new Vector2(p1.X - m, p0.Y + m), new Vector2(p1.X - m, p0.Y + m + l), c, 2f);
            dl.AddLine(new Vector2(p0.X + m, p1.Y - m), new Vector2(p0.X + m + l, p1.Y - m), c, 2f);
            dl.AddLine(new Vector2(p0.X + m, p1.Y - m), new Vector2(p0.X + m, p1.Y - m - l), c, 2f);
            dl.AddLine(p1 - new Vector2(m, m), p1 - new Vector2(m + l, m), c, 2f);
            dl.AddLine(p1 - new Vector2(m, m), p1 - new Vector2(m, m + l), c, 2f);
        }

        private static void DrawStatusDot(ImDrawListPtr dl, Vector2 p0, Vector2 p1, float time)
        {
            var dotAlpha = 0.45f + 0.35f * MathF.Sin(time * 5.0f);
            var dot = new Vector2(p1.X - 34, p0.Y + 28);
            dl.AddCircleFilled(dot, 4.0f, Col(0.0f, 1.0f, 0.85f, dotAlpha), 12);
            dl.AddCircle(dot, 8.0f, Col(0.0f, 1.0f, 0.85f, dotAlpha * 0.45f), 16, 1.0f);
        }

        private static void DrawNoiseDots(
            ImDrawListPtr dl,
            Vector2 p0,
            float time,
            ReadOnlySpan<Vector2> noiseDots,
            ReadOnlySpan<float> noisePhases
        )
        {
            var count = Math.Min(noiseDots.Length, noisePhases.Length);
            for (var i = 0; i < count; i++)
            {
                var alpha = 0.12f + 0.32f * MathF.Max(0f, MathF.Sin(time * 3.5f + noisePhases[i]));
                dl.AddCircleFilled(p0 + noiseDots[i], 1.3f, Col(0.4f, 0.95f, 1.0f, alpha), 6);
            }
        }

        private static float Clamp01(float value)
        {
            return MathF.Max(0f, MathF.Min(1f, value));
        }

        private static uint Col(float r, float g, float b, float a)
        {
            return ImGui.GetColorU32(new Vector4(r, g, b, a));
        }
    }
}
