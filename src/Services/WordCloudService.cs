using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;
using WordCloud.Server.Models;

namespace WordCloud.Server.Services;

internal class WordCloudService(
    HttpClient client,
    IMemoryCache cache,
    CutWordService cutWordService,
    ILogger<WordCloudService> logger)
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly MemoryCacheEntryOptions _cacheOptions;

    static WordCloudService()
    {
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(2))
            .RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose());
    }

    private static (int, int) GetProperImageSize(int width, int height)
    {
        if (Math.Max(width, height) > 2048)
        {
            if (width > height)
            {
                height = height * 2048 / width;
                width = 2048;
            }
            else
            {
                width = width * 2048 / height;
                height = 2048;
            }
        }

        return (width, height);
    }

    public async Task<IResult> GenerateWordCloudAsync(string host, WordCloudOptions options)
    {
        logger.LogInformation("Generating word cloud with options: {options}", options);

        var builder = new WordCloudBuilder();

        SKImage? background = null;
        if (options.BackgroundImageUrl is { } imageUrl)
        {
            using var stream = await client.GetStreamAsync(imageUrl);
            background = SKImage.FromEncodedData(stream);
            var (width, height) = GetProperImageSize(background.Width, background.Height);

            builder
                .WithBackgroundImage(background)
                .WithSize(width, height)
                .WithMaxFontSize(Math.Min(width, height))
                .WithBlur(Math.Min(width, height) / 144);
        }

        if (options.FontUrl is { } fontUrl)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (cache.TryGetValue(fontUrl, out SKTypeface? font) && font is not null)
                    builder.WithFont(font);
                else
                {
                    using var stream = await client.GetStreamAsync(fontUrl);
                    font = SKTypeface.FromStream(stream);
                    builder.WithFont(font);

                    cache.Set(fontUrl, font, _cacheOptions);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

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
