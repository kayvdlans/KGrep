using KGrep;

var onlyMatching = false;
var extendedRegularExpressions = false;

var pattern = string.Empty;
string inputLine = Console.In.ReadToEnd();

foreach (string arg in args)
{
    switch (arg)
    {
        case "-o":
            onlyMatching = true;
            break;
        case "-E":
            extendedRegularExpressions = true;
            break;
        default:
            if (!extendedRegularExpressions)
            {
                Console.WriteLine("Expected argument '-E' to be defined before the pattern.");
                Environment.Exit(2);
            }

            pattern = arg;
            break;
    }
}

var foundMatch = false;

foreach (string line in inputLine.Split(Environment.NewLine))
{
    if (MatchPattern(line, pattern, out List<string> matches))
    {
        if (onlyMatching)
            foreach (string match in matches)
            {
                Console.WriteLine(match);
            }
        else
            Console.WriteLine(line);

        foundMatch = true;
    }
}

int exitCode = foundMatch ? 0 : 1;
Environment.Exit(exitCode);

return;

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

static bool MatchPattern(string inputLine, string pattern, out List<string> matches)
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

        if (anchoredEnd)
        {
            if (length != line.Length)
                return false;

            matches.Add(inputLine);
            return true;
        }

        matches.Add(inputLine[..length]);
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

        matches.Add(inputLine.Substring(start, bestLength));
        pos = start + Math.Max(bestLength, 1);
    }

    return matches.Count > 0;
}