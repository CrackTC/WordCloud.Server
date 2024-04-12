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

    public IResult GenerateWordCloud(WordCloudOptions options)
    {
        logger.LogInformation("Generating word cloud with options: {options}", options);

        var builder = new WordCloudBuilder();
        if (options.MaxFontSize is { } maxFontsize)
            builder.WithMaxFontSize(maxFontsize);
        if (options.MinFontSize is { } minFontSize)
            builder.WithMinFontSize(minFontSize);

        if (options.FontUrl is { } fontUrl)
        {
            _semaphore.Wait();
            try
            {
                if (cache.TryGetValue(fontUrl, out SKTypeface? font) && font is not null)
                    builder.WithFont(font);
                else
                {
                    using var stream = client.GetStreamAsync(fontUrl).Result;
                    font = SKTypeface.FromStream(stream);
                    builder.WithFont(font);

                    cache.Set(fontUrl, font, new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(2))
                        .RegisterPostEvictionCallback((_, value, _, _) => (value as IDisposable)?.Dispose()));
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        if (options.FontSizeStep is { } step)
            builder.WithFontSizeStep(step);
        if (options.Padding is { } padding)
            builder.WithPadding(padding);
        if (options.BackgroundColor is { } backgroundColor)
            builder.WithBackground(SKColor.Parse(backgroundColor));

        SKImage? background = null;
        if (options.BackgroundImageUrl is { } imageUrl)
        {
            using var stream = client.GetStreamAsync(imageUrl).Result;
            background = SKImage.FromEncodedData(stream);
            builder.WithBackgroundImage(background);
            builder.WithSize(background.Width, background.Height);
        }

        if (options.BackgroundImageBlur is { } blur)
            builder.WithBlur(blur);
        if (options.Similarity is { } similarity)
            builder.WithSimilarity(similarity);
        if (options.StrokeWidth is { } strokeWidth)
            builder.WithStrokeWidth(strokeWidth);
        if (options.Verticality is { } verticality)
            builder.WithVerticality(verticality);
        if (options.Colors is { } colors)
            builder.WithColorFunc(_ => SKColor.Parse(colors[Random.Shared.Next(colors.Length)]));
        if (options.StrokeColors is { } strokeColors)
            builder.WithStrokeColorFunc(_ => SKColor.Parse(strokeColors[Random.Shared.Next(strokeColors.Length)]));

        using var cloud = builder.Build();
        var dict = cutWordService.CutWord(options.Text)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        using var image = cloud.GenerateImage(dict);
        var data = image.Encode(SKEncodedImageFormat.Webp, options.Quality ?? 100);

        background?.Dispose();

        return Results.File(data.AsStream(), "image/webp");
    }
}
