using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using WordCloud.Server.Models;
using WordCloud.Server.Services;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSingleton<CutWordService>()
    .AddSingleton<WordCloudService>()
    .AddMemoryCache()
    .ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default))
    .AddHttpClient<WordCloudService>();

builder.Logging.AddConsole();

var app = builder.Build();

app.MapPost(
    "/cut",
    (CutWordService service, [FromForm] string text) => service.CutWord(text)
).DisableAntiforgery();

app.MapPost(
    "/wordcloud",
    async (
      WordCloudService service,
      HttpRequest request,
      [FromBody] WordCloudOptions options
    ) => await service.GenerateWordCloudAsync($"{request.Scheme}://{request.Host}", options)
).DisableAntiforgery();

app.MapGet(
    "/result/{id}",
    (WordCloudService service, string id) => service.GetResult(id)
);

app.Run($"http://*:{app.Configuration.GetValue<int>("PORT")}/");

[JsonSerializable(typeof(WordCloudOptions))]
[JsonSerializable(typeof(WordCloudResult))]
[JsonSerializable(typeof(IEnumerable<string>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
