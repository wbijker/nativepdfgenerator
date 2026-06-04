using System.Text;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Content;

/// <summary>
/// A passive buffer holding the operators valid between <c>BT</c> and
/// <c>ET</c> (ISO 32000-1 §9.4): text state (Tc, Tw, Tz, TL, Tf, Tr, Ts),
/// text positioning (Td, TD, Tm, T*), text showing (Tj, TJ, ', "), colour
/// (g/rg/k/cs/sc/scn and stroke equivalents), the general graphics state
/// (w, J, j, M, d, ri, i, gs) and marked content (MP, DP, BMC, BDC, EMC).
/// Each fluent method writes its operator directly to the internal
/// <see cref="StringBuilder"/>.
/// <para>
/// Decoupled from any <see cref="ContentStream"/> — typed font and
/// ExtGState resource resolution happens outside (e.g. via
/// <see cref="ContentStream.UseFont"/>). Hand a built <c>Text</c> to
/// <see cref="ContentStream.AddText"/>, which appends the body framed by
/// <c>BT … ET</c>. The block is <i>not</i> wrapped in <c>q</c>/<c>Q</c>:
/// text state and colour changes inside leak past it, exactly like every
/// other operator on the stream. Wrap explicitly with
/// <see cref="ContentStream.Save"/> / <see cref="ContentStream.Restore"/>
/// if you want hermetic isolation.
/// </para>
/// </summary>
public sealed class Text
{
    private readonly StringBuilder _sb = new();

    public Text()
    {
    }

    internal void FlushTo(StringBuilder target)
    {
        if (_sb.Length == 0) return;
        target.Append("BT\n").Append(_sb).Append("ET\n");
    }

    // ===== Text state =========================================================

    public Text SetCharSpacing(double spacing) => Op($"{N(spacing)} Tc");
    public Text SetWordSpacing(double spacing) => Op($"{N(spacing)} Tw");
    public Text SetHorizontalScaling(double percent) => Op($"{N(percent)} Tz");
    public Text SetLeading(double leading) => Op($"{N(leading)} TL");
    public Text SetTextRise(double rise) => Op($"{N(rise)} Ts");
    public Text SetTextRenderMode(TextRenderMode mode) => Op($"{(int)mode} Tr");

    /// <summary>Tf — select a font (by resource name) and size. Get the name from <see cref="ContentStream.UseFont"/>.</summary>
    public Text SetFont(string name, double size) =>
        Op($"/{PdfName.Escape(name)} {N(size)} Tf");

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

    public Text Show(double x, double y, string text) =>
        SetTextMatrix(1, 0, 0, 1, x, y).ShowText(text);

    public Text Show(double a, double b, double c, double d, double e, double f, string text) =>
        SetTextMatrix(a, b, c, d, e, f).ShowText(text);

    public Text Show(PdfMatrix m, string text) => SetTextMatrix(m).ShowText(text);

    // ===== Colour =============================================================

    public Text SetGrayFill(double gray) => Op($"{N(gray)} g");
    public Text SetGrayStroke(double gray) => Op($"{N(gray)} G");
    public Text SetRgbFill(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} rg");
    public Text SetRgbStroke(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} RG");
    public Text SetCmykFill(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} {N(color.C4)} k");
    public Text SetCmykStroke(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} {N(color.C4)} K");

    /// <summary>
    /// Apply <paramref name="color"/> as the non-stroking colour. Note: alpha
    /// is ignored — set it on the parent <see cref="ContentStream"/> via
    /// <see cref="ContentStream.SetFillOpacity"/> before <c>AddText</c>.
    /// </summary>
    public Text SetFillColor(PdfColor color) => color.Space switch
    {
        ColorSpace.Gray => SetGrayFill(color.C1),
        ColorSpace.Cmyk => SetCmykFill(color),
        _ => SetRgbFill(color),
    };

    /// <summary>
    /// Apply <paramref name="color"/> as the stroking colour. Note: alpha is
    /// ignored — set it on the parent <see cref="ContentStream"/> via
    /// <see cref="ContentStream.SetStrokeOpacity"/> before <c>AddText</c>.
    /// </summary>
    public Text SetStrokeColor(PdfColor color) => color.Space switch
    {
        ColorSpace.Gray => SetGrayStroke(color.C1),
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

    /// <summary>gs — apply an ExtGState by resource name. Get the name from <see cref="ContentStream.UseExtGState"/>.</summary>
    public Text SetExtGState(string name) => Op($"/{PdfName.Escape(name)} gs");

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