using Microsoft.AspNetCore.Hosting;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Share.Queries.GetPrShareData;

namespace Xenoh.Infrastructure.Services;

public sealed class PrShareImageService(IWebHostEnvironment environment, IHttpClientFactory httpClientFactory)
    : IPrShareImageService
{
    private const int W = 1456;
    private const int H = 1088;

    private static readonly Color Bg = Color.ParseHex("070711");
    private static readonly Color Panel = Color.ParseHex("11101B");
    private static readonly Color Gold = Color.ParseHex("F28C1C");
    private static readonly Color GoldDeep = Color.ParseHex("B65312");
    private static readonly Color White = Color.ParseHex("F5F5FA");
    private static readonly Color Muted = Color.ParseHex("8C89B4");
    private static readonly Color Border = Color.ParseHex("FFFFFF22");

    public async Task<byte[]> GenerateAsync(PrShareData data, CancellationToken ct = default)
    {
        using var avatar = await TryLoadAvatarAsync(data.AvatarUrl, ct);
        using var image = new Image<Rgba32>(W, H);

        image.Mutate(ctx =>
        {
            var family = GetFontFamily();

            DrawBackground(ctx);
            DrawTopBrand(ctx, family);
            DrawTopAvatarHint(ctx, avatar);
            DrawMainContent(ctx, family, data);
            DrawAthleteStrip(ctx, family, data, avatar);
            DrawFooterBrand(ctx, family);
        });

        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms, ct);
        return ms.ToArray();
    }

    private static void DrawBackground(IImageProcessingContext ctx)
    {
        ctx.Fill(
            new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(W, H),
                GradientRepetitionMode.None,
                new ColorStop(0f, Color.ParseHex("05050C")),
                new ColorStop(0.55f, Bg),
                new ColorStop(1f, Color.ParseHex("11101A"))),
            new RectangularPolygon(0, 0, W, H));

        ctx.Fill(
            new RadialGradientBrush(
                new PointF(1030, 705),
                430,
                GradientRepetitionMode.None,
                new ColorStop(0f, Gold.WithAlpha(0.2f)),
                new ColorStop(0.5f, GoldDeep.WithAlpha(0.08f)),
                new ColorStop(1f, Gold.WithAlpha(0f))),
            new RectangularPolygon(0, 0, W, H));

        ctx.Fill(Color.ParseHex("FFFFFF14"), new RectangularPolygon(0, 100, W, 1));
        DrawRing(ctx, new PointF(W - 24, 54), 210, Gold.WithAlpha(0.22f), 1.2f);
        DrawRing(ctx, new PointF(W - 24, 54), 286, Gold.WithAlpha(0.15f), 1.1f);
        DrawRing(ctx, new PointF(W - 24, 54), 372, Gold.WithAlpha(0.09f), 1.0f);
        DrawRing(ctx, new PointF(-74, 704), 232, Gold.WithAlpha(0.07f), 1.0f);

        ctx.Fill(Gold.WithAlpha(0.85f), new EllipsePolygon(60, 695, 2.4f));
        ctx.Fill(Gold.WithAlpha(0.65f), new EllipsePolygon(674, 367, 2.2f));
        ctx.Fill(Gold.WithAlpha(0.75f), new EllipsePolygon(1365, 463, 2.6f));
        ctx.Fill(Gold.WithAlpha(0.5f), new EllipsePolygon(60, 222, 2.0f));

        var beam = new LinearGradientBrush(
            new PointF(656, 697),
            new PointF(1228, 697),
            GradientRepetitionMode.None,
            new ColorStop(0f, Gold.WithAlpha(0f)),
            new ColorStop(0.55f, Gold.WithAlpha(0.72f)),
            new ColorStop(1f, Gold.WithAlpha(0f)));
        ctx.Fill(beam, new RectangularPolygon(650, 692, 590, 7));
    }

    private static void DrawTopBrand(IImageProcessingContext ctx, FontFamily family)
    {
        var logoFont = family.CreateFont(28, FontStyle.Bold);
        ctx.DrawText(new RichTextOptions(logoFont) { Origin = new PointF(72, 42) }, "X", Gold);
        ctx.DrawText(new RichTextOptions(logoFont) { Origin = new PointF(104, 42) }, "E N O H", White);
    }

    private static void DrawTopAvatarHint(IImageProcessingContext ctx, Image<Rgba32>? avatar)
    {
        const float x = 1366;
        const float y = 55;
        ctx.Fill(Color.ParseHex("0D0D18").WithAlpha(0.8f), new EllipsePolygon(x, y, 23));
        ctx.Draw(Pens.Solid(Border, 1.2f), new EllipsePolygon(x, y, 23));
        if (avatar is not null)
        {
            using var small = BuildCircularAvatar(avatar, 34);
            ctx.DrawImage(small, new Point((int)x - 17, (int)y - 17), 1f);
        }
        else
        {
            ctx.Draw(Pens.Solid(White.WithAlpha(0.78f), 1.7f), new EllipsePolygon(x, y - 5, 5));
            ctx.Draw(Pens.Solid(White.WithAlpha(0.78f), 1.7f), new EllipsePolygon(x, y + 10, 10, 8));
        }

        ctx.Fill(Gold, new EllipsePolygon(x + 17, y - 17, 3.4f));
    }

    private static void DrawMainContent(IImageProcessingContext ctx, FontFamily family, PrShareData data)
    {
        const float lx = 110;

        var labelFont = family.CreateFont(18, FontStyle.Bold);
        ctx.DrawText(new RichTextOptions(labelFont) { Origin = new PointF(lx, 185) }, "PERSONAL RECORD", Gold);
        ctx.DrawText(new RichTextOptions(labelFont) { Origin = new PointF(lx + 145, 184) }, "TROPHY", Color.ParseHex("F7C77D"));

        var nameFont = family.CreateFont(91, FontStyle.Bold);
        ctx.DrawText(new RichTextOptions(nameFont) { Origin = new PointF(lx, 226) }, TruncateLine(data.ExerciseName, 18), White);

        var badgeFont = family.CreateFont(28, FontStyle.Bold);
        var badge = "PR LIFTED";
        var badgeBounds = TextMeasurer.MeasureSize(badge, new TextOptions(badgeFont));
        var badgeBox = new RectangularPolygon(lx, 333, badgeBounds.Width + 78, 56);
        ctx.Draw(Pens.Solid(Gold.WithAlpha(0.85f), 1.2f), badgeBox);
        ctx.DrawText(new RichTextOptions(family.CreateFont(30, FontStyle.Bold)) { Origin = new PointF(lx + 26, 346) }, "^", Gold);
        ctx.DrawText(new RichTextOptions(badgeFont) { Origin = new PointF(lx + 67, 349) }, badge, Gold);

        var statFont = family.CreateFont(205, FontStyle.Bold);
        var weight = $"{data.WeightKg:0.##}";
        ctx.DrawText(new RichTextOptions(statFont) { Origin = new PointF(lx, 413) }, weight, Gold);
        ctx.DrawText(new RichTextOptions(family.CreateFont(62, FontStyle.Bold)) { Origin = new PointF(lx + 414, 553) }, "kg", Gold);

        var reps = $"x {data.Reps} rep{(data.Reps != 1 ? "s" : "")}";
        ctx.DrawText(new RichTextOptions(family.CreateFont(40, FontStyle.Regular)) { Origin = new PointF(lx + 4, 632) }, reps, Muted);
        ctx.DrawText(new RichTextOptions(family.CreateFont(27, FontStyle.Regular)) { Origin = new PointF(lx + 4, 694) }, "New personal best", White);

        var prFont = family.CreateFont(390, FontStyle.Bold);
        ctx.DrawText(new RichTextOptions(prFont) { Origin = new PointF(840, 365) }, "PR", Gold.WithAlpha(0.12f));
        ctx.DrawText(new RichTextOptions(prFont) { Origin = new PointF(840, 365) }, "PR", Color.ParseHex("2B1A13").WithAlpha(0.3f));
    }

    private static void DrawAthleteStrip(
        IImageProcessingContext ctx,
        FontFamily family,
        PrShareData data,
        Image<Rgba32>? avatar)
    {
        const float x = 110;
        const float y = 762;
        const float w = 1236;
        const float h = 134;

        ctx.Fill(Panel.WithAlpha(0.72f), new RectangularPolygon(x, y, w, h));
        ctx.Draw(Pens.Solid(Border, 1.1f), new RectangularPolygon(x, y, w, h));

        ctx.Fill(
            new RadialGradientBrush(
                new PointF(x + 86, y + 67),
                92,
                GradientRepetitionMode.None,
                new ColorStop(0f, Gold.WithAlpha(0.32f)),
                new ColorStop(1f, Gold.WithAlpha(0f))),
            new RectangularPolygon(x, y, 190, h));

        if (avatar is not null)
        {
            using var avatarImage = BuildCircularAvatar(avatar, 92);
            ctx.DrawImage(avatarImage, new Point((int)x + 40, (int)y + 21), 1f);
        }
        else
        {
            DrawFallbackAvatar(ctx, new PointF(x + 86, y + 67), 46, family);
        }

        ctx.Draw(Pens.Solid(Gold.WithAlpha(0.95f), 1.4f), new EllipsePolygon(x + 86, y + 67, 46));

        ctx.DrawText(
            new RichTextOptions(family.CreateFont(29, FontStyle.Bold)) { Origin = new PointF(x + 172, y + 36) },
            TruncateLine(data.UserName, 28),
            White);
        ctx.DrawText(
            new RichTextOptions(family.CreateFont(22, FontStyle.Regular)) { Origin = new PointF(x + 172, y + 78) },
            $"Level {data.Level}   |   {data.AchievedAt:dd MMM yyyy}",
            Muted);

        ctx.Fill(Color.ParseHex("FFFFFF0B"), new EllipsePolygon(x + w - 66, y + 67, 29));
        ctx.Draw(Pens.Solid(Border, 1.1f), new EllipsePolygon(x + w - 66, y + 67, 29));
        ctx.DrawText(
            new RichTextOptions(family.CreateFont(35, FontStyle.Regular)) { Origin = new PointF(x + w - 79, y + 40) },
            "->",
            Gold);
    }

    private static void DrawFooterBrand(IImageProcessingContext ctx, FontFamily family)
    {
        var brandFont = family.CreateFont(20, FontStyle.Bold);
        var tagFont = family.CreateFont(13, FontStyle.Regular);
        var brand = "X E N O H";
        var tag = "S T R O N G E R   E V E R Y   D A Y .";
        var brandSize = TextMeasurer.MeasureSize(brand, new TextOptions(brandFont));
        var tagSize = TextMeasurer.MeasureSize(tag, new TextOptions(tagFont));
        ctx.DrawText(new RichTextOptions(brandFont) { Origin = new PointF((W - brandSize.Width) / 2, 948) }, brand, White);
        ctx.DrawText(new RichTextOptions(tagFont) { Origin = new PointF((W - tagSize.Width) / 2, 985) }, tag, Muted);
        ctx.DrawText(new RichTextOptions(family.CreateFont(22, FontStyle.Regular)) { Origin = new PointF(W / 2f - 7, 1028) }, "v", Gold);
    }

    private async Task<Image<Rgba32>?> TryLoadAvatarAsync(string? avatarUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            return null;

        try
        {
            if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme is not ("http" or "https"))
                    return null;

                using var response = await httpClientFactory.CreateClient().GetAsync(uri, ct);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                return await Image.LoadAsync<Rgba32>(stream, ct);
            }

            if (string.IsNullOrWhiteSpace(environment.WebRootPath))
                return null;

            var relativePath = avatarUrl.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar);
            var path = System.IO.Path.Combine(environment.WebRootPath, relativePath);
            if (!File.Exists(path))
                return null;

            return await Image.LoadAsync<Rgba32>(path, ct);
        }
        catch
        {
            return null;
        }
    }

    private static Image<Rgba32> BuildCircularAvatar(Image<Rgba32> source, int size)
    {
        var avatar = source.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop
        }));

        var radius = size / 2f;
        var center = radius - 0.5f;
        avatar.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    if ((dx * dx) + (dy * dy) > radius * radius)
                        row[x].A = 0;
                }
            }
        });

        return avatar;
    }

    private static void DrawFallbackAvatar(IImageProcessingContext ctx, PointF center, float radius, FontFamily family)
    {
        ctx.Fill(Color.ParseHex("1A1420"), new EllipsePolygon(center.X, center.Y, radius));
        ctx.DrawText(
            new RichTextOptions(family.CreateFont(36, FontStyle.Bold))
            {
                Origin = new PointF(center.X - 26, center.Y - 28)
            },
            "|||",
            Gold);
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
        => text.Length <= max ? text : text[..max].TrimEnd() + "...";
}
