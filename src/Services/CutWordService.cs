using JiebaNet.Segmenter.PosSeg;

namespace WordCloud.Server.Services;

internal class CutWordService
{
    private readonly PosSegmenter _segmenter = new();

    readonly string[] blackList = ["ad", "c", "d", "x", "r", "u", "z", "y", "p", "f"];

    public IEnumerable<string> CutWord(string text)
        => _segmenter.Cut(text)
                     .Where(token => !blackList.Contains(token.Flag)
                            && !(token.Flag[0] is 'v' && token.Word.Length is < 2)
                            && !(token.Flag[0] is 'q' && token.Word.Length is < 3))
                     .Select(token => token.Word);
}
