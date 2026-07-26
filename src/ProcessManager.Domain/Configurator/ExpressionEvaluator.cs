using System.Globalization;

namespace ProcessManager.Domain.Configurator;

/// <summary>
/// A small, safe expression evaluator for the configurator's calculated
/// attributes, formula rules and formula-driven BoM quantities. Deliberately
/// <b>not</b> a general <c>eval</c> — it is a hand-written recursive-descent
/// parser over a whitelisted grammar (the design handoff calls out "never raw
/// eval; use a real expression parser in .NET").
///
/// Supported grammar:
///   ternary   : <c>cond ? a : b</c>
///   logical   : <c>|| &amp;&amp;</c>
///   equality  : <c>== !=</c>
///   relational: <c>&lt; &gt; &lt;= &gt;=</c>
///   additive  : <c>+ -</c>
///   multiplic.: <c>* / %</c>
///   unary     : <c>- !</c>
///   primary   : number, string literal, identifier, function call, ( expr )
///   functions : <c>has('id')</c>, ceil/floor/round/abs/min/max/roundup/rounddown
///
/// Values are <see cref="double"/>; booleans are 1/0 and a value is "truthy" when
/// it is non-zero (and finite). Any parse/eval failure or non-finite result
/// yields 0, matching the prototype engine's forgiving behaviour.
/// </summary>
public static class ExpressionEvaluator
{
    /// <summary>Evaluate <paramref name="expr"/> to a double. Returns 0 on any failure.</summary>
    public static double Eval(string? expr, EvalScope scope)
    {
        try
        {
            var parser = new Parser(expr ?? "", scope);
            var v = parser.ParseAndFinish();
            return (double.IsNaN(v) || double.IsInfinity(v)) ? 0 : v;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Evaluate to a boolean (truthy test).</summary>
    public static bool EvalBool(string? expr, EvalScope scope) => Truthy(Eval(expr, scope));

    internal static bool Truthy(double v) => v != 0 && !double.IsNaN(v);

    /// <summary>
    /// Validate an authored expression before it is committed. Reports a syntax
    /// error and any identifier that is not a known attribute / calc value / helper.
    /// </summary>
    public static ExprValidation Validate(string? expr, IEnumerable<string> knownIdentifiers, EvalScope sampleScope)
    {
        var result = new ExprValidation();
        var src = (expr ?? "").Trim();
        if (src.Length == 0)
        {
            result.Ok = false;
            result.Error = "Empty expression";
            return result;
        }

        var known = new HashSet<string>(knownIdentifiers, StringComparer.Ordinal)
        {
            "has", "true", "false", "null",
            "ceil", "floor", "round", "abs", "min", "max", "roundup", "rounddown",
        };

        // Strip string + number literals so their contents aren't read as identifiers.
        var stripped = System.Text.RegularExpressions.Regex.Replace(src, "'[^']*'|\"[^\"]*\"", " ");
        stripped = System.Text.RegularExpressions.Regex.Replace(stripped, "\\b\\d+(\\.\\d+)?\\b", " ");
        var idents = System.Text.RegularExpressions.Regex.Matches(stripped, "(?:\\.)?[A-Za-z_$][A-Za-z0-9_$]*");
        var unknown = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in idents)
        {
            var token = m.Value;
            if (token.StartsWith('.')) continue; // property access e.g. Math.ceil
            if (!known.Contains(token) && !unknown.Contains(token)) unknown.Add(token);
        }
        if (unknown.Count > 0)
        {
            result.Ok = false;
            result.Unknown = unknown;
            result.Error = "Unknown: " + string.Join(", ", unknown);
        }

        // Syntax check via a sample evaluation against the supplied scope.
        try
        {
            var parser = new Parser(src, sampleScope);
            result.Value = parser.ParseAndFinish();
        }
        catch
        {
            result.Ok = false;
            result.Error = (result.Unknown.Count > 0 ? result.Error + " · " : "") + "Syntax error";
        }
        return result;
    }

    // ── recursive-descent parser ──────────────────────────────────────────────
    private sealed class Parser
    {
        private readonly string _s;
        private int _pos;
        private readonly EvalScope _scope;

        public Parser(string s, EvalScope scope) { _s = s; _scope = scope; }

        public double ParseAndFinish()
        {
            var v = ParseTernary();
            SkipWs();
            if (_pos < _s.Length) throw new FormatException("Unexpected trailing input");
            return v;
        }

        private void SkipWs() { while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++; }

        private bool Match(string op)
        {
            SkipWs();
            if (_pos + op.Length <= _s.Length && _s.Substring(_pos, op.Length) == op)
            {
                // don't let "<" swallow "<=" etc — caller order handles multi-char first
                _pos += op.Length;
                return true;
            }
            return false;
        }

        private char Peek() { SkipWs(); return _pos < _s.Length ? _s[_pos] : '\0'; }

        private double ParseTernary()
        {
            var cond = ParseOr();
            SkipWs();
            if (Peek() == '?')
            {
                _pos++; // consume ?
                var a = ParseTernary();
                SkipWs();
                if (Peek() != ':') throw new FormatException("Expected ':' in ternary");
                _pos++; // consume :
                var b = ParseTernary();
                return Truthy(cond) ? a : b;
            }
            return cond;
        }

        private double ParseOr()
        {
            var left = ParseAnd();
            while (true)
            {
                SkipWs();
                if (Match("||"))
                {
                    var right = ParseAnd();
                    left = (Truthy(left) || Truthy(right)) ? 1 : 0;
                }
                else break;
            }
            return left;
        }

        private double ParseAnd()
        {
            var left = ParseEquality();
            while (true)
            {
                SkipWs();
                if (Match("&&"))
                {
                    var right = ParseEquality();
                    left = (Truthy(left) && Truthy(right)) ? 1 : 0;
                }
                else break;
            }
            return left;
        }

        private double ParseEquality()
        {
            var left = ParseRelational();
            while (true)
            {
                SkipWs();
                if (Match("==")) { var r = ParseRelational(); left = left == r ? 1 : 0; }
                else if (Match("!=")) { var r = ParseRelational(); left = left != r ? 1 : 0; }
                else break;
            }
            return left;
        }

        private double ParseRelational()
        {
            var left = ParseAdditive();
            while (true)
            {
                SkipWs();
                if (Match("<=")) { var r = ParseAdditive(); left = left <= r ? 1 : 0; }
                else if (Match(">=")) { var r = ParseAdditive(); left = left >= r ? 1 : 0; }
                else if (Match("<")) { var r = ParseAdditive(); left = left < r ? 1 : 0; }
                else if (Match(">")) { var r = ParseAdditive(); left = left > r ? 1 : 0; }
                else break;
            }
            return left;
        }

        private double ParseAdditive()
        {
            var left = ParseMultiplicative();
            while (true)
            {
                SkipWs();
                var c = Peek();
                if (c == '+') { _pos++; left += ParseMultiplicative(); }
                else if (c == '-') { _pos++; left -= ParseMultiplicative(); }
                else break;
            }
            return left;
        }

        private double ParseMultiplicative()
        {
            var left = ParseUnary();
            while (true)
            {
                SkipWs();
                var c = Peek();
                if (c == '*') { _pos++; left *= ParseUnary(); }
                else if (c == '/') { _pos++; left /= ParseUnary(); }
                else if (c == '%') { _pos++; left %= ParseUnary(); }
                else break;
            }
            return left;
        }

        private double ParseUnary()
        {
            SkipWs();
            var c = Peek();
            if (c == '-') { _pos++; return -ParseUnary(); }
            if (c == '+') { _pos++; return ParseUnary(); }
            if (c == '!') { _pos++; return Truthy(ParseUnary()) ? 0 : 1; }
            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            SkipWs();
            var c = Peek();
            if (c == '(')
            {
                _pos++;
                var v = ParseTernary();
                SkipWs();
                if (Peek() != ')') throw new FormatException("Expected ')'");
                _pos++;
                return v;
            }
            if (char.IsDigit(c) || c == '.') return ParseNumber();
            if (c == '\'' || c == '"') throw new FormatException("Unexpected string literal");
            if (char.IsLetter(c) || c == '_' || c == '$') return ParseIdentifierOrCall();
            throw new FormatException($"Unexpected character '{c}'");
        }

        private double ParseNumber()
        {
            var start = _pos;
            while (_pos < _s.Length && (char.IsDigit(_s[_pos]) || _s[_pos] == '.')) _pos++;
            return double.Parse(_s.Substring(start, _pos - start), CultureInfo.InvariantCulture);
        }

        private string ParseStringLiteral()
        {
            var quote = _s[_pos];
            _pos++; // opening quote
            var start = _pos;
            while (_pos < _s.Length && _s[_pos] != quote) _pos++;
            var str = _s.Substring(start, _pos - start);
            if (_pos >= _s.Length) throw new FormatException("Unterminated string");
            _pos++; // closing quote
            return str;
        }

        private double ParseIdentifierOrCall()
        {
            var start = _pos;
            while (_pos < _s.Length && (char.IsLetterOrDigit(_s[_pos]) || _s[_pos] == '_' || _s[_pos] == '$')) _pos++;
            var name = _s.Substring(start, _pos - start);

            SkipWs();
            if (Peek() == '(')
            {
                _pos++; // (
                var args = new List<object>();
                SkipWs();
                if (Peek() != ')')
                {
                    do
                    {
                        SkipWs();
                        var ch = Peek();
                        if (ch == '\'' || ch == '"') args.Add(ParseStringLiteral());
                        else args.Add(ParseTernary());
                        SkipWs();
                    } while (Peek() == ',' && ++_pos > 0);
                }
                SkipWs();
                if (Peek() != ')') throw new FormatException("Expected ')' in call");
                _pos++;
                return CallFunction(name, args);
            }

            return name switch
            {
                "true" => 1,
                "false" => 0,
                "null" => 0,
                _ => _scope.GetValue(name),
            };
        }

        private double CallFunction(string name, List<object> args)
        {
            double Arg(int i) => args[i] is double d ? d : 0;
            switch (name)
            {
                case "has":
                    return _scope.Has(args.Count > 0 ? args[0]?.ToString() ?? "" : "") ? 1 : 0;
                case "ceil":
                case "roundup":
                    return Math.Ceiling(Arg(0));
                case "floor":
                case "rounddown":
                    return Math.Floor(Arg(0));
                case "round":
                    return Math.Round(Arg(0), MidpointRounding.AwayFromZero);
                case "abs":
                    return Math.Abs(Arg(0));
                case "min":
                    return Math.Min(Arg(0), Arg(1));
                case "max":
                    return Math.Max(Arg(0), Arg(1));
                default:
                    throw new FormatException($"Unknown function '{name}'");
            }
        }
    }
}

/// <summary>Evaluation scope: numeric identifier values plus a <c>has('id')</c> predicate.</summary>
public sealed class EvalScope
{
    private readonly Dictionary<string, double> _values;
    private readonly Func<string, bool> _has;

    public EvalScope(Dictionary<string, double> values, Func<string, bool> has)
    {
        _values = values;
        _has = has;
    }

    public double GetValue(string id) => _values.TryGetValue(id, out var v) ? v : 0;
    public bool Has(string id) => _has(id);
}

public sealed class ExprValidation
{
    public bool Ok { get; set; } = true;
    public string Error { get; set; } = "";
    public List<string> Unknown { get; set; } = new();
    public double Value { get; set; }
}
