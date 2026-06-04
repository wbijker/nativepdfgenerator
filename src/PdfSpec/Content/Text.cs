using System.Text;
using PdfSpec.Geometry;
using PdfSpec.Objects;
using PdfSpec.Text;

namespace PdfSpec.Content;

/// <summary>
/// A text object (ISO 32000-1 §9.4) — buffers the operators that are valid
/// between <c>BT</c> and <c>ET</c>: text state (Tc, Tw, Tz, TL, Tf, Tr, Ts),
/// text positioning (Td, TD, Tm, T*), text showing (Tj, TJ, ', "), colour
/// (g/rg/k/cs/sc/scn and stroke equivalents), the general graphics state
/// (w, J, j, M, d, ri, i, gs) and marked content (MP, DP, BMC, BDC, EMC).
/// <para>
/// Construction via <see cref="ContentStream.AddText"/>. The text body is
/// built up by calling methods on the instance; it auto-flushes onto the
/// parent stream — wrapped in <c>q BT … ET Q</c> by default — the next time
/// any of these happens: another <c>AddText()</c>, a non-text operator on
/// the parent stream, or <see cref="ContentStream"/> serialization. Call
/// <see cref="NoSaveRestore"/> to flush as <c>BT … ET</c> only when the
/// text-block's gstate (notably a clip from <c>Tr=7</c>) needs to persist
/// past it.
/// </para>
/// </summary>
public sealed class Text
{
    private readonly ContentStream _cs;
    private readonly StringBuilder _sb = new();
    private bool _saveRestore = true;
    private bool _closed;

    internal Text(ContentStream cs) => _cs = cs;

    internal StringBuilder Buffer => _sb;
    internal bool SaveRestoreEnabled => _saveRestore;
    internal void MarkClosed() => _closed = true;

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
    public Text SetTextRenderMode(int mode) => Op($"{mode} Tr");
    public Text SetTextRenderMode(TextRenderMode mode) => SetTextRenderMode((int)mode);

    public Text SetFont(string name, double size) =>
        Op($"/{PdfName.Escape(name)} {N(size)} Tf");

    /// <summary>Tf — select a typed font and size; auto-registers the font on the owning page.</summary>
    public Text SetFont(Font font, double size) =>
        SetFont(_cs.RequirePage(nameof(SetFont)).UseFont(font), size);

    // ===== Text positioning ===================================================

    public Text SetTextMatrix(double a, double b, double c, double d, double e, double f) =>
        Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(e)} {N(f)} Tm");

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

    // ===== Colour =============================================================

    public Text SetGrayFill(double gray) => Op($"{N(gray)} g");
    public Text SetGrayStroke(double gray) => Op($"{N(gray)} G");
    public Text SetRgbFill(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} rg");
    public Text SetRgbStroke(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} RG");
    public Text SetCmykFill(double c, double m, double y, double k) => Op($"{N(c)} {N(m)} {N(y)} {N(k)} k");
    public Text SetCmykStroke(double c, double m, double y, double k) => Op($"{N(c)} {N(m)} {N(y)} {N(k)} K");
    public Text SetFillColor(Color color) => SetRgbFill(color.R, color.G, color.B);
    public Text SetStrokeColor(Color color) => SetRgbStroke(color.R, color.G, color.B);
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
    public Text SetRenderingIntent(string intent) => Op($"/{PdfName.Escape(intent)} ri");
    public Text SetRenderingIntent(RenderingIntent intent) => SetRenderingIntent(RenderingIntentName(intent));

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
    public Text SetBlendMode(BlendMode mode) => SetExtGState(ExtGState.ForBlendMode(BlendModeName(mode)));

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
        if (_closed) throw new InvalidOperationException(
            "Text block has been closed (auto-flushed by a subsequent AddText, a cs operation, or serialization). " +
            "Re-open with cs.AddText() to continue writing text.");
        _sb.Append(text).Append('\n');
        return this;
    }

    private static string N(double value) => ContentStream.N(value);
    private static string Inline(PdfObject obj) => ContentStream.Inline(obj);

    private static string RenderingIntentName(RenderingIntent intent) => intent switch
    {
        RenderingIntent.AbsoluteColorimetric => "AbsoluteColorimetric",
        RenderingIntent.RelativeColorimetric => "RelativeColorimetric",
        RenderingIntent.Saturation => "Saturation",
        _ => "Perceptual",
    };

    private static string BlendModeName(BlendMode mode) => mode switch
    {
        BlendMode.Multiply => "Multiply",
        BlendMode.Screen => "Screen",
        BlendMode.Overlay => "Overlay",
        BlendMode.Darken => "Darken",
        BlendMode.Lighten => "Lighten",
        BlendMode.ColorDodge => "ColorDodge",
        BlendMode.ColorBurn => "ColorBurn",
        BlendMode.HardLight => "HardLight",
        BlendMode.SoftLight => "SoftLight",
        BlendMode.Difference => "Difference",
        BlendMode.Exclusion => "Exclusion",
        _ => "Normal",
    };
}
