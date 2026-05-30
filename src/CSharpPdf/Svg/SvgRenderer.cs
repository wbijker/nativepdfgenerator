using System.Globalization;
using System.Xml.Linq;
using CSharpPdf.Content;

namespace CSharpPdf.Svg;

/// <summary>
/// Minimal SVG → PDF renderer. Parses a subset of SVG 1.1 — rect, circle,
/// ellipse, line, polygon, polyline, path (M/L/H/V/C/S/Q/T/Z, absolute and
/// relative), and <c>g</c> groups — and emits PDF content-stream operators.
/// Styling supported: fill, stroke, stroke-width, fill-opacity, stroke-opacity
/// (via the rendering colour, no transparency group), and <c>style="..."</c>.
/// Coordinates are placed by setting a single <c>cm</c> transform that maps the
/// viewBox to the requested PDF rectangle (and flips Y so SVG y-down works).
/// </summary>
internal sealed class SvgRenderer
{
    private readonly ContentStream _cs;

    public SvgRenderer(ContentStream cs) { _cs = cs; }

    public void Render(string svgXml, double pdfX, double pdfY, double pdfWidth, double pdfHeight)
    {
        XDocument doc = XDocument.Parse(svgXml);
        XElement root = doc.Root ?? throw new InvalidOperationException("Empty SVG.");

        var (vbX, vbY, vbW, vbH) = ParseViewBox(root, pdfWidth, pdfHeight);
        double sx = pdfWidth / vbW;
        double sy = pdfHeight / vbH;

        _cs.Save();
        // PDF cm maps source (svg) → page: (sx, 0, 0, -sy, tx, ty)
        // tx, ty places the *top-left* of the SVG box at (pdfX, pdfY).
        _cs.Transform(sx, 0, 0, -sy, pdfX - vbX * sx, pdfY + vbY * sy);

        RenderChildren(root);

        _cs.Restore();
    }

    private void RenderChildren(XElement parent)
    {
        foreach (var el in parent.Elements())
        {
            switch (el.Name.LocalName)
            {
                case "g":         RenderGroup(el); break;
                case "rect":      RenderRect(el); break;
                case "circle":    RenderCircle(el); break;
                case "ellipse":   RenderEllipse(el); break;
                case "line":      RenderLine(el); break;
                case "polygon":   RenderPolygon(el, close: true); break;
                case "polyline":  RenderPolygon(el, close: false); break;
                case "path":      RenderPath(el); break;
                // unknown element — silently skip
            }
        }
    }

    private void RenderGroup(XElement g)
    {
        _cs.Save();
        ApplyGroupTransform(g);
        RenderChildren(g);
        _cs.Restore();
    }

    private void ApplyGroupTransform(XElement g)
    {
        string? t = g.Attribute("transform")?.Value;
        if (string.IsNullOrEmpty(t)) return;
        // Parse a small set: translate(x[,y]), scale(sx[,sy]), rotate(deg)
        int i = 0;
        while (i < t.Length)
        {
            while (i < t.Length && (char.IsWhiteSpace(t[i]) || t[i] == ',')) i++;
            int nameStart = i;
            while (i < t.Length && char.IsLetter(t[i])) i++;
            if (i == nameStart) break;
            string name = t.Substring(nameStart, i - nameStart);
            while (i < t.Length && t[i] != '(') i++;
            if (i >= t.Length) break;
            i++; // skip '('
            var args = new List<double>();
            while (i < t.Length && t[i] != ')')
            {
                while (i < t.Length && (char.IsWhiteSpace(t[i]) || t[i] == ',')) i++;
                if (i < t.Length && t[i] != ')')
                {
                    args.Add(ReadNumber(t, ref i));
                }
            }
            if (i < t.Length) i++; // skip ')'
            switch (name)
            {
                case "translate":
                    _cs.Translate(args[0], args.Count > 1 ? args[1] : 0);
                    break;
                case "scale":
                    _cs.Scale(args[0], args.Count > 1 ? args[1] : args[0]);
                    break;
                case "rotate":
                    _cs.Rotate(args[0]);
                    break;
            }
        }
    }

    // ---- shapes ----

