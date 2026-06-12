using System.Globalization;

namespace PdfSpec.Svg;

/// <summary>
/// Tokenize an SVG path <c>d</c> attribute into a flat list of
/// (command, args) tuples. Handles implicit-continuation (subsequent
/// number pairs after <c>M</c>/<c>m</c> act as <c>L</c>/<c>l</c>),
/// signed-numbers-without-separator (<c>"10-5"</c> → <c>10, -5</c>),
/// and the SVG arc's flag-adjacency rule (args 3 and 4 of A/a are
/// single-digit 0/1 flags).
///
/// <para>
/// Supported commands: M m L l H h V v C c S s Q q T t A a Z z. The
/// renderer turns Q/T quadratics into equivalent cubics and approximates
/// A arcs with cubic bezier segments.
/// </para>
/// </summary>
internal static class SvgPathParser
{
    public readonly struct Op
    {
        public char Cmd { get; }
        public double[] Args { get; }
        public Op(char cmd, double[] args) { Cmd = cmd; Args = args; }
    }

    public static List<Op> Parse(string d)
    {
        var ops = new List<Op>();
        if (string.IsNullOrEmpty(d)) return ops;

        int i = 0;
        char pending = '\0';
        while (i < d.Length)
        {
            SkipSep(d, ref i);
            if (i >= d.Length) break;

            char c = d[i];
            if (char.IsLetter(c))
            {
                pending = c;
                i++;
            }

            if (pending == '\0')
                throw new FormatException("SVG path must start with a command letter.");

            int n = ArgCount(pending);
            if (n == 0)
            {
                ops.Add(new Op(pending, Array.Empty<double>()));
                continue;
            }

            // For implicit continuation we may stop before another set —
            // skip ws/commas first and bail out if a new command starts.
            SkipSep(d, ref i);
            if (i >= d.Length) break;
            if (char.IsLetter(d[i])) continue;

            var args = new double[n];
            bool isArc = pending is 'A' or 'a';
            for (int j = 0; j < n; j++)
            {
                SkipSep(d, ref i);
                if (i >= d.Length)
                    throw new FormatException("Unexpected end of SVG path data.");

                if (isArc && (j == 3 || j == 4))
                {
                    // Single-digit flag. Spec says it's 0 or 1 with no
                    // separator required — read exactly one character.
                    args[j] = d[i] == '1' ? 1 : 0;
                    i++;
                }
                else
                {
                    args[j] = ReadNumber(d, ref i);
                }
            }

            ops.Add(new Op(pending, args));

            // M/m → L/l for subsequent implicit pairs.
            if (pending == 'M') pending = 'L';
            else if (pending == 'm') pending = 'l';
        }

        return ops;
    }

    private static int ArgCount(char c) => c switch
    {
        'M' or 'm' or 'L' or 'l' or 'T' or 't' => 2,
        'H' or 'h' or 'V' or 'v' => 1,
        'C' or 'c' => 6,
        'S' or 's' or 'Q' or 'q' => 4,
        'A' or 'a' => 7,
        'Z' or 'z' => 0,
        _ => throw new FormatException($"Unknown SVG path command: {c}"),
    };

    private static void SkipSep(string d, ref int i)
    {
        while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ',')) i++;
    }

    private static double ReadNumber(string d, ref int i)
    {
        int start = i;
        if (i < d.Length && (d[i] == '-' || d[i] == '+')) i++;
        bool seenDot = false;
        while (i < d.Length && (char.IsDigit(d[i]) || (d[i] == '.' && !seenDot)))
        {
            if (d[i] == '.') seenDot = true;
            i++;
        }
        if (i < d.Length && (d[i] == 'e' || d[i] == 'E'))
        {
            i++;
            if (i < d.Length && (d[i] == '-' || d[i] == '+')) i++;
            while (i < d.Length && char.IsDigit(d[i])) i++;
        }

        if (i == start)
            throw new FormatException($"Expected a number in SVG path at index {start}.");

        return double.Parse(d.AsSpan(start, i - start),
            NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
