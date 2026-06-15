using System.Text;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Content;

/// <summary>
/// A buffer holding the operators valid between <c>BT</c> and <c>ET</c>
/// (ISO 32000-1 §9.4): text state, text positioning, text showing, colour,
/// general graphics state and marked content. Each fluent method writes
/// directly to the internal <see cref="StringBuilder"/>.
/// <para>
/// Construct with a <see cref="ContentStream"/> and a required
/// <see cref="Font"/> + size — the leading <c>Tf</c> is emitted on
/// construction so every block has an explicit font. The block
/// auto-wraps in <c>q BT … ET Q</c> by default — pass
/// <c>saveRestore: false</c> to flush as <c>BT … ET</c> only (used when
/// state set inside, e.g. a glyph clip from Tr=7, must persist past the
/// block).
/// </para>
/// </summary>
public sealed class Text
{
    private readonly StringBuilder _sb = new();
    private readonly ContentStream _cs;
    private readonly bool _saveRestore;

    // Tracked so SetTextMatrix can offset the baseline by the font's
    // ascent — the caller's (e, f) names the AABB top-left, not the
    // baseline. Assigned by SetFont in the constructor.
    private Font _currentFont = null!;
    private double _currentFontSize;

    public Text(ContentStream cs, Font font, double size, bool saveRestore = true)
    {
        _cs = cs;
        _saveRestore = saveRestore;
        SetFont(font, size);
    }

    private double CurrentAscent() =>
        _currentFont.GetVerticalMetrics(_currentFontSize).Ascent;

    internal void FlushTo(StringBuilder target)
    {
        if (_sb.Length == 0) return;
        if (_saveRestore) target.Append("q\nBT\n").Append(_sb).Append("ET\nQ\n");
        else target.Append("BT\n").Append(_sb).Append("ET\n");
    }

    /// <summary>
    /// Append this text block onto the parent content stream — wrapped in
    /// <c>q BT … ET Q</c> by default (or <c>BT … ET</c> only when
    /// constructed with <c>saveRestore: false</c>). Terminates the fluent
    /// chain and returns the parent stream so subsequent operators can
    /// follow.
    /// </summary>
    public ContentStream Build()
    {
        _cs.FlushText(this);
        return _cs;
    }

    /// <summary>Append a raw line of text-block content (escape hatch).</summary>
    public Text Raw(string line)
    {
        _sb.Append(line);
        if (!line.EndsWith('\n')) _sb.Append('\n');
        return this;
    }

    // ===== Text state =========================================================

    public Text SetCharSpacing(double spacing) => Op($"{N(spacing)} Tc");
    public Text SetWordSpacing(double spacing) => Op($"{N(spacing)} Tw");
    public Text SetHorizontalScaling(double percent) => Op($"{N(percent)} Tz");
    public Text SetLeading(double leading) => Op($"{N(leading)} TL");
    public Text SetTextRise(double rise) => Op($"{N(rise)} Ts");
    public Text SetTextRenderMode(TextRenderMode mode) => Op($"{(int)mode} Tr");

    /// <summary>Tf — select a font (by resource name) and size.</summary>
    public Text SetFont(string name, double size) =>
        Op($"/{PdfName.Escape(name)} {N(size)} Tf");

    /// <summary>Tf — select a typed font and size; auto-registers it on the owning page or form. Records the metrics so a subsequent <see cref="SetTextMatrix(double, double, double, double, double, double)"/> can offset the baseline by ascent to place the AABB top-left at the caller's (e, f).</summary>
    public Text SetFont(Font font, double size)
    {
        _currentFont = font;
        _currentFontSize = size;
        return SetFont(_cs.FontNameOf(_cs.UseFont(font)), size);
    }

    /// <summary>Tf — select a font by its registered reference (from <see cref="ContentStream.UseFont"/>) and size.</summary>
    public Text SetFont(PdfReference fontRef, double size) =>
        SetFont(_cs.FontNameOf(fontRef), size);

    // ===== Text positioning ===================================================