    private void RenderRect(XElement el)
    {
        double x = ReadAttr(el, "x", 0);
        double y = ReadAttr(el, "y", 0);
        double w = ReadAttr(el, "width", 0);
        double h = ReadAttr(el, "height", 0);
        _cs.Rectangle(x, y, w, h);
        StyleAndPaint(el);
    }

    private void RenderCircle(XElement el)
    {
        double cx = ReadAttr(el, "cx", 0);
        double cy = ReadAttr(el, "cy", 0);
        double r = ReadAttr(el, "r", 0);
        _cs.Circle(cx, cy, r);
        StyleAndPaint(el);
    }

    private void RenderEllipse(XElement el)
    {
        double cx = ReadAttr(el, "cx", 0);
        double cy = ReadAttr(el, "cy", 0);
        double rx = ReadAttr(el, "rx", 0);
        double ry = ReadAttr(el, "ry", 0);
        _cs.Ellipse(cx, cy, rx, ry);
        StyleAndPaint(el);
    }

    private void RenderLine(XElement el)
    {
        double x1 = ReadAttr(el, "x1", 0), y1 = ReadAttr(el, "y1", 0);
        double x2 = ReadAttr(el, "x2", 0), y2 = ReadAttr(el, "y2", 0);
        _cs.MoveTo(x1, y1);
        _cs.LineTo(x2, y2);
        StrokeOnly(el);
    }

    private void RenderPolygon(XElement el, bool close)
    {
        string points = el.Attribute("points")?.Value ?? "";
        int i = 0;
        bool first = true;
        while (i < points.Length)
        {
            while (i < points.Length && (char.IsWhiteSpace(points[i]) || points[i] == ',')) i++;
            if (i >= points.Length) break;
            double x = ReadNumber(points, ref i);
            while (i < points.Length && (char.IsWhiteSpace(points[i]) || points[i] == ',')) i++;
            double y = ReadNumber(points, ref i);
            if (first) { _cs.MoveTo(x, y); first = false; }
            else _cs.LineTo(x, y);
        }
        if (close) _cs.ClosePath();
        if (close) StyleAndPaint(el);
        else StrokeOnly(el);
    }

