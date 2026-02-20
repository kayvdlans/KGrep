namespace KGrep;

public class Fragment(State start, State accept)
{
    public State Start { get; } = start;
    public State Accept { get; } = accept;
}