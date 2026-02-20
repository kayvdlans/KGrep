namespace KGrep;

public static class Nfa
{
    public static Fragment Epsilon()
    {
        var start = new State();
        var accept = new State();

        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        return new Fragment(start, accept);
    }

    public static Fragment PredicateAtom(Func<char, bool> predicate)
    {
        var start = new State();
        var accept = new State();

        start.Transitions.Add(new Transition
        {
            Condition = predicate,
            Target = accept
        });

        return new Fragment(start, accept);
    }

    public static Fragment Literal(char c)
    {
        return PredicateAtom(ch => ch == c);
    }

    public static Fragment Concat(Fragment a, Fragment b)
    {
        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = b.Start
        });

        return new Fragment(a.Start, b.Accept);
    }

    public static Fragment Alternate(Fragment a, Fragment b)
    {
        var start = new State();
        var accept = new State();

        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = a.Start
        });
        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = b.Start
        });

        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });
        b.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        return new Fragment(start, accept);
    }

    public static Fragment Star(Fragment a)
    {
        var start = new State();
        var accept = new State();

        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = a.Start
        });
        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = a.Start
        });
        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        return new Fragment(start, accept);
    }

    public static Fragment Plus(Fragment a)
    {
        var start = new State();
        var accept = new State();

        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = a.Start
        });

        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = a.Start
        });
        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        return new Fragment(start, accept);
    }

    public static Fragment Question(Fragment a)
    {
        var start = new State();
        var accept = new State();

        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = a.Start
        });
        start.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        a.Accept.Transitions.Add(new Transition
        {
            Condition = null,
            Target = accept
        });

        return new Fragment(start, accept);
    }
}