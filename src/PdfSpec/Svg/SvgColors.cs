using System.Globalization;
using PdfSpec.Geometry;

namespace PdfSpec.Svg;

/// <summary>
/// Parse SVG / CSS colour expressions into <see cref="SvgPaint"/>.
/// Supports <c>none</c> / <c>transparent</c>, <c>#RGB</c>, <c>#RRGGBB</c>,
/// <c>rgb(r, g, b)</c> (integers 0-255 or percentages), and a small set
/// of named CSS colours. Anything unrecognised falls through as
/// <c>null</c> (= "inherit").
/// </summary>
internal static class SvgColors
{
    public static SvgPaint? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();

        if (s.Equals("none", StringComparison.OrdinalIgnoreCase)
            || s.Equals("transparent", StringComparison.OrdinalIgnoreCase))
            return SvgPaint.None;

        if (s.StartsWith('#'))
        {
            var hex = s.AsSpan(1);
            if (hex.Length == 3)
            {
                int r = HexDigit(hex[0]); int g = HexDigit(hex[1]); int b = HexDigit(hex[2]);
                if (r < 0 || g < 0 || b < 0) return null;
                return SvgPaint.Of(PdfColor.Rgb((r * 17) / 255.0, (g * 17) / 255.0, (b * 17) / 255.0));
            }
            if (hex.Length == 6)
            {
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                    return null;
                return SvgPaint.Of(PdfColor.FromHex(rgb));
            }
            return null;
        }

        if (s.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(')'))
        {
            var inner = s.Substring(4, s.Length - 5);
            var parts = inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return null;
            if (!TryParseComponent(parts[0], out var r) ||
                !TryParseComponent(parts[1], out var g) ||
                !TryParseComponent(parts[2], out var b))
                return null;
            return SvgPaint.Of(PdfColor.Rgb(r, g, b));
        }

        return Named.TryGetValue(s, out var named) ? SvgPaint.Of(named) : null;
    }

    private static bool TryParseComponent(string s, out double v)
    {
        if (s.EndsWith('%'))
        {
            if (double.TryParse(s[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
            { v = Math.Clamp(pct / 100.0, 0, 1); return true; }
            v = 0; return false;
        }
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
        { v = Math.Clamp(n / 255.0, 0, 1); return true; }
        v = 0; return false;
    }

    private static int HexDigit(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    // Small CSS-named subset (about 30 names) — covers the colours seen
    // in the kinds of icons users typically paste in. Anything else
    // either uses the hex / rgb forms or quietly inherits.
    private static readonly Dictionary<string, PdfColor> Named =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["black"]     = PdfColor.FromHex(0x000000),
            ["white"]     = PdfColor.FromHex(0xFFFFFF),
            ["silver"]    = PdfColor.FromHex(0xC0C0C0),
            ["gray"]      = PdfColor.FromHex(0x808080),
            ["grey"]      = PdfColor.FromHex(0x808080),
            ["maroon"]    = PdfColor.FromHex(0x800000),
            ["red"]       = PdfColor.FromHex(0xFF0000),
            ["purple"]    = PdfColor.FromHex(0x800080),
            ["fuchsia"]   = PdfColor.FromHex(0xFF00FF),
            ["green"]     = PdfColor.FromHex(0x008000),
            ["lime"]      = PdfColor.FromHex(0x00FF00),
            ["olive"]     = PdfColor.FromHex(0x808000),
            ["yellow"]    = PdfColor.FromHex(0xFFFF00),
            ["navy"]      = PdfColor.FromHex(0x000080),
            ["blue"]      = PdfColor.FromHex(0x0000FF),
            ["teal"]      = PdfColor.FromHex(0x008080),
            ["aqua"]      = PdfColor.FromHex(0x00FFFF),
            ["cyan"]      = PdfColor.FromHex(0x00FFFF),
            ["magenta"]   = PdfColor.FromHex(0xFF00FF),
            ["orange"]    = PdfColor.FromHex(0xFFA500),
            ["pink"]      = PdfColor.FromHex(0xFFC0CB),
            ["brown"]     = PdfColor.FromHex(0xA52A2A),
            ["gold"]      = PdfColor.FromHex(0xFFD700),
            ["indigo"]    = PdfColor.FromHex(0x4B0082),
            ["violet"]    = PdfColor.FromHex(0xEE82EE),
            ["beige"]     = PdfColor.FromHex(0xF5F5DC),
            ["coral"]     = PdfColor.FromHex(0xFF7F50),
            ["khaki"]     = PdfColor.FromHex(0xF0E68C),
            ["salmon"]    = PdfColor.FromHex(0xFA8072),
            ["tan"]       = PdfColor.FromHex(0xD2B48C),
            ["turquoise"] = PdfColor.FromHex(0x40E0D0),
            ["wheat"]     = PdfColor.FromHex(0xF5DEB3),
            ["lightgray"] = PdfColor.FromHex(0xD3D3D3),
            ["lightgrey"] = PdfColor.FromHex(0xD3D3D3),
            ["darkgray"]  = PdfColor.FromHex(0xA9A9A9),
            ["darkgrey"]  = PdfColor.FromHex(0xA9A9A9),
        };
}
