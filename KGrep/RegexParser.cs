namespace KGrep;

public class RegexParser(string pattern)
{
    private int _index;

    private bool End => _index >= pattern.Length;
    private char Peek => End ? '\0' : pattern[_index];

    private char Next()
    {
        return End ? '\0' : pattern[_index++];
    }

    private bool Consume(char c)
    {
        if (Peek != c)
            return false;

        _index++;
        return true;
    }

    public Fragment Parse()
    {
        Fragment expression = ParseExpresion();

        return End ? expression : throw new Exception($"Unexpected char '{Peek} at {_index}");
    }

    private Fragment ParseExpresion()
    {
        Fragment left = ParseTerm();

        while (Consume('|'))
        {
            Fragment right = ParseTerm();
            left = Nfa.Alternate(left, right);
        }

        return left;
    }

    private Fragment ParseTerm()
    {
        Fragment? accept = null;

        while (!End && Peek != ')' && Peek != '|')
        {
            Fragment factor = ParseFactor();
            accept = accept is null ? factor : Nfa.Concat(accept, factor);
        }

        return accept ?? Nfa.Epsilon();
    }

    private Fragment ParseFactor()
    {
        Fragment atom = ParseAtom();

        if (!End)
        {
            if (Consume('*')) return Nfa.Star(atom);
            if (Consume('+')) return Nfa.Plus(atom);
            if (Consume('?')) return Nfa.Question(atom);
        }

        return atom;
    }

    private Fragment ParseAtom()
    {
        if (Consume('('))
        {
            Fragment expression = ParseExpresion();

            return Consume(')') ? expression : throw new Exception("Unclosed '('");
        }

        if (Consume('.'))
            return Nfa.PredicateAtom(_ => true);

        if (Consume('\\'))
        {
            char next = Next();
            return next switch
            {
                'd' => Nfa.PredicateAtom(char.IsAsciiDigit),
                'w' => Nfa.PredicateAtom(c => char.IsAsciiLetterOrDigit(c) || c == '_'),
                '\\' => Nfa.Literal('\\'),
                _ => throw new Exception($"Unsupported escape \\{next}")
            };
        }

        return Peek == '[' ? ParseCharacterClass() : Nfa.Literal(Next());
    }

    private Fragment ParseCharacterClass()
    {
        Consume('[');

        bool negate = Consume('^');

        List<char> chars = [];
        while (!End && Peek != ']')
        {
            chars.Add(Next());
        }

        if (!Consume(']'))
            throw new Exception("Unclosed '['");

        char[] set = [.. chars];
        Func<char, bool> pred = negate
            ? c => Array.IndexOf(set, c) < 0
            : c => Array.IndexOf(set, c) >= 0;

        return Nfa.PredicateAtom(pred);
    }
}