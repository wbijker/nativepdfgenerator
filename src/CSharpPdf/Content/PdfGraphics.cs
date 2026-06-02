using CSharpPdf.Geometry;
using CSharpPdf.Images;
using CSharpPdf.Layout;
using CSharpPdf.Objects;
using CSharpPdf.Text;

namespace CSharpPdf.Content;

/// <summary>
/// The drawing surface at PDF's page-description state. Every state-mutating
/// operator (colour, line attributes, transforms, text state) lives here and
/// nowhere else — instances of <c>PdfGraphics</c> are obtained from
/// <see cref="IPdfCanvas.Graphics"/>, which emits q on entry; disposing the
/// instance emits the matching Q so every state change is automatically
/// bracketed and cannot leak.
///
/// Use with <c>using</c>:
/// <code>
/// using var g = canvas.Graphics();
/// g.SetFillRgb(1, 0, 0);
/// g.DrawRectangle(0, 0, 50, 50, fill: Colors.Red);
/// </code>
///
/// Includes:
/// <list type="bullet">
/// <item>Graphic state, transforms, colour, ExtGState.</item>
/// <item>Path drawing via build-then-paint callbacks (no orphan paths possible).</item>
/// <item>Shape conveniences that internally save/restore state.</item>
/// <item>Shadings.</item>
/// <item>Text state setters (Tf, Tc, Tw, Tz, TL, Ts, Tr).</item>
/// <item>Atomic <c>DrawText…</c> helpers — each opens its own BT…ET.</item>
/// <item>Nested scopes: <see cref="Graphics"/> (nested q…Q) and <see cref="Text"/> (enter a text object).</item>
/// <item>XObject painting (image / form / generic Do).</item>
/// </list>
///
/// Coordinates are PDF user space (origin bottom-left, Y increases upward);
/// arguments are in points unless noted.
/// </summary>
public interface PdfGraphics : IDisposable
{
    // ===== Nested scopes (imperative — return disposable scopes) =====

    /// <summary>q…Q — open a nested saved graphic state. Dispose emits Q.</summary>
    PdfGraphics Graphics();

    /// <summary>BT…ET — open a text object. Dispose emits ET.</summary>
    PdfTextObject Text();

    // Marked content / structure / optional content / artifact / MarkPoint
    // are intentionally NOT on this interface. They live at the
    // page-description level on IPdfCanvas, where their body is itself an
    // IPdfCanvas (recursive). To wrap drawing in a marked-content sequence,
    // compose canvas.MarkedContent(c => { using var g = c.Graphics(); … }).

    // ===== Graphics state (§8.4) =====================================

    void SetLineWidth(double width);
    void SetLineCap(LineCap cap);
    void SetLineJoin(LineJoin join);
    void SetMiterLimit(double limit);
    void SetDashPattern(double[] pattern, double phase = 0);
    void SetFlatness(double tolerance);
    void SetRenderingIntent(RenderingIntent intent);

    /// <summary>Apply a one-off ExtGState dictionary (registered and invoked via gs).</summary>
    void ApplyExtGState(PdfDictionary gs);

    /// <summary>Non-stroking (fill) alpha 0..1 via an ExtGState (ca key).</summary>
    void SetFillOpacity(double alpha);

    /// <summary>Stroking alpha 0..1 via an ExtGState (CA key).</summary>
    void SetStrokeOpacity(double alpha);

    /// <summary>Current blend mode via an ExtGState (BM key).</summary>
    void SetBlendMode(BlendMode mode);

    // ===== Transforms (§8.3) =========================================

    void Transform(double a, double b, double c, double d, double e, double f);
    void Translate(double tx, double ty);
    void Scale(double sx, double sy);

    /// <summary>Rotate counter-clockwise by <paramref name="degrees"/> around the current origin.</summary>
    void Rotate(double degrees);

    // ===== Colour (§8.6) =============================================

    void SetFillGray(double gray);
    void SetStrokeGray(double gray);
    void SetFillRgb(double r, double g, double b);
    void SetStrokeRgb(double r, double g, double b);
    void SetFillCmyk(double c, double m, double y, double k);
    void SetStrokeCmyk(double c, double m, double y, double k);
    void SetFillColor(Color color);
    void SetStrokeColor(Color color);
    void SetFillColorSpace(string name);
    void SetStrokeColorSpace(string name);
    void SetFillColorN(params double[] components);
    void SetStrokeColorN(params double[] components);

    /// <summary>Set fill to a named pattern previously registered on the page.</summary>
    void SetFillPattern(string patternName);

    /// <summary>Set stroke to a named pattern previously registered on the page.</summary>
    void SetStrokePattern(string patternName);

    // ===== Path drawing (§8.5) =======================================

    /// <summary>Build a path with <paramref name="build"/> and stroke it (S operator).</summary>
    void StrokePath(Action<PdfPath> build);

    /// <summary>Build a path with <paramref name="build"/> and fill it (f / f* operator).</summary>
    void FillPath(Action<PdfPath> build, FillRule rule = FillRule.NonZero);

    /// <summary>Build a path with <paramref name="build"/> and fill + stroke it (B / B* operator).</summary>
    void FillAndStrokePath(Action<PdfPath> build, FillRule rule = FillRule.NonZero);

    /// <summary>Build a path and use it as a clip (W / W* + n) — subsequent drawing is clipped to this region.</summary>
    void ClipPath(Action<PdfPath> build, FillRule rule = FillRule.NonZero);

