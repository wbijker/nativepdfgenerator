using System.Globalization;
using System.Text;
using CSharpPdf.Objects;

namespace CSharpPdf.Content;

/// <summary>
/// A fluent builder for a PDF content stream (Chapter 2, "PDF Imaging Model").
/// Emits the page-description operators in the postfix (operands-then-operator)
/// syntax: graphic-state stack, path construction and painting, the three device
/// color spaces, coordinate transforms, clipping, and marked content.
/// </summary>
public sealed class ContentStream
{
    private readonly StringBuilder _sb = new();

    public byte[] ToBytes() => Encoding.Latin1.GetBytes(_sb.ToString());

    /// <summary>Append a raw line of content-stream text (escape hatch).</summary>
    public ContentStream Raw(string line)
    {
        _sb.Append(line);
        if (!line.EndsWith('\n'))
        {
            _sb.Append('\n');
        }
        return this;
    }

    // ----- Graphic state stack -----

    /// <summary>q — push (save) the current graphic state.</summary>
    public ContentStream Save() => Op("q");

    /// <summary>Q — pop (restore) the graphic state.</summary>
    public ContentStream Restore() => Op("Q");

    // ----- Graphic state attributes -----

    /// <summary>w — set the line width.</summary>
    public ContentStream SetLineWidth(double width) => Op($"{N(width)} w");

    /// <summary>J — line cap: 0 butt, 1 round, 2 projecting square.</summary>
    public ContentStream SetLineCap(int cap) => Op($"{cap} J");

    /// <summary>j — line join: 0 miter, 1 round, 2 bevel.</summary>
    public ContentStream SetLineJoin(int join) => Op($"{join} j");

    /// <summary>M — miter limit.</summary>
    public ContentStream SetMiterLimit(double limit) => Op($"{N(limit)} M");

    /// <summary>d — dash pattern (array of on/off lengths) and phase.</summary>
    public ContentStream SetDash(double[] pattern, double phase = 0)
    {
        string array = string.Join(' ', Array.ConvertAll(pattern, N));
        return Op($"[{array}] {N(phase)} d");
    }

    /// <summary>gs — apply a named ExtGState from the resource dictionary.</summary>
    public ContentStream SetExtGState(string name) => Op($"/{PdfName.Escape(name)} gs");

    // ----- Coordinate transforms -----

    /// <summary>cm — concatenate the matrix [a b c d e f] onto the CTM.</summary>
    public ContentStream Transform(double a, double b, double c, double d, double e, double f) =>
        Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(e)} {N(f)} cm");

    public ContentStream Translate(double tx, double ty) => Transform(1, 0, 0, 1, tx, ty);

    public ContentStream Scale(double sx, double sy) => Transform(sx, 0, 0, sy, 0, 0);

    /// <summary>Rotate counter-clockwise about the current origin.</summary>
    public ContentStream Rotate(double degrees)
    {
        double r = degrees * Math.PI / 180.0;
        double cos = Math.Cos(r), sin = Math.Sin(r);
        return Transform(cos, sin, -sin, cos, 0, 0);
    }

    // ----- Color (the three device color spaces) -----

    public ContentStream SetGrayFill(double gray) => Op($"{N(gray)} g");
    public ContentStream SetGrayStroke(double gray) => Op($"{N(gray)} G");

    public ContentStream SetRgbFill(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} rg");
    public ContentStream SetRgbStroke(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} RG");

    public ContentStream SetCmykFill(double c, double m, double y, double k) =>
        Op($"{N(c)} {N(m)} {N(y)} {N(k)} k");
    public ContentStream SetCmykStroke(double c, double m, double y, double k) =>
        Op($"{N(c)} {N(m)} {N(y)} {N(k)} K");

    // ----- Path construction -----

    /// <summary>m — begin a new subpath at (x, y).</summary>
    public ContentStream MoveTo(double x, double y) => Op($"{N(x)} {N(y)} m");

    /// <summary>l — append a straight line to (x, y).</summary>
    public ContentStream LineTo(double x, double y) => Op($"{N(x)} {N(y)} l");

    /// <summary>c — append a cubic Bézier curve with two control points.</summary>
    public ContentStream CurveTo(double x1, double y1, double x2, double y2, double x3, double y3) =>
        Op($"{N(x1)} {N(y1)} {N(x2)} {N(y2)} {N(x3)} {N(y3)} c");

    /// <summary>re — append a complete rectangle subpath.</summary>
    public ContentStream Rectangle(double x, double y, double width, double height) =>
        Op($"{N(x)} {N(y)} {N(width)} {N(height)} re");

    /// <summary>h — close the current subpath back to its start.</summary>
    public ContentStream ClosePath() => Op("h");

    /// <summary>Append a circle subpath, approximated by four Bézier arcs.</summary>
    public ContentStream Circle(double cx, double cy, double r) => Ellipse(cx, cy, r, r);

    /// <summary>Append an ellipse subpath, approximated by four Bézier arcs.</summary>
    public ContentStream Ellipse(double cx, double cy, double rx, double ry)
    {
        const double k = 0.5522847498307936; // (4/3)*(sqrt(2)-1): circle-to-Bézier constant
        double kx = rx * k, ky = ry * k;
        MoveTo(cx + rx, cy);
        CurveTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
        CurveTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
        CurveTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
        CurveTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
        return ClosePath();
    }

    // ----- Path painting -----

    public ContentStream Stroke() => Op("S");
    public ContentStream CloseStroke() => Op("s");
    public ContentStream Fill() => Op("f");
    public ContentStream FillEvenOdd() => Op("f*");
    public ContentStream FillStroke() => Op("B");
    public ContentStream FillStrokeEvenOdd() => Op("B*");
    public ContentStream CloseFillStroke() => Op("b");

    /// <summary>n — end the path without painting (used after a clip).</summary>
    public ContentStream EndPath() => Op("n");

    // ----- Clipping -----

    /// <summary>W — use the current path as a clip (nonzero winding).</summary>
    public ContentStream Clip() => Op("W");

    /// <summary>W* — use the current path as a clip (even-odd rule).</summary>
    public ContentStream ClipEvenOdd() => Op("W*");

    // ----- Marked content -----

    public ContentStream MarkPoint(string tag) => Op($"/{PdfName.Escape(tag)} MP");

    public ContentStream MarkPoint(string tag, PdfDictionary properties) =>
        Op($"/{PdfName.Escape(tag)} {Inline(properties)} DP");

    public ContentStream BeginMarkedContent(string tag) => Op($"/{PdfName.Escape(tag)} BMC");

    public ContentStream BeginMarkedContent(string tag, PdfDictionary properties) =>
        Op($"/{PdfName.Escape(tag)} {Inline(properties)} BDC");

    public ContentStream EndMarkedContent() => Op("EMC");

    // ----- Helpers -----

    private ContentStream Op(string text)
    {
        _sb.Append(text).Append('\n');
        return this;
    }

    // Format a number: invariant culture, integers as integers, no scientific notation.
    private static string N(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string Inline(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }
}
