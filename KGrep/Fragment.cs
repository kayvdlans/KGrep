namespace KGrep;

public class Fragment(State start, State accept)
{
    public State Start { get; } = start;
    public State Accept { get; } = accept;

    public Fragment Clone()
    {
        // Maps old state to new state.
        var map = new Dictionary<State, State>();
        State newStart = CloneState(Start);
        State newAccept = map[Accept];

        return new Fragment(newStart, newAccept);

        State CloneState(State s)
        {
            if (map.TryGetValue(s, out State? existing))
                return existing;

            var ns = new State();
            map[s] = ns;

            foreach (Transition t in s.Transitions)
            {
                ns.Transitions.Add(new Transition { Target = CloneState(t.Target), Condition = t.Condition });
            }

            return ns;
        }
    }
}