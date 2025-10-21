using System.Text.RegularExpressions;

static class CorpusLoader
{
    private static readonly Regex WordRe = new Regex("[A-Za-z]+", RegexOptions.Compiled);


    public static Dictionary<string,int> LoadFrequencies(string corpusPath)
    {
        if (!File.Exists(corpusPath))
            throw new FileNotFoundException("corpus.txt not found", corpusPath);


        var freq = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);


        foreach (var line in File.ReadLines(corpusPath))
        {
            foreach (Match m in WordRe.Matches(line))
            {
                var w = m.Value.ToLowerInvariant();
                if (w.Length == 0) continue;
                if (freq.TryGetValue(w, out var c)) freq[w] = c + 1; else freq[w] = 1;
            }
        }
        return freq;
    }
}