using System.Globalization;
using System.Text;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Layout;
using PdfSpec.Objects;
using PdfSpec.Fonts;

namespace PdfSpec.Content;

/// <summary>
/// A fluent builder for a PDF content stream (ISO 32000-1 §8.2; full operator
/// list in Annex A). Emits the page-description operators in postfix
/// (operands-then-operator) form: graphic state (§8.4), path construction and
/// painting (§8.5), colour (§8.6), shadings (§8.7.4.5), XObjects (§8.8/§8.10),
/// and marked content (§14.6). Text (§9.4) lives on the dedicated
/// <see cref="Text"/> class; obtain one via <see cref="AddText"/>.
///
/// <para>
/// State is appended to a single flat byte buffer. Graphic-state save/restore
/// is just the pair of operators <see cref="Save"/> and <see cref="Restore"/>
/// — they emit a <c>q</c>/<c>Q</c> like everything else. <see cref="AddText"/>
/// returns a <see cref="Text"/> with its own buffer that auto-flushes onto
/// this stream the next time any other operator runs (or on
/// <see cref="ToBytes"/>).
/// </para>
///
/// <para>
/// Page-attached streams (obtained from <see cref="PdfPage.Content"/>) provide
/// typed overloads — pass a <see cref="PdfColor"/>, <see cref="ExtGState"/>,
/// <see cref="PdfImage"/>, <see cref="FormXObject"/>, or
/// <see cref="ReuseComponent"/> directly; the stream registers and
/// deduplicates each on the owning page automatically. Free-standing streams
/// (e.g. inside a <see cref="FormXObject.Content"/>) throw on those overloads
/// — call the raw-name variants instead.
/// </para>
/// </summary>
public sealed class ContentStream
{
    private readonly StringBuilder _sb = new();
    private readonly PdfPage? _page;
    private readonly FormXObject? _form;

    // Per-stream resource dedup for typed overloads. On a page-attached
    // stream these double as the per-page dedup tables.
    private readonly Dictionary<PdfReference, string> _xobjectNames = new();
    private readonly Dictionary<FormXObject, string> _formNames = new();
    private readonly Dictionary<PdfReference, string> _shadingNames = new();
    private int _imgSeq, _formSeq, _shSeq;

    private readonly double _width;
    private readonly double _height;
    private readonly ContentStream? _parent;
    private readonly double _parentX;
    private readonly double _parentY;
    private double _layoutCursorY;

    /// <summary>
    /// Every content stream exposes a top-left-origin coordinate system to
    /// the caller (Y down) but emits PDF-native bottom-left-origin operators
    /// (Y up). User → PDF conversion runs through <see cref="TranslateXY"/>
    /// at every coordinate boundary. Streams know their bounding box
    /// (<see cref="Width"/>, <see cref="Height"/>) so a sub-stream created
    /// via <see cref="CreateSubStream"/> can later flush its body back into
    /// the parent with a positioning <c>cm</c>.
    /// </summary>
    public ContentStream(double width, double height)
    {
        _width = width;
        _height = height;
    }

    internal ContentStream(PdfPage page) : this(page.PageWidth, page.PageHeight) => _page = page;

    internal ContentStream(FormXObject form) : this(form.BoundingBoxWidth, form.BoundingBoxHeight) => _form = form;

    private ContentStream(ContentStream parent, double x, double y, double width, double height)
        : this(width, height)
    {
        _parent = parent;
        _parentX = x;
        _parentY = y;
        _page = parent._page;
        _form = parent._form;
    }

    /// <summary>This stream's bounding-box width in user units.</summary>
    public double Width => _width;

    /// <summary>This stream's bounding-box height in user units — also the reference for top-left ↔ bottom-left Y flip.</summary>
    public double Height => _height;

    /// <summary>This stream's bounding box as a <see cref="PdfSize"/> — shorthand for <c>new PdfSize(Width, Height)</c>.</summary>
    public PdfSize Size => new(_width, _height);

    /// <summary>
    /// Map a user-space (top-left, Y-down) point to the stream's PDF-native
    /// (bottom-left, Y-up) point. X passes through; Y becomes
    /// <c>Height − userY</c>. Every coordinate-emitting method routes
    /// through this so the emitted operators stay pure PDF spec.
    /// </summary>
    public (double X, double Y) TranslateXY(double userX, double userY) => (userX, _height - userY);

