using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var command = args[0].ToLowerInvariant();
        var opts = ArgParser.Parse(args.Skip(1).ToArray());

        try
        {
            switch (command)
            {
                case "predict":
                    RunPredict(opts);
                    break;
                case "eval":
                    RunEval(opts);
                    break;
                case "compare":
                    RunCompareAll(opts); // <-- üçlü karşılaştırma
                    break;
                default:
                    Console.Error.WriteLine($"Unknown command: {command}\n");
                    PrintUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
        }
    }

    // -------------------- PREDICT --------------------
    static void RunPredict(Dictionary<string, string> opts)
    {
        var corpusPath = Require(opts, "--corpus");
        var inputPath  = Require(opts, "--input");
        var outputPath = Require(opts, "--output");

        var ext = opts.ContainsKey("--ext") ? opts["--ext"].ToLowerInvariant() : null;

        Console.WriteLine("Loading corpus and building frequency dictionary…");
        var freq = CorpusLoader.LoadFrequencies(corpusPath);
        var sc = new SpellingCorrector(freq,
            new SpellingOptions {
                UseFirstLetterLockExtension   = ext == "first-letter",
                UseTransposePriorityExtension = ext == "tp" || ext == "transpose-first"
            });

        Console.WriteLine("Reading misspelled words and predicting…");
        var lines = File.ReadAllLines(inputPath);
        var preds = new List<string>(capacity: lines.Length);

        foreach (var raw in lines)
        {
            var word = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(word))
            {
                preds.Add(string.Empty);
                continue;
            }
            var pred = sc.Predict(word);
            preds.Add(pred);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        File.WriteAllLines(outputPath, preds, Encoding.UTF8);
        Console.WriteLine($"Done. Wrote predictions to {outputPath}");
    }

    // -------------------- EVAL --------------------
    static void RunEval(Dictionary<string, string> opts)
    {
        var corpusPath = Require(opts, "--corpus");
        var misspelled = Require(opts, "--misspelled");
        var gold       = Require(opts, "--gold");
        var outPath    = opts.ContainsKey("--out") ? opts["--out"] : null;

        var ext = opts.ContainsKey("--ext") ? opts["--ext"].ToLowerInvariant() : null;

        Console.WriteLine("Loading corpus and building frequency dictionary…");
        var freq = CorpusLoader.LoadFrequencies(corpusPath);
        var sc = new SpellingCorrector(freq,
            new SpellingOptions {
                UseFirstLetterLockExtension   = ext == "first-letter",
                UseTransposePriorityExtension = ext == "tp" || ext == "transpose-first"
            });

        var mis = File.ReadAllLines(misspelled).Select(s => s.Trim()).ToArray();
        var ans = File.ReadAllLines(gold).Select(s => s.Trim()).ToArray();
        if (mis.Length != ans.Length)
            throw new InvalidOperationException("Misspelled and gold files must have the same number of lines.");

        List<string>? predsForDump = outPath != null ? new List<string>(mis.Length) : null;

        int correct = 0;
        for (int i = 0; i < mis.Length; i++)
        {
            var pred = sc.Predict(mis[i]);
            if (predsForDump != null) predsForDump.Add(pred);
            if (string.Equals(pred, ans[i], StringComparison.OrdinalIgnoreCase))
                correct++;
        }

        double acc = mis.Length == 0 ? 0 : (double)correct / mis.Length;
        Console.WriteLine($"Accuracy = {correct} / {mis.Length} = {acc:F4}");

        if (predsForDump != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath!) ?? ".");
            File.WriteAllLines(outPath!, predsForDump, Encoding.UTF8);
            Console.WriteLine($"Wrote eval predictions to {outPath}");
        }
    }

    // -------------------- COMPARE (üçlü) --------------------
    // Baseline vs First-Letter vs Transpose-First
    // Opsiyonel: --out-base <f> --out-first <f> --out-tp <f>
    static void RunCompareAll(Dictionary<string, string> opts)
    {
        var corpusPath = Require(opts, "--corpus");
        var misspelled = Require(opts, "--misspelled");
        var gold       = Require(opts, "--gold");

        Console.WriteLine("Loading corpus and building frequency dictionary…");
        var freq = CorpusLoader.LoadFrequencies(corpusPath);

        var scBase  = new SpellingCorrector(freq, new SpellingOptions { });
        var scFirst = new SpellingCorrector(freq, new SpellingOptions { UseFirstLetterLockExtension = true });
        var scTp    = new SpellingCorrector(freq, new SpellingOptions { UseTransposePriorityExtension = true });

        var mis = File.ReadAllLines(misspelled).Select(s => s.Trim()).ToArray();
        var ans = File.ReadAllLines(gold).Select(s => s.Trim()).ToArray();
        if (mis.Length != ans.Length)
            throw new InvalidOperationException("Misspelled and gold files must have the same number of lines.");

        var outBase  = opts.ContainsKey("--out-base")  ? opts["--out-base"]  : null;
        var outFirst = opts.ContainsKey("--out-first") ? opts["--out-first"] : null;
        var outTp    = opts.ContainsKey("--out-tp")    ? opts["--out-tp"]    : null;

        List<string>? predsBase  = outBase  != null ? new List<string>(mis.Length) : null;
        List<string>? predsFirst = outFirst != null ? new List<string>(mis.Length) : null;
        List<string>? predsTp    = outTp    != null ? new List<string>(mis.Length) : null;

        int correctBase = 0, correctFirst = 0, correctTp = 0;

        for (int i = 0; i < mis.Length; i++)
        {
            var pBase  = scBase.Predict(mis[i]);
            var pFirst = scFirst.Predict(mis[i]);
            var pTp    = scTp.Predict(mis[i]);

            if (predsBase  != null) predsBase.Add(pBase);
            if (predsFirst != null) predsFirst.Add(pFirst);
            if (predsTp    != null) predsTp.Add(pTp);

            if (string.Equals(pBase,  ans[i], StringComparison.OrdinalIgnoreCase)) correctBase++;
            if (string.Equals(pFirst, ans[i], StringComparison.OrdinalIgnoreCase)) correctFirst++;
            if (string.Equals(pTp,    ans[i], StringComparison.OrdinalIgnoreCase)) correctTp++;
        }

        double accBase  = mis.Length == 0 ? 0 : (double)correctBase  / mis.Length;
        double accFirst = mis.Length == 0 ? 0 : (double)correctFirst / mis.Length;
        double accTp    = mis.Length == 0 ? 0 : (double)correctTp    / mis.Length;

        Console.WriteLine();
        Console.WriteLine("Version                    Correct / Total    Accuracy");
        Console.WriteLine("------------------------   ----------------    --------");
        Console.WriteLine($"Baseline                   {correctBase,3} / {mis.Length,-3}      {accBase:F4}");
        Console.WriteLine($"Extension (First-Letter)   {correctFirst,3} / {mis.Length,-3}      {accFirst:F4}");
        Console.WriteLine($"Extension (Transpose-First){correctTp,3} / {mis.Length,-3}      {accTp:F4}");

        if (predsBase != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outBase!) ?? ".");
            File.WriteAllLines(outBase!, predsBase, Encoding.UTF8);
            Console.WriteLine($"Baseline predictions  → {outBase}");
        }
        if (predsFirst != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outFirst!) ?? ".");
            File.WriteAllLines(outFirst!, predsFirst, Encoding.UTF8);
            Console.WriteLine($"First-Letter predictions → {outFirst}");
        }
        if (predsTp != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outTp!) ?? ".");
            File.WriteAllLines(outTp!, predsTp, Encoding.UTF8);
            Console.WriteLine($"Transpose-First predictions → {outTp}");
        }
    }

    // -------------------- HELPERS --------------------
    static string Require(Dictionary<string,string> opts, string key)
    {
        if (!opts.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
            throw new ArgumentException($"Missing required option: {key}");
        return val;
    }

    static void PrintUsage()
    {
        Console.WriteLine(@"IR HW1 – Spelling Corrector (C#)
Usage:
  dotnet run -- predict --corpus <path> --input <path> --output <path> [--ext first-letter|tp]
  dotnet run -- eval    --corpus <path> --misspelled <path> --gold <path> [--ext first-letter|tp] [--out <file>]
  dotnet run -- compare --corpus <path> --misspelled <path> --gold <path> [--out-base <file>] [--out-first <file>] [--out-tp <file>]

Options:
  --corpus        Path to corpus.txt
  --input         Path to file with misspelled words (one per line). For predict.
  --output        Output path to write predictions (one per line). For predict.
  --misspelled    Path to test-words-misspelled.txt. For eval/compare.
  --gold          Path to test-words-correct.txt. For eval/compare.
  --out           (eval)     Optional file path to dump predictions.
  --out-base      (compare)  Optional file path for baseline predictions.
  --out-first     (compare)  Optional file path for first-letter predictions.
  --out-tp        (compare)  Optional file path for transpose-first predictions.
  --ext first-letter|tp      Enable extension: first-letter (lock first char) or tp (transpose-first).");
    }
}
