using System.Text.Json.Nodes;

namespace WordCloud.Server.Services;

internal class CutWordService(
    IConfiguration configuration,
    HttpClient client)
{
    private readonly string _posUrl = configuration["POS_URL"]!;
    private readonly HttpClient _client = client;

    private readonly string[] _blackList = [
        "AD",
        "AS",
        "BA",
        "CC",
        "CD",
        "CS",
        "DEC",
        "DEG",
        "DER",
        "DEV",
        "DT",
        "ETC",
        "IJ",
        "LB",
        "LC",
        "M",
        "MSP",
        "NOI",
        "P",
        "PN",
        "PU",
        "SB",
        "SP",
        "VC",
        "VE",
    ];

    private async Task<JsonArray> Cut(string text)
    {
        using var formContent = new FormUrlEncodedContent([new("text", text)]);
        var response = await _client.PostAsync(_posUrl, formContent);
        return JsonNode.Parse(await response.Content.ReadAsStreamAsync())?.AsArray()
            ?? throw new InvalidOperationException("Response array is null");
    }

    public async Task<IEnumerable<string>> CutWordAsync(string text)
        => (await Cut(text))
             .OfType<JsonArray>()
             .Select(pair => (Word: pair[0]!.GetValue<string>(), Flag: pair[1]!.GetValue<string>()))
             .Where(pair => !_blackList.Contains(pair.Flag)
                     && !(pair.Flag[0] is 'V' && pair.Word.Length < 2)
                     && !(pair.Flag is "M" && pair.Word.Length < 3))
             .Select(pair => pair.Word);
}
