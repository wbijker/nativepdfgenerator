using System.Globalization;
using System.Xml.Linq;

namespace PdfSpec.Svg;

/// <summary>
/// Parse SVG XML into an <see cref="SvgDocument"/>. Reads the root
/// <c>&lt;svg&gt;</c>'s width / height / viewBox; walks supported
/// children — <c>g</c>, <c>rect</c>, <c>circle</c>, <c>ellipse</c>,
/// <c>line</c>, <c>polyline</c>, <c>polygon</c>, <c>path</c>; collects
/// fill / stroke / stroke-width / opacity / fill-opacity /
/// stroke-opacity / transform on each node (with inline <c>style="…"</c>
/// taking precedence over presentation attributes).
///
/// <para>
/// Unsupported elements (text, image, defs, style, gradients, …) are
/// silently skipped — they round-trip out as empty space, not as a
/// parse error.
/// </para>
/// </summary>
internal static class SvgParser
{
    public static SvgDocument Parse(string xml)
    {
        XDocument doc;
        try { doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace); }
        catch (System.Xml.XmlException ex)
        {
            throw new FormatException("SVG content is not valid XML.", ex);
        }

        var root = doc.Root
            ?? throw new FormatException("SVG document is empty.");
        if (root.Name.LocalName != "svg")
            throw new FormatException($"Expected <svg> root, got <{root.Name.LocalName}>.");

        double width  = ParseLength(Attr(root, "width"))  ?? 0;
        double height = ParseLength(Attr(root, "height")) ?? 0;
        var viewBox = ParseViewBox(Attr(root, "viewBox"));

        // No explicit size → fall back to the viewBox dims.
        if (width  <= 0 && viewBox is { } vb) width  = vb.Width;
        if (height <= 0 && viewBox is { } vb2) height = vb2.Height;

        var rootGroup = new SvgGroup { Attrs = ParseAttrs(root) };
        ParseChildren(root, rootGroup);

