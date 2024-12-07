using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;
using WordCloud.Server.Models;

namespace WordCloud.Server.Services;

internal class WordCloudService(
    IConfiguration configuration,
    HttpClient client,
    IMemoryCache cache,
    CutWordService cutWordService,
    ILogger<WordCloudService> logger
)
{
    private static readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(2))
            .RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose());
    private readonly SKTypeface _typeface = SKTypeface.FromFile(configuration.GetValue<string>("FONT_PATH"));
    private readonly SKTypeface _emojiTypeface = SKTypeface.FromFile(configuration.GetValue<string>("EMOJI_FONT_PATH"));

    private static (int, int) FitBoxSize(int limit, int width, int height)
    {
        if (Math.Max(width, height) > limit)
        {
            if (width > height)
            {
                height = height * limit / width;
                width = limit;
            }
            else
            {
                width = width * limit / height;
                height = limit;
            }
        }

        return (width, height);
    }

    public async Task<IResult> GenerateWordCloudAsync(string host, WordCloudOptions options)
    {
        logger.LogInformation("Generating word cloud with options: {options}", options);

        var builder = new WordCloudBuilder()
            .WithFont(_typeface)
            .WithEmojiFont(_emojiTypeface);

        SKImage? background = null;
        if (options.BackgroundImageUrl is { } imageUrl)
        {
            using var stream = await client.GetStreamAsync(imageUrl);
            background = SKImage.FromEncodedData(stream);
            var (width, height) = FitBoxSize(options.BackgroundSizeLimit ?? 1280, background.Width, background.Height);

            builder
                .WithBackgroundImage(background)
                .WithSize(width, height)
                .WithMaxFontSize(Math.Min(width, height))
                .WithBlur(Math.Min(width, height) / 144);
        }
        else
        {
            var (w, h) = (options.Width ?? 1280, options.Height ?? 720);
            builder.WithSize(w, h);
        }

        if (options.Scale is { } scale)
            builder.WithScale(scale);
        if (options.MaxFontSize is { } maxFontsize)
            builder.WithMaxFontSize(maxFontsize);
        if (options.MinFontSize is { } minFontSize)
            builder.WithMinFontSize(minFontSize);
        if (options.FontSizeStep is { } step)
            builder.WithFontSizeStep(step);
        if (options.Padding is { } padding)
            builder.WithPadding(padding);
        if (options.BackgroundColor is { } backgroundColor)
            builder.WithBackground(SKColor.Parse(backgroundColor));

        if (options.BackgroundImageBlur is { } blur)
            builder.WithBlur(blur);
        if (options.Similarity is { } similarity)
            builder.WithSimilarity(similarity);
        if (options.StrokeWidth is { } strokeWidth)
            builder.WithStrokeWidth(strokeWidth);
        if (options.StrokeRatio is { } strokeRatio)
            builder.WithStrokeRatio(strokeRatio);
        if (options.Verticality is { } verticality)
            builder.WithVerticality(verticality);
        if (options.Colors is { } colors)
            builder.WithColorFunc(_ => SKColor.Parse(colors[Random.Shared.Next(colors.Length)]));
        if (options.StrokeColors is { } strokeColors)
            builder.WithStrokeColorFunc(_ => SKColor.Parse(strokeColors[Random.Shared.Next(strokeColors.Length)]));

        using var cloud = builder.Build();
        var dict = (await cutWordService.CutWordAsync(options.Text))
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        var image = cloud.GenerateImage(dict);
        var id = Guid.NewGuid().ToString();
        cache.Set(id, image, _cacheOptions);
        cache.Set($"{id}-quality", options.Quality ?? 100, _cacheOptions);
        var url = host + "/result/" + id;

        background?.Dispose();

        return Results.Json(new WordCloudResult(url));
    }

    public IResult GetResult(string id)
    {
        if (cache.TryGetValue<SKImage>(id, out var image) && image is not null)
        {
            var quality = cache.Get<int>($"{id}-quality");
            var data = image.Encode(SKEncodedImageFormat.Webp, quality);
            return Results.File(data.AsStream(), "image/webp");
        }

        return Results.NotFound();
    }
}