    private void RenderPath(XElement el)
    {
        string d = el.Attribute("d")?.Value ?? "";
        double cx = 0, cy = 0;
        double subStartX = 0, subStartY = 0;
        double lastCtrlX = 0, lastCtrlY = 0;
        char lastCmd = 'M';
        int i = 0;
        char op = '\0';
        while (i < d.Length)
        {
            char c = d[i];
            if (char.IsLetter(c)) { op = c; i++; }
            else if (c == ',' || char.IsWhiteSpace(c)) { i++; continue; }
            // If op is missing (continuation of previous command), reuse it.
            if (op == '\0') break;
            bool rel = char.IsLower(op);
            switch (char.ToUpper(op))
            {
                case 'M':
                {
                    double x = ReadNumber(d, ref i);
                    SkipSep(d, ref i);
                    double y = ReadNumber(d, ref i);
                    if (rel) { x += cx; y += cy; }
                    _cs.MoveTo(x, y);
                    cx = x; cy = y; subStartX = x; subStartY = y;
                    op = rel ? 'l' : 'L'; // implicit Lineto for further coords
                    lastCmd = op;
                    break;
                }
                case 'L':
                {
                    double x = ReadNumber(d, ref i);
                    SkipSep(d, ref i);
                    double y = ReadNumber(d, ref i);
                    if (rel) { x += cx; y += cy; }
                    _cs.LineTo(x, y);
                    cx = x; cy = y;
                    lastCmd = op;
                    break;
                }
                case 'H':
                {
                    double x = ReadNumber(d, ref i);
                    if (rel) x += cx;
                    _cs.LineTo(x, cy);
                    cx = x;
                    lastCmd = op;
                    break;
                }
                case 'V':
                {
                    double y = ReadNumber(d, ref i);
                    if (rel) y += cy;
                    _cs.LineTo(cx, y);
                    cy = y;
                    lastCmd = op;
                    break;
                }
                case 'C':
                {
                    double x1 = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y1 = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double x2 = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y2 = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double x = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y = ReadNumber(d, ref i);
                    if (rel) { x1+=cx; y1+=cy; x2+=cx; y2+=cy; x+=cx; y+=cy; }
                    _cs.CurveTo(x1, y1, x2, y2, x, y);
                    lastCtrlX = x2; lastCtrlY = y2;
                    cx = x; cy = y;
                    lastCmd = op;
                    break;
                }
                case 'S':
                {
                    double x2 = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y2 = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double x = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y = ReadNumber(d, ref i);
                    if (rel) { x2+=cx; y2+=cy; x+=cx; y+=cy; }
                    double x1, y1;
                    if (char.ToUpper(lastCmd) is 'C' or 'S')
                    {
                        x1 = 2 * cx - lastCtrlX;
                        y1 = 2 * cy - lastCtrlY;
                    }
                    else { x1 = cx; y1 = cy; }
                    _cs.CurveTo(x1, y1, x2, y2, x, y);
                    lastCtrlX = x2; lastCtrlY = y2;
                    cx = x; cy = y;
                    lastCmd = op;
                    break;
                }
                case 'Q':
                {
                    double qx = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double qy = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double x = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y = ReadNumber(d, ref i);
                    if (rel) { qx+=cx; qy+=cy; x+=cx; y+=cy; }
                    // Convert quadratic to cubic.
                    double x1 = cx + 2.0 / 3.0 * (qx - cx);
                    double y1 = cy + 2.0 / 3.0 * (qy - cy);
                    double x2 = x + 2.0 / 3.0 * (qx - x);
                    double y2 = y + 2.0 / 3.0 * (qy - y);
                    _cs.CurveTo(x1, y1, x2, y2, x, y);
                    lastCtrlX = qx; lastCtrlY = qy;
                    cx = x; cy = y;
                    lastCmd = op;
                    break;
                }
                case 'T':
                {
                    double x = ReadNumber(d, ref i); SkipSep(d, ref i);
                    double y = ReadNumber(d, ref i);
                    if (rel) { x+=cx; y+=cy; }
                    double qx, qy;
                    if (char.ToUpper(lastCmd) is 'Q' or 'T')
                    {
                        qx = 2 * cx - lastCtrlX;
                        qy = 2 * cy - lastCtrlY;
                    }
                    else { qx = cx; qy = cy; }
                    double x1 = cx + 2.0 / 3.0 * (qx - cx);
                    double y1 = cy + 2.0 / 3.0 * (qy - cy);
                    double x2 = x + 2.0 / 3.0 * (qx - x);
                    double y2 = y + 2.0 / 3.0 * (qy - y);
                    _cs.CurveTo(x1, y1, x2, y2, x, y);
                    lastCtrlX = qx; lastCtrlY = qy;
                    cx = x; cy = y;
                    lastCmd = op;
                    break;
                }
                case 'Z':
                {
                    _cs.ClosePath();
                    cx = subStartX; cy = subStartY;
                    lastCmd = op;
                    // no coordinates follow; require a new explicit command
                    op = '\0';
                    break;
                }
                default:
                    // unsupported command (e.g., 'A' arc) — bail out cleanly
                    return;
            }
        }
        StyleAndPaint(el);
    }

    // ---- style / paint ----

    private void StyleAndPaint(XElement el)
    {
        string? fill = ResolveStyle(el, "fill");
        string? stroke = ResolveStyle(el, "stroke");
        double strokeWidth = ParseLength(ResolveStyle(el, "stroke-width")) ?? 1.0;
        bool hasFill = !(fill is null or "none");
        bool hasStroke = !(stroke is null or "none");
        if (!hasFill && !hasStroke)
        {
            // SVG default fill = black if neither is set explicitly.
            hasFill = true;
            fill = "#000000";
        }

        if (hasFill)
        {
            var (r, g, b) = ParseColor(fill!);
            _cs.SetRgbFill(r, g, b);
        }
        if (hasStroke)
        {
            var (r, g, b) = ParseColor(stroke!);
            _cs.SetRgbStroke(r, g, b);
            _cs.SetLineWidth(strokeWidth);
        }

        if (hasFill && hasStroke) _cs.FillStroke();
        else if (hasFill) _cs.Fill();
        else _cs.Stroke();
    }

