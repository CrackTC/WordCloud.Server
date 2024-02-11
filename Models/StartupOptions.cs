using System.Text.Json.Serialization;

namespace WordCloud.Server.Models;

internal record StartupOptions(
   [property: JsonPropertyName("port")] int Port
);