    public byte[] ToBytes() => Encoding.Latin1.GetBytes(_sb.ToString());

    /// <summary>Append a raw line of content-stream text (escape hatch).</summary>
    public ContentStream Raw(string line)
    {
        _sb.Append(line);
        if (!line.EndsWith('\n')) _sb.Append('\n');
        return this;
    }

    // ===== Graphic state stack =================================================

    public ContentStream Save() => Op("q");
    public ContentStream Restore() => Op("Q");

    // ===== Graphic state attributes ===========================================

    public ContentStream SetLineWidth(double width) => Op($"{N(width)} w");
    public ContentStream SetLineCap(int cap) => Op($"{cap} J");
    public ContentStream SetLineCap(LineCap cap) => SetLineCap((int)cap);
    public ContentStream SetLineJoin(int join) => Op($"{join} j");
    public ContentStream SetLineJoin(LineJoin join) => SetLineJoin((int)join);
    public ContentStream SetMiterLimit(double limit) => Op($"{N(limit)} M");
    public ContentStream SetFlatness(double flatness) => Op($"{N(flatness)} i");
    public ContentStream SetRenderingIntent(RenderingIntent intent) => Op($"/{intent} ri");

    public ContentStream SetDash(double[] pattern, double phase = 0)
    {
        string array = string.Join(' ', Array.ConvertAll(pattern, N));
        return Op($"[{array}] {N(phase)} d");
    }

    public ContentStream SetExtGState(string name) => Op($"/{PdfName.Escape(name)} gs");

    /// <summary>gs — apply a typed <see cref="ExtGState"/>, auto-registering it on the owning page or form.</summary>
    public ContentStream SetExtGState(ExtGState gs)
    {
        var reference = UseExtGState(gs);
        return SetExtGState(ExtGStateNameOf(reference));
    }

    /// <summary>Set non-stroking alpha via an ExtGState (ca key).</summary>
    public ContentStream SetFillOpacity(double alpha) => SetExtGState(ExtGState.ForFillOpacity(alpha));

    /// <summary>Set stroking alpha via an ExtGState (CA key).</summary>
    public ContentStream SetStrokeOpacity(double alpha) => SetExtGState(ExtGState.ForStrokeOpacity(alpha));

    /// <summary>Set current blend mode via an ExtGState (BM key).</summary>
    public ContentStream SetBlendMode(BlendMode mode) => SetExtGState(ExtGState.ForBlendMode(mode));

    // ===== Coordinate transforms ==============================================