    private void StrokeOnly(XElement el)
    {
        string? stroke = ResolveStyle(el, "stroke") ?? "#000000";
        double strokeWidth = ParseLength(ResolveStyle(el, "stroke-width")) ?? 1.0;
        var (r, g, b) = ParseColor(stroke);
        _cs.SetRgbStroke(r, g, b);
        _cs.SetLineWidth(strokeWidth);
        _cs.Stroke();
    }

    private static string? ResolveStyle(XElement el, string name)
    {
        var attr = el.Attribute(name)?.Value;
        if (!string.IsNullOrEmpty(attr)) return attr;
        var style = el.Attribute("style")?.Value;
        if (string.IsNullOrEmpty(style)) return null;
        foreach (string pair in style.Split(';'))
        {
            int idx = pair.IndexOf(':');
            if (idx <= 0) continue;
            string k = pair.Substring(0, idx).Trim();
            if (k == name) return pair.Substring(idx + 1).Trim();
        }
        return null;
    }

    private static (double R, double G, double B) ParseColor(string raw)
    {
        string s = raw.Trim();
        if (s.StartsWith("#"))
        {
            if (s.Length == 4) // #RGB
            {
                int r = Convert.ToInt32(new string(s[1], 2), 16);
                int g = Convert.ToInt32(new string(s[2], 2), 16);
                int b = Convert.ToInt32(new string(s[3], 2), 16);
                return (r / 255.0, g / 255.0, b / 255.0);
            }
            if (s.Length == 7)
            {
                int r = Convert.ToInt32(s.Substring(1, 2), 16);
                int g = Convert.ToInt32(s.Substring(3, 2), 16);
                int b = Convert.ToInt32(s.Substring(5, 2), 16);
                return (r / 255.0, g / 255.0, b / 255.0);
            }
        }
        if (s.StartsWith("rgb("))
        {
            string inside = s.Substring(4, s.Length - 5);
            var parts = inside.Split(',');
            return (
                double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture) / 255.0,
                double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) / 255.0,
                double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture) / 255.0);
        }
        return s.ToLowerInvariant() switch
        {
            "black" => (0, 0, 0),
            "white" => (1, 1, 1),
            "red"   => (1, 0, 0),
            "green" => (0, 0.5, 0),
            "blue"  => (0, 0, 1),
            "yellow" => (1, 1, 0),
            "orange" => (1, 0.65, 0),
            "purple" => (0.5, 0, 0.5),
            "gray" or "grey" => (0.5, 0.5, 0.5),
            "lightgray" or "lightgrey" => (0.83, 0.83, 0.83),
            "darkblue" => (0, 0, 0.55),
            "darkred" => (0.55, 0, 0),
            _ => (0, 0, 0),
        };
    }

    private static (double X, double Y, double W, double H) ParseViewBox(XElement root, double defaultW, double defaultH)
    {
        string? vb = root.Attribute("viewBox")?.Value;
        if (!string.IsNullOrEmpty(vb))
        {
            var parts = vb.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return (
                double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                double.Parse(parts[3], CultureInfo.InvariantCulture));
        }
        double w = ParseLength(root.Attribute("width")?.Value) ?? defaultW;
        double h = ParseLength(root.Attribute("height")?.Value) ?? defaultH;
        return (0, 0, w, h);
    }

    private static double ReadAttr(XElement el, string name, double fallback) =>
        ParseLength(el.Attribute(name)?.Value) ?? fallback;

    private static double? ParseLength(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        string s = raw.Trim();
        // strip trailing units (px / pt / mm / cm / in / %) — keep the numeric magnitude
        int i = 0;
        if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '-' || s[i] == '+')) i++;
        if (i == 0) return null;
        return double.Parse(s.AsSpan(0, i), CultureInfo.InvariantCulture);
    }

    private static double ReadNumber(string s, ref int i)
    {
        SkipSep(s, ref i);
        int start = i;
        if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            i++;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && char.IsDigit(s[i])) i++;
        }
        return double.Parse(s.AsSpan(start, i - start), CultureInfo.InvariantCulture);
    }

    private static void SkipSep(string s, ref int i)
    {
        while (i < s.Length && (s[i] == ',' || char.IsWhiteSpace(s[i]))) i++;
    }
}
