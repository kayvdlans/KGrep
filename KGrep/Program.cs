using System.Text;
using KGrep;

var recursiveFileSearch = false;
var onlyMatching = false;
var extendedRegularExpressions = false;
var mode = ColorMode.Never;

var pattern = string.Empty;
List<SourceFile> inputFiles = [];

foreach (string arg in args)
{
    switch (arg)
    {
        case not null when arg.StartsWith("--color="):
            mode = arg[8..].ToLowerInvariant() switch
            {
                "never" => ColorMode.Never,
                "always" => ColorMode.Always,
                "auto" => ColorMode.Auto,
                _ => throw new Exception("Unexpected color value.")
            };
            break;
        case "-r":
            recursiveFileSearch = true;
            break;
        case "-o":
            onlyMatching = true;
            break;
        case "-E":
            extendedRegularExpressions = true;
            break;
        case not null:
            if (!extendedRegularExpressions)
            {
                Console.WriteLine("Expected argument '-E' to be defined before the pattern.");
                Environment.Exit(2);
            }

            if (string.IsNullOrEmpty(pattern))
            {
                pattern = arg;
                break;
            }

            if (!Directory.Exists(arg) && !File.Exists(arg))
            {
                Console.WriteLine($"Expected '{arg}' to be a valid file path.");
                Environment.Exit(2);
            }

            if (!recursiveFileSearch)
            {
                inputFiles.Add(new SourceFile(arg, File.ReadAllText(arg)));
                break;
            }

            inputFiles.AddRange(Directory
                .EnumerateFiles(arg, "*", SearchOption.AllDirectories)
                .Select(file => new SourceFile(file, File.ReadAllText(file))));

            break;
    }
}

if (inputFiles.Count == 0)
    inputFiles.Add(new SourceFile("Console", Console.In.ReadToEnd()));

var foundMatch = false;
foreach (SourceFile file in inputFiles)
{
    foreach (string line in file.Input.Split(Environment.NewLine))
    {
        if (MatchPattern(line, pattern, out List<Match> matches))
        {
            ReadOnlySpan<char> span = line.AsSpan();
            var start = 0;
            var builder = new StringBuilder();
            foreach (Match match in matches)
            {
                if (inputFiles.Count > 1)
                    builder.Append($"{file.Path}:");

                if (onlyMatching)
                {
                    builder.Append(span.Slice(match.Start, match.Length));
                    builder.Append(Environment.NewLine);
                    continue;
                }

                builder.Append(span[start..match.Start]);
                builder.Append(ShouldHighlight(mode) ? "\x1b[01;31m" : "");
                builder.Append(span.Slice(match.Start, match.Length));
                builder.Append(ShouldHighlight(mode) ? "\x1b[m" : "");
                start = match.Start + match.Length;
            }

            if (!onlyMatching)
            {
                if (start < line.Length)
                    builder.Append(span[start..]);

                builder.Append(Environment.NewLine);
            }

            Console.Write(builder);

            foundMatch = true;
        }
    }
}


int exitCode = foundMatch ? 0 : 1;
Environment.Exit(exitCode);

return;

static bool ShouldHighlight(ColorMode mode)
{
    return mode switch
    {
        ColorMode.Never => false,
        ColorMode.Always => true,
        ColorMode.Auto => !Console.IsOutputRedirected,
        _ => throw new Exception($"Invalid ColorMode: {mode}")
    };
}

static HashSet<State> EpsilonClosure(IEnumerable<State> states)
{
    State[] collection = states as State[] ?? [.. states];
    var stack = new Stack<State>(collection);
    var closure = new HashSet<State>(collection);

    while (stack.Count > 0)
    {
        State state = stack.Pop();

        foreach (Transition t in state.Transitions.Where(t => t.IsEpsilon && !closure.Contains(t.Target)))
        {
            closure.Add(t.Target);
            stack.Push(t.Target);
        }
    }

    return closure;
}

static HashSet<State> Move(IEnumerable<State> states, char c)
{
    var result = new HashSet<State>();

    foreach (State state in states)
    {
        foreach (Transition t in state.Transitions.Where(t => !t.IsEpsilon && t.Condition!(c)))
        {
            result.Add(t.Target);
        }
    }

    return result;
}


static bool Match(Fragment nfa, ReadOnlySpan<char> input, out int length)
{
    length = -1;

    HashSet<State> current = EpsilonClosure([nfa.Start]);

    if (current.Contains(nfa.Accept))
        length = 0;

    for (var i = 0; i < input.Length; i++)
    {
        current = EpsilonClosure(Move(current, input[i]));

        if (current.Count == 0)
            break;

        if (current.Contains(nfa.Accept))
            length = i + 1;
    }

    return length >= 0;
}

static bool MatchPattern(string inputLine, string pattern, out List<Match> matches)
{
    matches = [];

    bool anchoredStart = pattern.StartsWith('^');
    bool anchoredEnd = pattern.EndsWith('$');

    if (anchoredStart) pattern = pattern[1..];
    if (anchoredEnd) pattern = pattern[..^1];

    Fragment nfa = new RegexParser(pattern).Parse();
    ReadOnlySpan<char> line = inputLine.AsSpan();

    if (anchoredStart)
    {
        if (!Match(nfa, inputLine, out int length))
            return false;

        if (anchoredEnd && length != line.Length)
            return false;

        matches.Add(new Match(0, length));
        return true;
    }

    var pos = 0;
    while (pos <= line.Length)
    {
        var found = false;

        int start;
        int bestLength = -1;

        for (start = pos; start <= line.Length; start++)
        {
            if (!Match(nfa, line[start..], out int length))
                continue;

            if (anchoredEnd && start + length != line.Length)
                continue;

            bestLength = length;
            found = true;
            break;
        }

        if (!found)
            break;

        matches.Add(new Match(start, bestLength));
        pos = start + Math.Max(bestLength, 1);
    }

    return matches.Count > 0;
}