namespace KGrep;

public class Transition
{
    public Func<char, bool>? Condition { get; init; }
    public State Target { get; init; } = null!;

    public bool IsEpsilon => Condition == null;
}