    /// <summary>
    /// Tm — set absolute text matrix. The caller's (e, f) names the
    /// text's AABB top-left (cap-top, in text-space terms). The emitted
    /// matrix translates by (e − c·ascent, pageHeight − f − d·ascent) so
    /// that text space (0, ascent) — the glyph cap top — maps to the
    /// requested user point. For upright matrices this collapses to
    /// "baseline at user f + ascent".
    /// </summary>
    public Text SetTextMatrix(double a, double b, double c, double d, double e, double f)
    {
        double ascent = CurrentAscent();
        var (pe, pf) = _cs.TranslateXY(e, f);
        return Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(pe - c * ascent)} {N(pf - d * ascent)} Tm");
    }

    public Text SetTextMatrix(PdfMatrix m) => SetTextMatrix(m.A, m.B, m.C, m.D, m.E, m.F);

    /// <summary>
    /// Set the text origin to user (x, y_baseline) — emits a plain
    /// <c>1 0 0 1 X Y Tm</c> with no ascent adjustment. Use this when
    /// the caller already knows the baseline position (e.g. multi-font
    /// line layout that resolves baseline from per-line max ascent
    /// across spans). <see cref="SetTextMatrix(double, double, double, double, double, double)"/>
    /// is the convenience that names the AABB top-left and offsets by
    /// the current font's ascent — not what you want when fonts vary.
    /// </summary>
    public Text SetBaseline(double x, double yBaseline)
    {
        var (px, py) = _cs.TranslateXY(x, yBaseline);
        return Op($"1 0 0 1 {N(px)} {N(py)} Tm");
    }

    /// <summary>Td — move text origin by (tx, ty) in text space (PDF-native delta: positive ty is up).</summary>
    public Text MoveText(double tx, double ty) => Op($"{N(tx)} {N(ty)} Td");
    /// <summary>TD — like Td but also sets <c>TL = -ty</c> (PDF-native delta: positive ty is up).</summary>
    public Text MoveTextSetLeading(double tx, double ty) => Op($"{N(tx)} {N(ty)} TD");
    public Text NextLine() => Op("T*");

    // ===== Text showing =======================================================

    public Text ShowText(string text)
    {
        AppendPdfStringLiteral(_sb, text);
        _sb.Append(" Tj\n");
        return this;
    }

    public Text NextLineShowText(string text)
    {
        AppendPdfStringLiteral(_sb, text);
        _sb.Append(" '\n");
        return this;
    }

    // Inline PDF string-literal escape — equivalent to PdfString.Write into
    // the target buffer, no MemoryStream/byte[]/string round-trip.
    private static void AppendPdfStringLiteral(StringBuilder sb, string value)
    {
        sb.Append('(');
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '(': sb.Append("\\("); break;
                case ')': sb.Append("\\)"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 0x20 || c > 0x7E)
                    {
                        sb.Append('\\').Append(Convert.ToString(c & 0xFF, 8).PadLeft(3, '0'));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append(')');
    }

    public Text NextLineShowText(double wordSpacing, double charSpacing, string text) =>
        Op($"{N(wordSpacing)} {N(charSpacing)} {Inline(new PdfString(text))} \"");

    public Text ShowTextWithKerning(params object[] items)
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

    // ===== Show convenience (combined Tm + Tj) ================================

    /// <summary>
    /// Show <paramref name="text"/> with the baseline at user
    /// (<paramref name="x"/>, <paramref name="y"/>). Equivalent to
    /// <c>SetTextMatrix(1, 0, 0, 1, x, y).ShowText(text)</c>.
    /// </summary>
    public Text Show(double x, double y, string text) =>
        SetTextMatrix(1, 0, 0, 1, x, y).ShowText(text);

    /// <summary>Show <paramref name="text"/> with an explicit text matrix — passed through verbatim.</summary>
    public Text Show(double a, double b, double c, double d, double e, double f, string text) =>
        SetTextMatrix(a, b, c, d, e, f).ShowText(text);

    /// <summary>Show <paramref name="text"/> with an explicit text matrix — passed through verbatim.</summary>
    public Text Show(PdfMatrix m, string text) => SetTextMatrix(m).ShowText(text);

    // ===== Colour =============================================================

    public Text SetGrayFill(double gray) => Op($"{N(gray)} g");
    public Text SetGrayStroke(double gray) => Op($"{N(gray)} G");
    public Text SetGrayFill(PdfColor color) => Op($"{N(color.C1)} g");
    public Text SetGrayStroke(PdfColor color) => Op($"{N(color.C1)} G");
    public Text SetRgbFill(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} rg");
    public Text SetRgbStroke(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} RG");
    public Text SetCmykFill(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} {N(color.C4)} k");
    public Text SetCmykStroke(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} {N(color.C4)} K");

    /// <summary>Apply <paramref name="color"/> as the non-stroking colour. For transparency, set <c>SetFillOpacity</c> on the parent <see cref="ContentStream"/> separately.</summary>
    public Text SetFillColor(PdfColor color) => color.Space switch
    {
        ColorSpace.Gray => SetGrayFill(color),
        ColorSpace.Cmyk => SetCmykFill(color),
        _ => SetRgbFill(color),
    };

    /// <summary>Apply <paramref name="color"/> as the stroking colour. For transparency, set <c>SetStrokeOpacity</c> on the parent <see cref="ContentStream"/> separately.</summary>
    public Text SetStrokeColor(PdfColor color) => color.Space switch
    {
        ColorSpace.Gray => SetGrayStroke(color),
        ColorSpace.Cmyk => SetCmykStroke(color),
        _ => SetRgbStroke(color),
    };

    public Text SetFillColorSpace(string name) => Op($"/{PdfName.Escape(name)} cs");
    public Text SetStrokeColorSpace(string name) => Op($"/{PdfName.Escape(name)} CS");

    public Text SetFillColorN(params double[] components) =>
        Op($"{string.Join(' ', Array.ConvertAll(components, N))} scn");

    public Text SetStrokeColorN(params double[] components) =>
        Op($"{string.Join(' ', Array.ConvertAll(components, N))} SCN");

    // ===== General graphics state (allowed inside BT/ET) ======================

    public Text SetLineWidth(double width) => Op($"{N(width)} w");
    public Text SetLineCap(int cap) => Op($"{cap} J");
    public Text SetLineCap(LineCap cap) => SetLineCap((int)cap);
    public Text SetLineJoin(int join) => Op($"{join} j");
    public Text SetLineJoin(LineJoin join) => SetLineJoin((int)join);
    public Text SetMiterLimit(double limit) => Op($"{N(limit)} M");
    public Text SetFlatness(double flatness) => Op($"{N(flatness)} i");
    public Text SetRenderingIntent(RenderingIntent intent) => Op($"/{intent} ri");

    public Text SetDash(double[] pattern, double phase = 0)
    {
        string array = string.Join(' ', Array.ConvertAll(pattern, N));
        return Op($"[{array}] {N(phase)} d");
    }

    /// <summary>gs — apply an ExtGState by resource name.</summary>
    public Text SetExtGState(string name) => Op($"/{PdfName.Escape(name)} gs");

    /// <summary>gs — apply a typed <see cref="ExtGState"/>, auto-registering it on the owning page or form.</summary>
    public Text SetExtGState(ExtGState gs) =>
        SetExtGState(_cs.ExtGStateNameOf(_cs.UseExtGState(gs)));

    /// <summary>gs — apply a previously-registered ExtGState by its reference (from <see cref="ContentStream.UseExtGState"/>).</summary>
    public Text SetExtGState(PdfReference gsRef) =>
        SetExtGState(_cs.ExtGStateNameOf(gsRef));

    // ===== Marked content (allowed inside BT/ET) ==============================

    public Text MarkPoint(string tag) => Op($"/{PdfName.Escape(tag)} MP");

    public Text MarkPoint(string tag, PdfDictionary properties) =>
        Op($"/{PdfName.Escape(tag)} {Inline(properties)} DP");

    public Text BeginMarkedContent(string tag) => Op($"/{PdfName.Escape(tag)} BMC");

    public Text BeginMarkedContent(string tag, PdfDictionary properties) =>
        Op($"/{PdfName.Escape(tag)} {Inline(properties)} BDC");

    public Text EndMarkedContent() => Op("EMC");

    // ===== Helpers ============================================================

    private Text Op(string text)
    {
        _sb.Append(text).Append('\n');
        return this;
    }

    private static string N(double value) => ContentStream.N(value);
    private static string Inline(PdfObject obj) => ContentStream.Inline(obj);
}
