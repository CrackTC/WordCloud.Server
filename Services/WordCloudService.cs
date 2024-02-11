using SkiaSharp;
using WordCloud.Server.Models;

namespace WordCloud.Server.Services;

internal class WordCloudService(CutWordService cutWordService, ILogger<WordCloudService> logger)
{
    public IResult GenerateWordCloud(WordCloudOptions options)
    {
        logger.LogInformation("Generating word cloud with options: {options}", options);

        var builder = new WordCloudBuilder();
        if (options.Width is { } width && options.Height is { } height)
            builder.WithSize(width, height);
        if (options.MaxFontSize is { } maxFontsize)
            builder.WithMaxFontSize(maxFontsize);
        if (options.MinFontSize is { } minFontSize)
            builder.WithMinFontSize(minFontSize);
        if (options.FontPath is { } fontPath)
            builder.WithFontFile(fontPath);
        if (options.FontSizeStep is { } step)
            builder.WithFontSizeStep(step);
        if (options.Padding is { } padding)
            builder.WithPadding(padding);
        if (options.BackgroundColor is { } backgroundColor)
            builder.WithBackground(SKColor.Parse(backgroundColor));
        if (options.BackgroundImage is { } backgroundImage)
        {
            var bytes = Convert.FromBase64String(backgroundImage);
            builder.WithBackgroundImage(SKImage.FromEncodedData(bytes));
        }
        if (options.BackgroundImageBlur is { } blur)
            builder.WithBlur(blur);
        if (options.Similarity is { } similarity)
            builder.WithSimilarity(similarity);
        if (options.StrokeWidth is { } strokeWidth)
            builder.WithStrokeWidth(strokeWidth);
        if (options.Colors is { } colors)
            builder.WithColorFunc(_ => SKColor.Parse(colors[Random.Shared.Next(colors.Length)]));
        if (options.StrokeColors is { } strokeColors)
            builder.WithStrokeColorFunc(_ => SKColor.Parse(strokeColors[Random.Shared.Next(strokeColors.Length)]));

        using var cloud = builder.Build();
        var dict = cutWordService.CutWord(options.Text)
                                 .GroupBy(x => x)
                                 .ToDictionary(x => x.Key, x => x.Count());

        using var image = cloud.GenerateImage(dict);
        using var data = image.Encode(SKEncodedImageFormat.Webp, 80);
        return Results.File(data.AsStream(), "image/webp");
    }
}
