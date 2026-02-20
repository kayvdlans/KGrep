using KGrep;

if (args[0] != "-E")
{
    Console.WriteLine("Expected first argument to be '-E'");
    Environment.Exit(2);
}

string pattern = args[1];
string inputLine = Console.In.ReadToEnd();

Console.Error.WriteLine("Logs from your program will appear here!");

var match = false;

foreach (string line in inputLine.Split('\n'))
{
    if (MatchPattern(line, pattern))
    {
        Console.WriteLine(line);
        match = true;
    }
}

int exitCode = match ? 0 : 1;
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

static bool Match(Fragment nfa, ReadOnlySpan<char> input)
{
    HashSet<State> current = EpsilonClosure([nfa.Start]);

    foreach (char c in input)
    {
        current = EpsilonClosure(Move(current, c));
        if (current.Count == 0)
            return false;
    }

    return current.Contains(nfa.Accept);
}

static bool MatchPrefix(Fragment nfa, ReadOnlySpan<char> input)
{
    HashSet<State> current = EpsilonClosure([nfa.Start]);

    if (current.Contains(nfa.Accept))
        return true;

    foreach (char c in input)
    {
        current = EpsilonClosure(Move(current, c));

        if (current.Count == 0)
            return false;

        if (current.Contains(nfa.Accept))
            return true;
    }

    return false;
}

static bool MatchPattern(string inputLine, string pattern)
{
    bool anchoredStart = pattern.StartsWith('^');
    bool anchoredEnd = pattern.EndsWith('$');

    if (anchoredStart) pattern = pattern[1..];
    if (anchoredEnd) pattern = pattern[..^1];

    Fragment nfa = new RegexParser(pattern).Parse();

    if (anchoredStart)
        return anchoredEnd ? Match(nfa, inputLine) : MatchPrefix(nfa, inputLine);

    for (var i = 0; i <= inputLine.Length; i++)
    {
        ReadOnlySpan<char> slice = inputLine.AsSpan(i);

        if (anchoredEnd)
        {
            if (Match(nfa, slice))
                return true;
        }
        else
        {
            if (MatchPrefix(nfa, slice))
                return true;
        }
    }

    return false;
}