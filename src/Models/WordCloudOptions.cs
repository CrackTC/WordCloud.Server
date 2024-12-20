using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WordCloud.Server.Models;

internal record WordCloudOptions(
   [property: JsonPropertyName("width")] int? Width,
   [property: JsonPropertyName("height")] int? Height,
   [property: JsonPropertyName("scale")] float? Scale,
   [property: JsonPropertyName("max_font_size")] int? MaxFontSize,
   [property: JsonPropertyName("min_font_size")] int? MinFontSize,
   [property: JsonPropertyName("font_size_step")] int? FontSizeStep,
   [property: JsonPropertyName("padding")] int? Padding,
   [property: JsonPropertyName("background_color")] string? BackgroundColor,
   [property: JsonPropertyName("background_image_url")] string? BackgroundImageUrl,
   [property: JsonPropertyName("background_image_blur")] int? BackgroundImageBlur,
   [property: JsonPropertyName("background_size_limit")] int? BackgroundSizeLimit,
   [property: JsonPropertyName("similarity")] float? Similarity,
   [property: JsonPropertyName("stroke_width")] int? StrokeWidth,
   [property: JsonPropertyName("stroke_ratio")] float? StrokeRatio,
   [property: JsonPropertyName("verticality")] float? Verticality,
   [property: JsonPropertyName("colors")] string[]? Colors,
   [property: JsonPropertyName("stroke_colors")] string[]? StrokeColors,
   [property: JsonPropertyName("quality")] int? Quality,
   [property: JsonPropertyName("text"), Required] string Text
);