    /// <summary>Build a path, clip to it, and stroke it (W / W* + S).</summary>
    void ClipAndStrokePath(Action<PdfPath> build, FillRule rule = FillRule.NonZero);

    /// <summary>Build a path, clip to it, and fill it (W / W* + f / f*).</summary>
    void ClipAndFillPath(Action<PdfPath> build, FillRule rule = FillRule.NonZero);

    /// <summary>Build a path, clip to it, and fill + stroke it (W / W* + B / B*).</summary>
    void ClipAndFillAndStrokePath(Action<PdfPath> build, FillRule rule = FillRule.NonZero);

    // ===== Shape conveniences ========================================

    void DrawRectangle(double x, double y, double width, double height,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1);
    void DrawRoundedRectangle(double x, double y, double width, double height, double radius,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1);
    void DrawCircle(double cx, double cy, double radius,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1);
    void DrawEllipse(double cx, double cy, double rx, double ry,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1);
    void DrawLine(double x1, double y1, double x2, double y2,
        Color stroke, double strokeWidth = 1);
    void DrawPolygon(ReadOnlySpan<Point> points,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1);
    void DrawPolyline(ReadOnlySpan<Point> points, Color stroke, double strokeWidth = 1);

    // ===== Shadings (§8.7.4) =========================================

    /// <summary>sh — paint a shading (registered to the page if not already).</summary>
    void PaintShading(PdfReference shading);

    /// <summary>sh — paint an already-named shading.</summary>
    void PaintShading(string registeredName);

    // ===== Text state setters (§9.3) =================================

    /// <summary>Tf — select font, auto-registering it on the page.</summary>
    void SetFont(Font font, double size);

    /// <summary>Tc — character spacing in unscaled text units.</summary>
    void SetCharSpacing(double tc);

    /// <summary>Tw — word spacing in unscaled text units (applies to the space character only).</summary>
    void SetWordSpacing(double tw);

    /// <summary>Tz — horizontal scaling as a percentage (100 = unscaled).</summary>
    void SetHorizontalScaling(double percent);

    /// <summary>TL — text leading: the line-to-line distance used by T*, ', ".</summary>
    void SetLeading(double leading);

    /// <summary>Ts — text rise: vertical offset applied to glyphs (positive = up).</summary>
    void SetTextRise(double rise);

    /// <summary>Tr — text rendering mode (fill / stroke / clip / invisible / combinations).</summary>
    void SetTextRenderMode(TextRenderMode mode);

    // ===== Atomic text drawing =======================================

    /// <summary>Draw one line of text at (x, baselineY). Opens its own BT…ET internally.</summary>
    void DrawText(Font font, double size, double x, double baselineY, string text);

    /// <summary>Draw one line of text centred horizontally on <paramref name="centerX"/>.</summary>
    void DrawTextCentered(Font font, double size, double centerX, double baselineY, string text);

    /// <summary>Draw one line of text right-aligned to <paramref name="rightX"/>.</summary>
    void DrawTextRight(Font font, double size, double rightX, double baselineY, string text);

    /// <summary>
    /// Word-wrap and draw text starting at (x, baselineY), breaking at
    /// <paramref name="maxWidth"/> and advancing by <paramref name="leading"/>.
    /// Returns the baseline Y just below the last line drawn.
    /// </summary>
    double DrawWrappedText(Font font, double size, double x, double baselineY,
        double maxWidth, double leading, string text);

    // ===== XObject painting (§8.9, §8.10) ===========================

    /// <summary>
    /// Draw a <see cref="PdfImage"/> into the box (x, y, w, h) where <c>(x, y)</c>
    /// is the lower-left corner in user space. The image is embedded once on
    /// the document and once on the page's resources; subsequent calls with
    /// the same instance just emit another <c>Do</c>. If
    /// <see cref="PdfImage.PreferInline"/> is set and the payload is below
    /// ~4 KB, the canvas may emit the image inline (BI/ID/EI) instead.
    /// </summary>
    void DrawImage(PdfImage image, double x, double y, double width, double height);

    /// <summary>Draw a form XObject (reusable vector content) at (x, y), no scaling.</summary>
    void DrawForm(FormXObject form, double x, double y);

    /// <summary>Draw a form XObject at (x, y) with uniform scale.</summary>
    void DrawForm(FormXObject form, double x, double y, double scale);

    /// <summary>Draw a form XObject at (x, y) with independent x/y scaling.</summary>
    void DrawForm(FormXObject form, double x, double y, double sx, double sy);

    /// <summary>Do — paint an already-registered XObject by name.</summary>
    void PaintXObject(string name);

    // ===== ReuseComponent painting ==================================

    /// <summary>
    /// Draw a <see cref="ReuseComponent"/> with its lower-left at <c>(x, y)</c>,
    /// no scaling. The component is embedded once on the document; subsequent
    /// calls with the same instance just emit another <c>Do</c>.
    /// </summary>
    void DrawComponent(ReuseComponent component, double x, double y);

    /// <summary>Draw a component at <c>(x, y)</c> with uniform scale.</summary>
    void DrawComponent(ReuseComponent component, double x, double y, double scale);

    /// <summary>Draw a component at <c>(x, y)</c> with independent x/y scaling.</summary>
    void DrawComponent(ReuseComponent component, double x, double y, double sx, double sy);
}
