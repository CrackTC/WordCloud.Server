using JiebaNet.Segmenter.PosSeg;

namespace WordCloud.Server.Services;

internal class CutWordService
{
    private readonly PosSegmenter _segmenter = new();

    private readonly HashSet<string> _blackList = [
        "ad",
        "c",
        "d",
        "e",
        "f",
        "k",
        "p",
        "r",
        "u",
        "ud",
        "ug",
        "uj",
        "ul",
        "uv",
        "uz",
        "x",
        "y",
        "yg",
        "z",
        "zg"
    ];

    public IEnumerable<string> CutWord(string text)
        => _segmenter.Cut(text)
            .Where(token => !_blackList.Contains(token.Flag)
                && !(token.Flag[0] is 'v' && token.Word.Length < 2)
                && !(token.Flag[0] is 'a' && token.Word.Length < 2)
                && !(token.Flag[0] is 'm' && token.Word.Length < 2)
                && !(token.Flag[0] is 'q' && token.Word.Length < 3))
            .Select(token => token.Word);
}
