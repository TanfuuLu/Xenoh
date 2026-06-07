using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.Infrastructure.Services;

public sealed class PrShareImageService : IPrShareImageService
{
    private const int W = 1200;
    private const int H = 630;

    // Palette
    private static readonly Color BgLeft = Color.ParseHex("07070F");
    private static readonly Color BgRight = Color.ParseHex("0D0D1C");
    private static readonly Color Gold = Color.ParseHex("C4772A");
    private static readonly Color GoldLight = Color.ParseHex("E8973C");
    private static readonly Color White = Color.ParseHex("F2F2F6");
    private static readonly Color Muted = Color.ParseHex("7878A0");
    private static readonly Color Dim = Color.ParseHex("404060");
    private static readonly Color Sep = Color.ParseHex("FFFFFF16");

    public async Task<byte[]> GenerateAsync(PrShareData data, CancellationToken ct = default)
    {
        using var image = new Image<Rgba32>(W, H);
        image.Mutate(ctx =>
        {
            var family = GetFontFamily();

            // ── 1. Full background ────────────────────────────────────────
            ctx.Fill(
                new LinearGradientBrush(
                    new PointF(0, 0), new PointF(W, H),
                    GradientRepetitionMode.None,
                    new ColorStop(0f, BgLeft),
                    new ColorStop(1f, Color.ParseHex("0F0F22"))),
                new RectangularPolygon(0, 0, W, H));

            // ── 2. Diagonal right panel ───────────────────────────────────
            // Panel edge: top at x=700, bottom at x=760 (slight inward diagonal)
            var panel = new Polygon(new LinearLineSegment(
                new PointF(700, 0), new PointF(W, 0),
                new PointF(W, H), new PointF(760, H)));

            ctx.Fill(BgRight, panel);

            // Gold radial glow in right panel
            ctx.Fill(
                new RadialGradientBrush(
                    new PointF(980, H * 0.45f), 340f,
                    GradientRepetitionMode.None,
                    new ColorStop(0f, Gold.WithAlpha(0.13f)),
                    new ColorStop(1f, Gold.WithAlpha(0f))),
                panel);

            // ── 3. "PR" watermark (right panel) ──────────────────────────
            var prFont = family.CreateFont(270, FontStyle.Bold);
            var prMeasure = TextMeasurer.MeasureSize("PR", new TextOptions(prFont));

            // Center "PR" in the right panel (panel midpoint ≈ x=950)
            float prX = 730f + (W - 730f - prMeasure.Width) / 2f;
            float prY = (H - prMeasure.Height) / 2f - 10f;
            ctx.DrawText(
                new RichTextOptions(prFont) { Origin = new PointF(prX, prY) },
                "PR",
                Gold.WithAlpha(0.055f));

            // ── 4. Decorative rings (top-right corner) ───────────────────
            DrawRing(ctx, new PointF(W, 0), 200f, Gold.WithAlpha(0.09f), 1.5f);
            DrawRing(ctx, new PointF(W, 0), 330f, Gold.WithAlpha(0.05f), 1.5f);
            DrawRing(ctx, new PointF(W, 0), 450f, Gold.WithAlpha(0.03f), 1.5f);

            // ── 5. Left accent bar ────────────────────────────────────────
            ctx.Fill(
                new LinearGradientBrush(
                    new PointF(0, 0), new PointF(0, H),
                    GradientRepetitionMode.None,
                    new ColorStop(0f, GoldLight),
                    new ColorStop(1f, Gold)),
                new RectangularPolygon(0, 0, 7, H));

            // ── 6. Content ────────────────────────────────────────────────
            const float lx = 58f;   // left content margin

            // "PERSONAL RECORD" label
            var labelFont = family.CreateFont(14, FontStyle.Bold);
            ctx.DrawText(
                new RichTextOptions(labelFont) { Origin = new PointF(lx, 52f) },
                "PERSONAL RECORD",
                Gold);

            // Separator line
            ctx.Fill(Sep, new RectangularPolygon(lx, 84f, 500f, 1f));

            // Exercise name
            var nameFont = family.CreateFont(70, FontStyle.Bold);
            var nameText = TruncateLine(data.ExerciseName, 20);
            ctx.DrawText(
                new RichTextOptions(nameFont) { Origin = new PointF(lx, 104f) },
                nameText, White);

            // Weight — hero stat in gold
            var weightFont = family.CreateFont(96, FontStyle.Bold);
            var weightStr = $"{data.WeightKg:0.##} kg";
            ctx.DrawText(
                new RichTextOptions(weightFont) { Origin = new PointF(lx, 200f) },
                weightStr, Gold);

            // Reps — directly below
            var repsFont = family.CreateFont(36, FontStyle.Regular);
            var repsStr = $"× {data.Reps} rep{(data.Reps != 1 ? "s" : "")}";
            ctx.DrawText(
                new RichTextOptions(repsFont) { Origin = new PointF(lx + 4f, 318f) },
                repsStr, Muted);

            // Competition lift badge
            float nextY = 376f;
            if (data.IsCompetitionLift)
            {
                const string tag = "COMPETITION LIFT";
                var tagFont = family.CreateFont(13, FontStyle.Bold);
                var tagMeas = TextMeasurer.MeasureSize(tag, new TextOptions(tagFont));
                float bW = tagMeas.Width + 26f, bH = 28f;
                float bX = lx, bY = nextY;
                ctx.Fill(Gold, new RectangularPolygon(bX, bY, bW, bH));
                ctx.DrawText(
                    new RichTextOptions(tagFont)
                    {
                        Origin = new PointF(bX + (bW - tagMeas.Width) / 2f,
                                            bY + (bH - tagMeas.Height) / 2f),
                    },
                    tag, Color.White);
                nextY += 46f;
            }

            // Bottom info separator
            ctx.Fill(Sep, new RectangularPolygon(lx, nextY + 6f, 500f, 1f));

            // User name
            var userFont = family.CreateFont(26, FontStyle.Bold);
            ctx.DrawText(
                new RichTextOptions(userFont) { Origin = new PointF(lx, nextY + 22f) },
                TruncateLine(data.UserName, 28), White);

            // Level · Date
            var metaFont = family.CreateFont(19, FontStyle.Regular);
            ctx.DrawText(
                new RichTextOptions(metaFont) { Origin = new PointF(lx, nextY + 62f) },
                $"Level {data.Level}  ·  {data.AchievedAt:dd MMM yyyy}", Muted);

            // ── 7. Branding ───────────────────────────────────────────────
            var dotFont = family.CreateFont(10, FontStyle.Regular);
            var brandFont = family.CreateFont(19, FontStyle.Bold);
            var dotMeas = TextMeasurer.MeasureSize("● ", new TextOptions(dotFont));
            var brandMeas = TextMeasurer.MeasureSize("xenoh.online", new TextOptions(brandFont));
            float brandX = W - 52f - dotMeas.Width - brandMeas.Width;
            float brandY = H - 46f;
            ctx.DrawText(new RichTextOptions(dotFont) { Origin = new PointF(brandX, brandY + 5f) }, "●", Gold.WithAlpha(0.55f));
            ctx.DrawText(new RichTextOptions(brandFont) { Origin = new PointF(brandX + dotMeas.Width, brandY) }, "xenoh.online", Dim);
        });

        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms, ct);
        return ms.ToArray();
    }

    private static void DrawRing(IImageProcessingContext ctx, PointF center, float radius, Color color, float thickness)
        => ctx.Draw(Pens.Solid(color, thickness), new EllipsePolygon(center.X, center.Y, radius, radius));

    private static FontFamily GetFontFamily()
    {
        if (SystemFonts.TryGet("Segoe UI", out var f)) return f;
        if (SystemFonts.TryGet("Arial", out f)) return f;
        if (SystemFonts.TryGet("DejaVu Sans", out f)) return f;
        if (SystemFonts.TryGet("Liberation Sans", out f)) return f;
        if (SystemFonts.TryGet("Helvetica", out f)) return f;

        string[] paths =
        [
            @"C:\Windows\Fonts\arial.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
        ];
        var fc = new FontCollection();
        foreach (var p in paths)
            if (File.Exists(p)) return fc.Add(p);

        return SystemFonts.Families.First();
    }

    private static string TruncateLine(string text, int max)
        => text.Length <= max ? text : text[..max].TrimEnd() + "…";
}
