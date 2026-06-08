using CSharpPdf.Layout;
using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Objects;

namespace CSharpPdf.Content;

/// <summary>
/// Operations valid inside a PDF text object (between BT and ET). Obtained
/// from <see cref="IPdfCanvas.Text"/> (or <see cref="PdfGraphics.Text"/>),
/// which emits BT on entry; disposing the returned instance emits the
/// matching ET. Nested text objects are not possible because BT inside BT is
/// illegal in PDF and there is no method on this interface to open one.
///
/// Use with <c>using</c>:
/// <code>
/// using var t = canvas.Text();
/// t.SetFont(StandardFont.Helvetica, 12);
/// t.MoveText(50, 700);
/// t.ShowText("Hello");
/// </code>
///
/// Per ISO 32000-1 Table 51, the following are forbidden in this state and
/// therefore absent from this interface:
/// <list type="bullet">
/// <item>q / Q (graphic state save/restore)</item>
/// <item>cm (transformation matrix)</item>
/// <item>w, J, j, M, d, ri, i, gs (general graphic state)</item>
/// <item>Path construction and painting (m, l, c, re, S, f, B, n, W, …)</item>
/// <item>XObject painting (Do)</item>
/// <item>Inline image (BI…EI)</item>
/// </list>
/// Colour, marked content, and the text-state / positioning / showing
/// operators all remain available.
/// </summary>
public interface PdfTextObject : IDisposable
{
    // ===== Text state setters (§9.3) =================================

    void SetFont(Font font, double size);
    void SetCharSpacing(double tc);
    void SetWordSpacing(double tw);
    void SetHorizontalScaling(double percent);
    void SetLeading(double leading);
    void SetTextRise(double rise);
    void SetTextRenderMode(TextRenderMode mode);

    // ===== Text positioning (§9.4.2) =================================

    /// <summary>Tm — set the text matrix and text line matrix.</summary>
    void SetTextMatrix(double a, double b, double c, double d, double e, double f);

    /// <summary>Td — translate the start of the next line by (tx, ty).</summary>
    void MoveText(double tx, double ty);

    /// <summary>TD — translate by (tx, ty) and set the leading to -ty.</summary>
    void MoveTextSetLeading(double tx, double ty);

    /// <summary>T* — move to the start of the next line using the current leading.</summary>
    void NextLine();

    // ===== Text showing (§9.4.3) =====================================

    /// <summary>Tj — show a string.</summary>
    void ShowText(string text);

    /// <summary>' — move to the next line and show a string.</summary>
    void NextLineShowText(string text);

    /// <summary>" — set word and char spacing, move to the next line, and show a string.</summary>
    void NextLineShowText(double wordSpacing, double charSpacing, string text);

    /// <summary>TJ — show one or more strings with individual glyph positioning (kerning).</summary>
    void ShowTextWithKerning(params object[] items);

    // ===== Colour (§8.6) =============================================
    // Colour operators are valid in both page description and text
    // object states, so they appear on both interfaces.

    void SetFillGray(double gray);
    void SetStrokeGray(double gray);
    void SetFillRgb(double r, double g, double b);
    void SetStrokeRgb(double r, double g, double b);
    void SetFillCmyk(double c, double m, double y, double k);
    void SetStrokeCmyk(double c, double m, double y, double k);
    void SetFillColor(Color color);
    void SetStrokeColor(Color color);

    // ===== Marked content (§14.6) ====================================
    // Marked content may nest freely inside a text object and stays at
    // the text-object state, so the body is itself a PdfTextObject.

    /// <summary>BMC…EMC — wrap <paramref name="body"/> in a marked-content sequence tagged <paramref name="tag"/>.</summary>
    void MarkedContent(string tag, Action<PdfTextObject> body);

    /// <summary>BDC…EMC — marked-content sequence with an associated property-list dictionary.</summary>
    void MarkedContent(string tag, PdfDictionary properties, Action<PdfTextObject> body);

    /// <summary>MP — a single-point marker with the given tag.</summary>
    void MarkPoint(string tag);

    /// <summary>DP — a single-point marker with an associated property-list dictionary.</summary>
    void MarkPoint(string tag, PdfDictionary properties);
}
