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

    // ----- XObjects (images and forms) -----

    /// <summary>Do — paint the named XObject resource (image or form).</summary>
    public ContentStream PaintXObject(string name) => Op($"/{PdfName.Escape(name)} Do");

    /// <summary>
    /// Draw an image XObject into the rectangle (x, y, width, height). Image space
    /// is the unit square, so the CTM is scaled by the target size first.
    /// </summary>
    public ContentStream DrawImage(string name, double x, double y, double width, double height) =>
        Save().Transform(width, 0, 0, height, x, y).PaintXObject(name).Restore();

    // ----- Text -----

    /// <summary>BT — begin a text object (cannot be nested).</summary>
    public ContentStream BeginText() => Op("BT");

    /// <summary>ET — end the current text object.</summary>
    public ContentStream EndText() => Op("ET");

    /// <summary>Tf — select a font resource by name and set its size.</summary>
    public ContentStream SetFont(string name, double size) =>
        Op($"/{PdfName.Escape(name)} {N(size)} Tf");

    /// <summary>Tm — set the text matrix (and text line matrix).</summary>
    public ContentStream SetTextMatrix(double a, double b, double c, double d, double e, double f) =>
        Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(e)} {N(f)} Tm");

    /// <summary>Td — move to the start of the next line, offset by (tx, ty).</summary>
    public ContentStream MoveText(double tx, double ty) => Op($"{N(tx)} {N(ty)} Td");

    /// <summary>TD — like Td, but also set the leading to -ty.</summary>
    public ContentStream MoveTextSetLeading(double tx, double ty) => Op($"{N(tx)} {N(ty)} TD");

    /// <summary>T* — move to the next line using the current leading.</summary>
    public ContentStream NextLine() => Op("T*");

    /// <summary>TL — set the text leading (line spacing).</summary>
    public ContentStream SetLeading(double leading) => Op($"{N(leading)} TL");

    /// <summary>Tc — set character spacing.</summary>
    public ContentStream SetCharSpacing(double spacing) => Op($"{N(spacing)} Tc");

    /// <summary>Tw — set word spacing (added at each space character).</summary>
    public ContentStream SetWordSpacing(double spacing) => Op($"{N(spacing)} Tw");

    /// <summary>Tz — set horizontal scaling, as a percentage (100 = normal).</summary>
    public ContentStream SetHorizontalScaling(double percent) => Op($"{N(percent)} Tz");

    /// <summary>Ts — set text rise (used for super/subscript).</summary>
    public ContentStream SetTextRise(double rise) => Op($"{N(rise)} Ts");

    /// <summary>Tr — set the text rendering mode (0 fill, 1 stroke, 2 fill+stroke, 7 clip, ...).</summary>
    public ContentStream SetTextRenderMode(int mode) => Op($"{mode} Tr");

    /// <summary>Tj — show a text string at the current text position.</summary>
    public ContentStream ShowText(string text) => Op($"{Inline(new PdfString(text))} Tj");

    /// <summary>' — move to the next line and show a text string.</summary>
    public ContentStream NextLineShowText(string text) => Op($"{Inline(new PdfString(text))} '");

    /// <summary>
    /// TJ — show text with manual glyph positioning. Pass interleaved strings and
    /// numeric adjustments (thousandths of a unit, subtracted from the position;
    /// a positive number moves the next glyph left).
    /// </summary>
    public ContentStream ShowTextWithKerning(params object[] items)
    {
        var array = new PdfArray();
        foreach (object item in items)
        {
            array.Add(item switch
            {
                string s => new PdfString(s),
                int i => new PdfNumber((long)i),
                long l => new PdfNumber(l),
                double d => new PdfNumber(d),
                _ => throw new ArgumentException($"Unsupported TJ item type: {item?.GetType()}"),
            });
        }
        return Op($"{Inline(array)} TJ");
    }

    /// <summary>Convenience: draw a single line of text in one call (BT…Tf…Tm…Tj…ET).</summary>
    public ContentStream DrawText(string fontName, double size, double x, double y, string text) =>
        BeginText().SetFont(fontName, size).SetTextMatrix(1, 0, 0, 1, x, y).ShowText(text).EndText();

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