        return new SvgDocument
        {
            IntrinsicWidth = width,
            IntrinsicHeight = height,
            ViewBox = viewBox,
            Root = rootGroup,
        };
    }

    private static void ParseChildren(XElement parent, SvgGroup g)
    {
        foreach (var el in parent.Elements())
        {
            if (ParseElement(el) is { } child)
                g.Children.Add(child);
        }
    }

    private static SvgNode? ParseElement(XElement el)
    {
        SvgNode? node = el.Name.LocalName switch
        {
            "g"        => ParseGroup(el),
            "rect"     => ParseRect(el),
            "circle"   => ParseCircle(el),
            "ellipse"  => ParseEllipse(el),
            "line"     => ParseLine(el),
            "polyline" => ParsePolyline(el, closed: false),
            "polygon"  => ParsePolyline(el, closed: true),
            "path"     => ParsePath(el),
            _          => null,
        };
        if (node is not null) node.Attrs = ParseAttrs(el);
        return node;
    }

    private static SvgGroup ParseGroup(XElement el)
    {
        var g = new SvgGroup();
        ParseChildren(el, g);
        return g;
    }

    private static SvgRect ParseRect(XElement el) => new()
    {
        X      = N(el, "x"),
        Y      = N(el, "y"),
        Width  = N(el, "width"),
        Height = N(el, "height"),
        Rx     = N(el, "rx"),
        Ry     = N(el, "ry"),
    };

    private static SvgCircle ParseCircle(XElement el) => new()
    {
        Cx = N(el, "cx"),
        Cy = N(el, "cy"),
        R  = N(el, "r"),
    };

    private static SvgEllipse ParseEllipse(XElement el) => new()
    {
        Cx = N(el, "cx"),
        Cy = N(el, "cy"),
        Rx = N(el, "rx"),
        Ry = N(el, "ry"),
    };

    private static SvgLine ParseLine(XElement el) => new()
    {
        X1 = N(el, "x1"),
        Y1 = N(el, "y1"),
        X2 = N(el, "x2"),
        Y2 = N(el, "y2"),
    };

    private static SvgPolyline ParsePolyline(XElement el, bool closed) =>
        new() { Points = ParseNumberList(Attr(el, "points") ?? ""), Closed = closed };

    private static SvgPath ParsePath(XElement el) =>
        new() { D = Attr(el, "d") ?? "" };

    // ===== attributes ========================================================

    private static SvgAttrs ParseAttrs(XElement el)
    {
        var a = new SvgAttrs
        {
            Fill           = SvgColors.Parse(Attr(el, "fill")),
            Stroke         = SvgColors.Parse(Attr(el, "stroke")),
            StrokeWidth    = ParseNumber(Attr(el, "stroke-width")),
            Opacity        = ParseNumber(Attr(el, "opacity")),
            FillOpacity    = ParseNumber(Attr(el, "fill-opacity")),
            StrokeOpacity  = ParseNumber(Attr(el, "stroke-opacity")),
            Transform      = ParseTransform(Attr(el, "transform")),
        };
        // Inline style overrides per CSS cascade rules.
        if (Attr(el, "style") is { } style) MergeStyleAttribute(style, a);
        return a;
    }

    private static void MergeStyleAttribute(string style, SvgAttrs a)
    {
        foreach (var rule in style.Split(';',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int colon = rule.IndexOf(':');
            if (colon < 0) continue;
            var key = rule[..colon].Trim();
            var val = rule[(colon + 1)..].Trim();
            switch (key)
            {
                case "fill":            a.Fill          = SvgColors.Parse(val); break;
                case "stroke":          a.Stroke        = SvgColors.Parse(val); break;
                case "stroke-width":    a.StrokeWidth   = ParseNumber(val); break;
                case "opacity":         a.Opacity       = ParseNumber(val); break;
                case "fill-opacity":    a.FillOpacity   = ParseNumber(val); break;
                case "stroke-opacity":  a.StrokeOpacity = ParseNumber(val); break;
            }
        }
    }

    // ===== primitives ========================================================

    private static string? Attr(XElement el, string name) => el.Attribute(name)?.Value;
    private static double N(XElement el, string name) => ParseNumber(Attr(el, name)) ?? 0;

    /// <summary>Parse <paramref name="s"/> as a double, ignoring trailing unit text (px / mm / …) — SVG is pixel-units-by-default and pixels ≈ points for our purposes.</summary>
    private static double? ParseLength(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var span = s.AsSpan().Trim();
        int end = 0;
        while (end < span.Length && (char.IsDigit(span[end]) || span[end] is '+' or '-' or '.' or 'e' or 'E'))
            end++;
        if (end == 0) return null;
        return double.Parse(span[..end], NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static double? ParseNumber(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : ParseLength(s);
    }

    private static (double X, double Y, double Width, double Height)? ParseViewBox(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var parts = ParseNumberList(raw);
        if (parts.Length < 4) return null;
        return (parts[0], parts[1], parts[2], parts[3]);
    }

    // ===== transform ========================================================

    private static SvgMatrix? ParseTransform(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var m = SvgMatrix.Identity;
        int i = 0;
        while (i < raw.Length)
        {
            while (i < raw.Length && (char.IsWhiteSpace(raw[i]) || raw[i] == ',')) i++;
            if (i >= raw.Length) break;

            int nameStart = i;
            while (i < raw.Length && (char.IsLetter(raw[i]) || raw[i] == '_')) i++;
            var name = raw[nameStart..i];

            while (i < raw.Length && raw[i] != '(') i++;
            if (i >= raw.Length) break;
            i++; // (
            int argStart = i;
            while (i < raw.Length && raw[i] != ')') i++;
            var argsStr = raw[argStart..i];
            if (i < raw.Length) i++; // )

            var args = ParseNumberList(argsStr);
            m = m.Multiply(BuildOpMatrix(name, args));
        }
        return m;
    }

    private static SvgMatrix BuildOpMatrix(string name, double[] args) => name switch
    {
        "translate" => SvgMatrix.Translate(
            args.Length > 0 ? args[0] : 0,
            args.Length > 1 ? args[1] : 0),
        "scale" => SvgMatrix.Scale(
            args.Length > 0 ? args[0] : 1,
            args.Length > 1 ? args[1] : (args.Length > 0 ? args[0] : 1)),
        "rotate" => args.Length >= 3
            ? SvgMatrix.Rotate(args[0], args[1], args[2])
            : SvgMatrix.Rotate(args.Length > 0 ? args[0] : 0),
        "skewX" => SvgMatrix.SkewX(args.Length > 0 ? args[0] : 0),
        "skewY" => SvgMatrix.SkewY(args.Length > 0 ? args[0] : 0),
        "matrix" when args.Length >= 6 => new SvgMatrix(args[0], args[1], args[2], args[3], args[4], args[5]),
        _ => SvgMatrix.Identity,
    };

    // ===== number list ======================================================

    /// <summary>Parse a whitespace / comma-separated list of doubles. Handles signed numbers with decimals and scientific notation; copes with no-separator-between-signed-numbers (e.g. <c>"10-5"</c> → <c>[10, -5]</c>).</summary>
    public static double[] ParseNumberList(string s)
    {
        var list = new List<double>();
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
            if (i >= s.Length) break;
            int start = i;
            if (s[i] is '-' or '+') i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
            // Exponent
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                i++;
                if (i < s.Length && (s[i] is '-' or '+')) i++;
                while (i < s.Length && char.IsDigit(s[i])) i++;
            }
            if (i > start)
            {
                if (double.TryParse(s[start..i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    list.Add(v);
            }
            else
            {
                // Defensive: nothing parsed, advance to avoid infinite loop.
                i++;
            }
        }
        return list.ToArray();
    }
}
