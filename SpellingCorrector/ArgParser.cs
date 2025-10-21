static class ArgParser
{
    public static Dictionary<string,string> Parse(string[] args)
    {
        var dict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith("--"))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    dict[a] = args[i + 1];
                    i++;
                }
                else
                {
                    dict[a] = "true"; // flag without value
                }
            }
        }
        return dict;
    }
}