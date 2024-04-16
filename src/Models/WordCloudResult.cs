using System.Text.Json.Serialization;

[Serializable]
internal record WordCloudResult(
    [property: JsonPropertyName("url")] string Url
);
