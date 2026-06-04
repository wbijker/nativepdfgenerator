using System.Text;
using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Text;

namespace PdfSpec.Content;

/// <summary>
/// A text object (ISO 32000-1 §9.4) — buffers the operators valid between
/// <c>BT</c> and <c>ET</c>: text state (Tc, Tw, Tz, TL, Tf, Tr, Ts), text
/// positioning (Td, TD, Tm, T*), text showing (Tj, TJ, ', "), colour
/// (g/rg/k/cs/sc/scn and stroke equivalents), the general graphics state
/// (w, J, j, M, d, ri, i, gs) and marked content (MP, DP, BMC, BDC, EMC).
/// <para>
/// Obtained from <see cref="ContentStream.AddText"/>. Auto-flushes onto the
/// parent stream — wrapped in <c>q BT … ET Q</c> by default — when the
/// parent next opens another child (<c>AddText</c>/<c>Push</c>) or emits a
/// non-text operator, or when the stream is serialised. Call
/// <see cref="NoSaveRestore"/> to flush as <c>BT … ET</c> only — useful
/// when state set inside the block (e.g. a glyph-shaped clip from Tr=7)
/// must persist past it.
/// </para>
/// </summary>
public sealed class Text : PdfContentPart
{
    private readonly ContentStream _cs;
    private bool _saveRestore = true;

    internal Text(ContentStream cs) => _cs = cs;

    internal override void FlushOnto(StringBuilder parentBuffer)
    {
        FlushChild();
        if (Buffer.Length == 0) return;
        if (_saveRestore) parentBuffer.Append("q\nBT\n").Append(Buffer).Append("ET\nQ\n");
        else parentBuffer.Append("BT\n").Append(Buffer).Append("ET\n");
    }

    /// <summary>
    /// Flush as <c>BT … ET</c> only — skip the surrounding <c>q</c>/<c>Q</c>.
    /// Use when state set inside this block needs to leak past it (e.g. a
    /// glyph-shaped clipping path from text rendering mode 7). The caller
    /// is then responsible for any outer save/restore.
    /// </summary>
    public Text NoSaveRestore()
    {
        _saveRestore = false;
        return this;
    }

    /// <summary>Append a raw line of text-block content (escape hatch).</summary>
    public Text Raw(string line)
    {
        EnsureOpen();
        Buffer.Append(line);
        if (!line.EndsWith('\n')) Buffer.Append('\n');
        return this;
    }

    // ===== Text state =========================================================

    public Text SetCharSpacing(double spacing) => Op($"{N(spacing)} Tc");
    public Text SetWordSpacing(double spacing) => Op($"{N(spacing)} Tw");
    public Text SetHorizontalScaling(double percent) => Op($"{N(percent)} Tz");
    public Text SetLeading(double leading) => Op($"{N(leading)} TL");
    public Text SetTextRise(double rise) => Op($"{N(rise)} Ts");
    public Text SetTextRenderMode(TextRenderMode mode) => Op($"{(int)mode} Tr");

    public Text SetFont(string name, double size) =>
        Op($"/{PdfName.Escape(name)} {N(size)} Tf");

    /// <summary>Tf — select a typed font and size; auto-registers the font on the owning page.</summary>
    public Text SetFont(Font font, double size) =>
        SetFont(_cs.RequirePage(nameof(SetFont)).UseFont(font), size);

    // ===== Text positioning ===================================================