    public ContentStream Transform(double a, double b, double c, double d, double e, double f) =>
        Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(e)} {N(f)} cm");

    /// <summary>cm — concatenate <paramref name="m"/> onto the current transformation matrix.</summary>
    public ContentStream Transform(PdfMatrix m) => Transform(m.A, m.B, m.C, m.D, m.E, m.F);

    /// <summary>cm — translate origin by (tx, ty) in PDF-native space (positive ty is up).</summary>
    public ContentStream Translate(double tx, double ty) => Transform(1, 0, 0, 1, tx, ty);
    public ContentStream Scale(double sx, double sy) => Transform(sx, 0, 0, sy, 0, 0);

    public ContentStream Rotate(double degrees)
    {
        double r = degrees * Math.PI / 180.0;
        double cos = Math.Cos(r), sin = Math.Sin(r);
        return Transform(cos, sin, -sin, cos, 0, 0);
    }

    // ===== Colour =============================================================

    public ContentStream SetGrayFill(double gray) => Op($"{N(gray)} g");
    public ContentStream SetGrayStroke(double gray) => Op($"{N(gray)} G");

    public ContentStream SetRgbFill(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} rg");
    public ContentStream SetRgbStroke(PdfColor color) => Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} RG");

    public ContentStream SetCmykFill(PdfColor color) =>
        Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} {N(color.C4)} k");
    public ContentStream SetCmykStroke(PdfColor color) =>
        Op($"{N(color.C1)} {N(color.C2)} {N(color.C3)} {N(color.C4)} K");

    /// <summary>
    /// Apply <paramref name="color"/> as the non-stroking colour, emitting
    /// the matching <c>g</c>/<c>rg</c>/<c>k</c> operator for its mode.
    /// For transparency call <see cref="SetFillOpacity"/> separately.
    /// </summary>
    public ContentStream SetFillColor(PdfColor color) => color.Space switch
    {
        ColorSpace.Gray => SetGrayFill(color.C1),
        ColorSpace.Cmyk => SetCmykFill(color),
        _ => SetRgbFill(color),
    };

    /// <summary>
    /// Apply <paramref name="color"/> as the stroking colour, emitting the
    /// matching <c>G</c>/<c>RG</c>/<c>K</c> operator for its mode.
    /// For transparency call <see cref="SetStrokeOpacity"/> separately.
    /// </summary>
    public ContentStream SetStrokeColor(PdfColor color) => color.Space switch
    {
        ColorSpace.Gray => SetGrayStroke(color.C1),
        ColorSpace.Cmyk => SetCmykStroke(color),
        _ => SetRgbStroke(color),
    };

    public ContentStream SetFillColorSpace(string name) => Op($"/{PdfName.Escape(name)} cs");
    public ContentStream SetStrokeColorSpace(string name) => Op($"/{PdfName.Escape(name)} CS");

    public ContentStream SetFillColorN(params double[] components) =>
        Op($"{string.Join(' ', Array.ConvertAll(components, N))} scn");
    public ContentStream SetStrokeColorN(params double[] components) =>
        Op($"{string.Join(' ', Array.ConvertAll(components, N))} SCN");

    public ContentStream SetFillPattern(string patternName) => Op($"/{PdfName.Escape(patternName)} scn");
    public ContentStream SetStrokePattern(string patternName) => Op($"/{PdfName.Escape(patternName)} SCN");

    // ===== Shadings ===========================================================

    public ContentStream PaintShading(string name) => Op($"/{PdfName.Escape(name)} sh");

    /// <summary>sh — paint a shading, auto-registering it on the owning page.</summary>
    public ContentStream PaintShading(PdfReference shading)
    {
        var page = RequirePage(nameof(PaintShading));
        if (!_shadingNames.TryGetValue(shading, out var name))
        {
            name = $"Sh{++_shSeq}";
            page.Resources.AddShading(name, shading);
            _shadingNames[shading] = name;
        }
        return PaintShading(name);
    }

    // ===== Path construction ==================================================

    public ContentStream MoveTo(double x, double y)
    {
        var (px, py) = TranslateXY(x, y);
        return Op($"{N(px)} {N(py)} m");
    }

    public ContentStream LineTo(double x, double y)
    {
        var (px, py) = TranslateXY(x, y);
        return Op($"{N(px)} {N(py)} l");
    }

    public ContentStream CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        var (p1x, p1y) = TranslateXY(x1, y1);
        var (p2x, p2y) = TranslateXY(x2, y2);
        var (p3x, p3y) = TranslateXY(x3, y3);
        return Op($"{N(p1x)} {N(p1y)} {N(p2x)} {N(p2y)} {N(p3x)} {N(p3y)} c");
    }

    public ContentStream CurveToV(double x2, double y2, double x3, double y3)
    {
        var (p2x, p2y) = TranslateXY(x2, y2);
        var (p3x, p3y) = TranslateXY(x3, y3);
        return Op($"{N(p2x)} {N(p2y)} {N(p3x)} {N(p3y)} v");
    }

    public ContentStream CurveToY(double x1, double y1, double x3, double y3)
    {
        var (p1x, p1y) = TranslateXY(x1, y1);
        var (p3x, p3y) = TranslateXY(x3, y3);
        return Op($"{N(p1x)} {N(p1y)} {N(p3x)} {N(p3y)} y");
    }

    /// <summary>re — rectangle with top-left at user (x, y) in the stream's top-left coords; emitted as PDF bottom-left.</summary>
    public ContentStream Rectangle(double x, double y, double width, double height)
    {
        var (px, py) = TranslateXY(x, y + height);
        return Op($"{N(px)} {N(py)} {N(width)} {N(height)} re");
    }

    public ContentStream ClosePath() => Op("h");

    public ContentStream Circle(double cx, double cy, double r) => Ellipse(cx, cy, r, r);

    public ContentStream Ellipse(double cx, double cy, double rx, double ry)
    {
        const double k = 0.5522847498307936;
        double kx = rx * k, ky = ry * k;
        MoveTo(cx + rx, cy);
        CurveTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
        CurveTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
        CurveTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
        CurveTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
        return ClosePath();
    }

    /// <summary>Trace a rounded-rectangle path (no painting; use within a path block).</summary>
    public ContentStream RoundedRectangle(double x, double y, double width, double height, double radius)
    {
        TraceRoundedRect(x, y, width, height, radius);
        return this;
    }

    public ContentStream Polygon(ReadOnlySpan<Point> points)
    {
        if (points.Length == 0) return this;
        MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) LineTo(points[i].X, points[i].Y);
        return ClosePath();
    }

    public ContentStream Polyline(ReadOnlySpan<Point> points)
    {
        if (points.Length == 0) return this;
        MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) LineTo(points[i].X, points[i].Y);
        return this;
    }

    // ===== Path painting ======================================================

    public ContentStream Stroke() => Op("S");
    public ContentStream CloseStroke() => Op("s");
    public ContentStream Fill() => Op("f");
    public ContentStream FillEvenOdd() => Op("f*");
    public ContentStream FillStroke() => Op("B");
    public ContentStream FillStrokeEvenOdd() => Op("B*");
    public ContentStream CloseFillStroke() => Op("b");
    public ContentStream CloseFillStrokeEvenOdd() => Op("b*");

    public ContentStream EndPath() => Op("n");

    // ===== Clipping ===========================================================

    public ContentStream Clip() => Op("W");
    public ContentStream ClipEvenOdd() => Op("W*");

    // ===== Path build-then-paint callbacks ====================================

    public ContentStream StrokePath(Action<ContentStream> build)
    {
        build(this);
        return Stroke();
    }

    public ContentStream FillPath(Action<ContentStream> build, FillRule rule = FillRule.NonZero)
    {
        build(this);
        return rule == FillRule.EvenOdd ? FillEvenOdd() : Fill();
    }

    public ContentStream FillAndStrokePath(Action<ContentStream> build, FillRule rule = FillRule.NonZero)
    {
        build(this);
        return rule == FillRule.EvenOdd ? FillStrokeEvenOdd() : FillStroke();
    }

    public ContentStream ClipPath(Action<ContentStream> build, FillRule rule = FillRule.NonZero)
    {
        build(this);
        if (rule == FillRule.EvenOdd) ClipEvenOdd(); else Clip();
        return EndPath();
    }

    // ===== Shape conveniences (self-contained: own q/Q wrap) ==================

    public ContentStream DrawRectangle(double x, double y, double width, double height,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        Rectangle(x, y, width, height);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawRoundedRectangle(double x, double y, double width, double height, double radius,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        TraceRoundedRect(x, y, width, height, radius);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawCircle(double cx, double cy, double radius,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        Circle(cx, cy, radius);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawEllipse(double cx, double cy, double rx, double ry,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        Ellipse(cx, cy, rx, ry);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawLine(double x1, double y1, double x2, double y2, PdfColor stroke, double strokeWidth = 1)
    {
        Save();
        SetStrokeColor(stroke);
        SetLineWidth(strokeWidth);
        MoveTo(x1, y1);
        LineTo(x2, y2);
        Stroke();
        return Restore();
    }

    public ContentStream DrawPolygon(ReadOnlySpan<Point> points,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (points.Length == 0 || (fill is null && stroke is null)) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) LineTo(points[i].X, points[i].Y);
        ClosePath();
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawPolyline(ReadOnlySpan<Point> points, PdfColor stroke, double strokeWidth = 1)
    {
        if (points.Length == 0) return this;
        Save();
        SetStrokeColor(stroke);
        SetLineWidth(strokeWidth);
        MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) LineTo(points[i].X, points[i].Y);
        Stroke();
        return Restore();
    }

    // ===== XObjects ===========================================================

    public ContentStream PaintXObject(string name) => Op($"/{PdfName.Escape(name)} Do");

    public ContentStream DrawImage(string name, double x, double y, double width, double height) =>
        Save().Transform(ImageTransform(x, y, width, height)).PaintXObject(name).Restore();

    /// <summary>Draw a <see cref="PdfImage"/> into the box (x, y, w, h) — embeds once, paints with Do (or inline for small images).</summary>
    public ContentStream DrawImage(PdfImage image, double x, double y, double width, double height)
    {
        var page = RequirePage(nameof(DrawImage));
        if (image.PreferInline && image.EncodedSize < 4096 && image.CanInline)
        {
            Save().Transform(ImageTransform(x, y, width, height)).Raw(image.BuildInlineBody());
            return Restore();
        }
        var reference = image.EmbedIn(page.Document);
        return DrawImage(UseXObjectByRef(reference), x, y, width, height);
    }

    public ContentStream DrawForm(FormXObject form, double x, double y) => DrawForm(form, x, y, 1, 1);
    public ContentStream DrawForm(FormXObject form, double x, double y, double scale) => DrawForm(form, x, y, scale, scale);
    public ContentStream DrawForm(FormXObject form, double x, double y, double sx, double sy)
    {
        var page = RequirePage(nameof(DrawForm));
        if (!_formNames.TryGetValue(form, out var name))
        {
            name = $"Fm{++_formSeq}";
            page.Resources.AddXObject(name, page.Document.AddObject(form.Build()));
            _formNames[form] = name;
        }
        return Save().Transform(ImageTransform(x, y, sx, sy)).PaintXObject(name).Restore();
    }

    public ContentStream DrawComponent(ReuseComponent component, double x, double y) => DrawComponent(component, x, y, 1, 1);
    public ContentStream DrawComponent(ReuseComponent component, double x, double y, double scale) => DrawComponent(component, x, y, scale, scale);
    public ContentStream DrawComponent(ReuseComponent component, double x, double y, double sx, double sy)
    {
        var page = RequirePage(nameof(DrawComponent));
        var reference = component.EmbedIn(page.Document);
        var name = UseXObjectByRef(reference);
        return Save().Transform(ImageTransform(x, y, sx, sy)).PaintXObject(name).Restore();
    }

    public ContentStream DrawInlineImageRgb(byte[] samples, int pixelWidth, int pixelHeight,
        double x, double y, double width, double height)
    {
        // Save() goes through Op() which flushes any open text first.
        Save().Transform(ImageTransform(x, y, width, height));
        _sb.Append("BI\n")
            .Append($"/W {pixelWidth} /H {pixelHeight} /CS /RGB /BPC 8\n")
            .Append("ID ")
            .Append(Encoding.Latin1.GetString(samples))
            .Append("\nEI\n");
        return Restore();
    }

    /// <summary>
    /// Build the CTM-concat for an XObject draw with the box's top-left at
    /// user (x, y) and size (w, h). The XObject's natural origin is bottom-left,
    /// so we land it at the PDF bottom-left of the user box —
    /// <c>TranslateXY(x, y + h)</c>.
    /// </summary>
    private PdfMatrix ImageTransform(double x, double y, double w, double h)
    {
        var (px, py) = TranslateXY(x, y + h);
        return new(w, 0, 0, h, px, py);
    }

    // ===== Text ===============================================================
    // All BT/ET-only operators live on the Text class. Construct a Text
    // with `new Text(cs)`, build it up with its fluent API (it has its own
    // StringBuilder and writes operators directly), then hand it to
    // AddText — the body is appended to this stream wrapped in q BT … ET Q
    // (or BT … ET only when Text.NoSaveRestore was called).

    /// <summary>
    /// Start a new <see cref="Text"/> block bound to this content stream.
    /// Build it up fluently and call <see cref="Text.Build"/> to flush —
    /// the buffered body is appended wrapped in <c>q BT … ET Q</c> by
    /// default, or <c>BT … ET</c> only when <paramref name="saveRestore"/>
    /// is <c>false</c>.
    /// </summary>
    public Text AddText(bool saveRestore = true) => new(this, saveRestore);

    /// <summary>Flush a <see cref="Text"/>'s buffered body onto this stream.</summary>
    internal void FlushText(Text text) => text.FlushTo(_sb);

    // ===== Sub-streams ========================================================

    /// <summary>
    /// Create a sub-stream rooted at user (<paramref name="x"/>, <paramref name="y"/>)
    /// in this stream's top-left coords with its own bounding box
    /// (<paramref name="width"/>, <paramref name="height"/>). The sub-stream
    /// operates in its own top-left origin (0, 0) at the box's upper-left.
    /// It shares resource hosting with this stream (fonts, XObjects and
    /// ExtGStates register on the same page or form). Call
    /// <see cref="Build"/> on the sub-stream to flush its buffered body
    /// back into this one, wrapped in <c>q</c> + positioning <c>cm</c> + <c>Q</c>.
    /// </summary>
    public ContentStream CreateSubStream(double x, double y, double width, double height) =>
        new(this, x, y, width, height);

    /// <summary>The Y coordinate (top-left coords) of the next block that <see cref="Render(Element)"/> will place. Advances by each rendered element's <see cref="RenderResult.NextY"/>.</summary>
    public double LayoutCursorY
    {
        get => _layoutCursorY;
        set => _layoutCursorY = value;
    }

    /// <summary>
    /// Lay out and render <paramref name="element"/> into a sub-stream
    /// placed at user (0, <see cref="LayoutCursorY"/>) with size taken
    /// from the element's <see cref="Element.SizeHint"/> (max where set,
    /// otherwise the available width / min height). On return the cursor
    /// is advanced by the element's reported <see cref="RenderResult.NextY"/>
    /// so a subsequent <c>Render</c> places the next element below this
    /// one. If the result carries a <see cref="RenderResult.NextElement"/>
    /// (partial fit), this recurses on it.
    /// </summary>
    public ContentStream Render(Element element)
    {
        var available = new PdfSize(_width, _height - _layoutCursorY);
        var hint = element.SizeHint(available);
        double w = hint.MaxWidth ?? available.Width;
        double h = hint.MaxHeight ?? hint.MinHeight;
        var sub = CreateSubStream(0, _layoutCursorY, w, h);
        var result = element.Render(sub, new PdfSize(w, h));
        sub.Build();
        _layoutCursorY += result.NextY;
        if (result.NextElement is not null) Render(result.NextElement);
        return this;
    }

    /// <summary>
    /// Flush this sub-stream's buffered body into its parent at the position
    /// it was created at — wrapped in <c>q 1 0 0 1 e f cm … Q</c>, where
    /// (e, f) is the parent's PDF point for the sub-stream's bottom-left
    /// corner. Returns the parent so chaining can continue. Calling Build
    /// on a top-level stream (no parent) is a no-op that returns this.
    /// </summary>
    public ContentStream Build()
    {
        if (_parent is null) return this;
        var (cx, cy) = _parent.TranslateXY(_parentX, _parentY + _height);
        _parent._sb.Append("q\n");
        _parent._sb.Append($"1 0 0 1 {N(cx)} {N(cy)} cm\n");
        _parent._sb.Append(_sb);
        _parent._sb.Append("Q\n");
        _sb.Clear();
        return _parent;
    }

    // ===== Marked content =====================================================

    public ContentStream MarkPoint(string tag) => Op($"/{PdfName.Escape(tag)} MP");

    public ContentStream MarkPoint(string tag, PdfDictionary properties) =>
        Op($"/{PdfName.Escape(tag)} {Inline(properties)} DP");

    public ContentStream BeginMarkedContent(string tag) => Op($"/{PdfName.Escape(tag)} BMC");

    public ContentStream BeginMarkedContent(string tag, PdfDictionary properties) =>
        Op($"/{PdfName.Escape(tag)} {Inline(properties)} BDC");

    public ContentStream EndMarkedContent() => Op("EMC");

    public ContentStream BeginOptionalContent(string propertyName) =>
        Op($"/OC /{PdfName.Escape(propertyName)} BDC");

    public ContentStream BeginStructureContent(string tag, int mcid) =>
        Op($"/{PdfName.Escape(tag)} <</MCID {mcid}>> BDC");

    public ContentStream BeginArtifact() => Op("/Artifact BMC");

    public ContentStream MarkedContent(string tag, Action<ContentStream> body)
    {
        BeginMarkedContent(tag);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    public ContentStream MarkedContent(string tag, PdfDictionary properties, Action<ContentStream> body)
    {
        BeginMarkedContent(tag, properties);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    public ContentStream OptionalContent(string registeredPropertyName, Action<ContentStream> body)
    {
        BeginOptionalContent(registeredPropertyName);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    public ContentStream StructureContent(string tag, int mcid, Action<ContentStream> body)
    {
        BeginStructureContent(tag, mcid);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    public ContentStream Artifact(Action<ContentStream> body)
    {
        BeginArtifact();
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    // ===== Helpers ============================================================

    /// <summary>Ascent of the document-level default font at its current size, or 0 if no default font is set. Used by <see cref="Text"/> to position glyph AABB top-left when no per-block font is selected.</summary>
    internal double DefaultFontAscent()
    {
        var doc = _page?.Document ?? (_form is { } form ? form.Document : null);
        if (doc?.DefaultFont is { } font)
            return font.GetVerticalMetrics(doc.DefaultFontSize).Ascent;
        return 0;
    }

    internal PdfPage RequirePage(string methodName) => _page ?? throw new InvalidOperationException(
        $"{methodName} requires a page-attached content stream (PdfPage.Content). " +
        $"Free-standing streams (e.g. FormXObject.Content) must use the raw-name overload after registering the resource themselves.");

    /// <summary>Register <paramref name="font"/> on the owning page or form and return the indirect reference to its <c>/Font</c> dictionary.</summary>
    public PdfReference UseFont(PdfSpec.Fonts.Font font)
    {
        if (_page is not null) return _page.UseFont(font);
        if (_form is not null) return _form.UseFont(font);
        throw NotAttached(nameof(UseFont));
    }

    /// <summary>Per-host resource name for a font reference returned by <see cref="UseFont"/> — used as the <c>Tf</c> argument.</summary>
    public string FontNameOf(PdfReference fontRef)
    {
        if (_page is not null) return _page.FontNameOf(fontRef);
        if (_form is not null) return _form.FontNameOf(fontRef);
        throw NotAttached(nameof(FontNameOf));
    }

    /// <summary>Register <paramref name="gs"/> on the owning page or form (deduplicating by instance) and return the indirect reference.</summary>
    public PdfReference UseExtGState(ExtGState gs)
    {
        if (_page is not null) return _page.UseExtGState(gs);
        if (_form is not null) return _form.UseExtGState(gs);
        throw NotAttached(nameof(UseExtGState));
    }

    /// <summary>Per-host resource name for an ExtGState reference returned by <see cref="UseExtGState"/> — used as the <c>gs</c> argument.</summary>
    public string ExtGStateNameOf(PdfReference gsRef)
    {
        if (_page is not null) return _page.ExtGStateNameOf(gsRef);
        if (_form is not null) return _form.ExtGStateNameOf(gsRef);
        throw NotAttached(nameof(ExtGStateNameOf));
    }

    private InvalidOperationException NotAttached(string methodName) => new(
        $"{methodName} requires a page- or form-attached content stream. " +
        $"Free-standing streams cannot register resources — use the raw-name overload after registering elsewhere.");

    private string UseXObjectByRef(PdfReference image)
    {
        var page = RequirePage(nameof(DrawImage));
        if (!_xobjectNames.TryGetValue(image, out var name))
        {
            name = $"Img{++_imgSeq}";
            page.Resources.AddXObject(name, image);
            _xobjectNames[image] = name;
        }
        return name;
    }

    private ContentStream Op(string text)
    {
        _sb.Append(text).Append('\n');
        return this;
    }

    internal static string N(double value)
    {
        if (double.IsInfinity(value) || double.IsNaN(value))
            return value.ToString(CultureInfo.InvariantCulture);
        double rounded = Math.Round(value, 3, MidpointRounding.AwayFromZero);
        return rounded == Math.Floor(rounded)
            ? ((long)rounded).ToString(CultureInfo.InvariantCulture)
            : rounded.ToString("0.###", CultureInfo.InvariantCulture);
    }

    internal static string Inline(PdfObject obj)
    {
        using var ms = new MemoryStream();
        obj.Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }

    private void TraceRoundedRect(double x, double y, double width, double height, double radius)
    {
        double r = Math.Min(radius, Math.Min(width, height) / 2);
        if (r <= 0) { Rectangle(x, y, width, height); return; }
        const double K = 0.5522847498307936;
        double c = r * K;
        double right = x + width, top = y + height;
        MoveTo(x + r, y);
        LineTo(right - r, y);
        CurveTo(right - r + c, y, right, y + r - c, right, y + r);
        LineTo(right, top - r);
        CurveTo(right, top - r + c, right - r + c, top, right - r, top);
        LineTo(x + r, top);
        CurveTo(x + r - c, top, x, top - r + c, x, top - r);
        LineTo(x, y + r);
        CurveTo(x, y + r - c, x + r - c, y, x + r, y);
        ClosePath();
    }

    private void ApplyFillStroke(PdfColor? fill, PdfColor? stroke, double strokeWidth)
    {
        if (fill is { } f) SetFillColor(f);
        if (stroke is { } s) { SetStrokeColor(s); SetLineWidth(strokeWidth); }
    }

    private void PaintByStyle(PdfColor? fill, PdfColor? stroke)
    {
        if (fill is not null && stroke is not null) FillStroke();
        else if (fill is not null) Fill();
        else Stroke();
    }
}
