using System;
using System.Collections.Generic;
using System.Linq;

public class SpellingOptions
{
    public bool UseFirstLetterLockExtension { get; init; } = false;
    // Komşu harf yer değişimi (transpose) adayları varsa onlara öncelik ver
    public bool UseTransposePriorityExtension { get; init; } = false;
}

public class SpellingCorrector
{
    private readonly Dictionary<string, int> _freq;
    private readonly SpellingOptions _options;
    private static readonly string Alphabet = "abcdefghijklmnopqrstuvwxyz";

    public SpellingCorrector(Dictionary<string, int> frequency, SpellingOptions options)
    {
        _freq = frequency ?? throw new ArgumentNullException(nameof(frequency));
        _options = options ?? new SpellingOptions();
    }

    public string Predict(string rawWord)
    {
        if (string.IsNullOrWhiteSpace(rawWord)) return string.Empty;

        var word = rawWord.ToLowerInvariant();

        // DİKKAT: Erken dönüş YOK (yanlış kelime korpusta geçse bile direkt dönmüyoruz).

        // DL=1 adayları üret
        var candidates = GenerateEdits1(word);

        // Sözlükle kesiştir
        var valid = candidates.Where(c => _freq.ContainsKey(c)).ToList();

        // TRANSPOSE-FIRST (opsiyonel): sözlükte transpozisyon adayları varsa yalnızca onlardan seç
        if (_options.UseTransposePriorityExtension && valid.Count > 0)
        {
            var transOnly  = GenerateTransposesOnly(word);
            var transValid = transOnly.Where(c => _freq.ContainsKey(c)).ToList();
            if (transValid.Count > 0)
                valid = transValid;
        }

        // FIRST-LETTER (opsiyonel): ilk harfi eşleşenler varsa havuzu onlarla sınırla
        if (_options.UseFirstLetterLockExtension && word.Length > 0 && valid.Count > 0)
        {
            var locked = valid.Where(v => v.Length > 0 && v[0] == word[0]).ToList();
            if (locked.Count > 0) valid = locked;
        }

        // Aday yoksa spec gereği boş satır
        if (valid.Count == 0)
            return string.Empty;

        // Seçim: frekans ↓, eşitlikte leksikografik (deterministik)
        return valid
            .OrderByDescending(v => _freq[v])
            .ThenBy(v => v, StringComparer.Ordinal)
            .First();
    }

    // Sadece bitişik transpozisyon adaylarını üretir
    public static IEnumerable<string> GenerateTransposesOnly(string word)
    {
        var results = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i + 1 < word.Length; i++)
        {
            var arr = word.ToCharArray();
            (arr[i], arr[i + 1]) = (arr[i + 1], arr[i]);
            results.Add(new string(arr));
        }
        return results;
    }

    public static HashSet<string> GenerateEdits1(string word)
    {
        var results = new HashSet<string>(StringComparer.Ordinal);

        var splits = Enumerable.Range(0, word.Length + 1)
                               .Select(i => (L: word[..i], R: word[i..]))
                               .ToArray();

        // Deletions
        foreach (var (L, R) in splits)
            if (R.Length > 0)
                results.Add(L + R[1..]);

        // Transposes (bitişik harfleri değiştir)
        foreach (var (L, R) in splits)
            if (R.Length > 1)
                results.Add(L + R[1] + R[0] + R[2..]);

        // Replacements (ikame)
        foreach (var (L, R) in splits)
            if (R.Length > 0)
                foreach (var c in Alphabet)
                    if (c != R[0])
                        results.Add(L + c + R[1..]);

        // Insertions (ekleme)
        foreach (var (L, R) in splits)
            foreach (var c in Alphabet)
                results.Add(L + c + R);

        return results;
    }
}