    public Text SetTextMatrix(double a, double b, double c, double d, double e, double f) =>
        Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(e)} {N(f)} Tm");

    /// <summary>Tm — replace the text matrix with <paramref name="m"/>. Absolute (not concatenating like cm).</summary>
    public Text SetTextMatrix(PdfMatrix m) => SetTextMatrix(m.A, m.B, m.C, m.D, m.E, m.F);

    public Text MoveText(double tx, double ty) => Op($"{N(tx)} {N(ty)} Td");
    public Text MoveTextSetLeading(double tx, double ty) => Op($"{N(tx)} {N(ty)} TD");
    public Text NextLine() => Op("T*");

    // ===== Text showing =======================================================

    public Text ShowText(string text) => Op($"{Inline(new PdfString(text))} Tj");
    public Text NextLineShowText(string text) => Op($"{Inline(new PdfString(text))} '");
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

    /// <summary>Show <paramref name="text"/> at (<paramref name="x"/>, <paramref name="y"/>) — equivalent to <c>SetTextMatrix(1, 0, 0, 1, x, y).ShowText(text)</c>.</summary>
    public Text Show(double x, double y, string text) =>
        SetTextMatrix(1, 0, 0, 1, x, y).ShowText(text);

    /// <summary>Show <paramref name="text"/> with a custom text matrix <c>[a b c d e f]</c>.</summary>
    public Text Show(double a, double b, double c, double d, double e, double f, string text) =>
        SetTextMatrix(a, b, c, d, e, f).ShowText(text);

    /// <summary>Show <paramref name="text"/> with the text matrix replaced by <paramref name="m"/>.</summary>
    public Text Show(PdfMatrix m, string text) => SetTextMatrix(m).ShowText(text);

    // ===== Colour =============================================================

    public Text SetGrayFill(double gray) => Op($"{N(gray)} g");
    public Text SetGrayStroke(double gray) => Op($"{N(gray)} G");
    public Text SetRgbFill(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} rg");
    public Text SetRgbStroke(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} RG");
    public Text SetCmykFill(double c, double m, double y, double k) => Op($"{N(c)} {N(m)} {N(y)} {N(k)} k");
    public Text SetCmykStroke(double c, double m, double y, double k) => Op($"{N(c)} {N(m)} {N(y)} {N(k)} K");
    /// <summary>Apply <paramref name="color"/> as the non-stroking colour (auto-emits fill alpha if &lt; 1).</summary>
    public Text SetFillColor(PdfColor color)
    {
        if (color.HasAlpha) SetFillOpacity(color.Alpha);
        return color.Space switch
        {
            ColorSpace.Gray => SetGrayFill(color.C1),
            ColorSpace.Cmyk => SetCmykFill(color.C1, color.C2, color.C3, color.C4),
            _ => SetRgbFill(color.C1, color.C2, color.C3),
        };
    }

    /// <summary>Apply <paramref name="color"/> as the stroking colour (auto-emits stroke alpha if &lt; 1).</summary>
    public Text SetStrokeColor(PdfColor color)
    {
        if (color.HasAlpha) SetStrokeOpacity(color.Alpha);
        return color.Space switch
        {
            ColorSpace.Gray => SetGrayStroke(color.C1),
            ColorSpace.Cmyk => SetCmykStroke(color.C1, color.C2, color.C3, color.C4),
            _ => SetRgbStroke(color.C1, color.C2, color.C3),
        };
    }
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

    public Text SetExtGState(string name) => Op($"/{PdfName.Escape(name)} gs");

    /// <summary>gs — apply a typed <see cref="ExtGState"/>, auto-registering it on the owning page.</summary>
    public Text SetExtGState(ExtGState gs) =>
        SetExtGState(_cs.RequirePage(nameof(SetExtGState)).UseExtGState(gs));

    /// <summary>Set non-stroking alpha via an ExtGState (ca key).</summary>
    public Text SetFillOpacity(double alpha) => SetExtGState(ExtGState.ForFillOpacity(alpha));

    /// <summary>Set stroking alpha via an ExtGState (CA key).</summary>
    public Text SetStrokeOpacity(double alpha) => SetExtGState(ExtGState.ForStrokeOpacity(alpha));

    /// <summary>Set current blend mode via an ExtGState (BM key).</summary>
    public Text SetBlendMode(BlendMode mode) => SetExtGState(ExtGState.ForBlendMode(mode));

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
        EnsureOpen();
        Buffer.Append(text).Append('\n');
        return this;
    }

}
