using System.Globalization;
using System.Text;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Objects;

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
/// Page-attached streams (obtained from <see cref="PdfPage.Content"/>) provide
/// typed overloads — pass a <see cref="Color"/>, <see cref="ExtGState"/>,
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

    // Per-stream resource dedup for typed XObject/shading overloads. On a
    // page-attached stream these double as the per-page dedup tables.
    private readonly Dictionary<PdfReference, string> _xobjectNames = new();
    private readonly Dictionary<FormXObject, string> _formNames = new();
    private readonly Dictionary<PdfReference, string> _shadingNames = new();
    private int _imgSeq, _formSeq, _shSeq;

    /// <summary>Construct a free-standing content stream. Typed image/form/component/ExtGState overloads will throw — use the raw-name variants.</summary>
    public ContentStream() { }

    /// <summary>Construct a page-attached content stream so typed overloads can auto-register resources on the page.</summary>
    internal ContentStream(PdfPage page) => _page = page;

    public byte[] ToBytes()
    {
        FlushOpenText();
        return Encoding.Latin1.GetBytes(_sb.ToString());
    }

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
    public ContentStream SetRenderingIntent(string intent) => Op($"/{PdfName.Escape(intent)} ri");
    public ContentStream SetRenderingIntent(RenderingIntent intent) => SetRenderingIntent(RenderingIntentName(intent));

    public ContentStream SetDash(double[] pattern, double phase = 0)
    {
        string array = string.Join(' ', Array.ConvertAll(pattern, N));
        return Op($"[{array}] {N(phase)} d");
    }

    public ContentStream SetExtGState(string name) => Op($"/{PdfName.Escape(name)} gs");

    /// <summary>gs — apply a typed <see cref="ExtGState"/>, auto-registering it on the owning page.</summary>
    public ContentStream SetExtGState(ExtGState gs)
    {
        var page = RequirePage(nameof(SetExtGState));
        return SetExtGState(page.UseExtGState(gs));
    }

    /// <summary>Set non-stroking alpha via an ExtGState (ca key).</summary>
    public ContentStream SetFillOpacity(double alpha) => SetExtGState(ExtGState.ForFillOpacity(alpha));

    /// <summary>Set stroking alpha via an ExtGState (CA key).</summary>
    public ContentStream SetStrokeOpacity(double alpha) => SetExtGState(ExtGState.ForStrokeOpacity(alpha));

    /// <summary>Set current blend mode via an ExtGState (BM key).</summary>
    public ContentStream SetBlendMode(BlendMode mode) => SetExtGState(ExtGState.ForBlendMode(BlendModeName(mode)));

    // ===== Coordinate transforms ==============================================

    public ContentStream Transform(double a, double b, double c, double d, double e, double f) =>
        Op($"{N(a)} {N(b)} {N(c)} {N(d)} {N(e)} {N(f)} cm");

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

    public ContentStream SetRgbFill(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} rg");
    public ContentStream SetRgbStroke(double r, double g, double b) => Op($"{N(r)} {N(g)} {N(b)} RG");

    public ContentStream SetCmykFill(double c, double m, double y, double k) =>
        Op($"{N(c)} {N(m)} {N(y)} {N(k)} k");
    public ContentStream SetCmykStroke(double c, double m, double y, double k) =>
        Op($"{N(c)} {N(m)} {N(y)} {N(k)} K");

    public ContentStream SetFillColor(Color color) => SetRgbFill(color.R, color.G, color.B);
    public ContentStream SetStrokeColor(Color color) => SetRgbStroke(color.R, color.G, color.B);

    public ContentStream SetFillColorSpace(string name) => Op($"/{PdfName.Escape(name)} cs");
    public ContentStream SetStrokeColorSpace(string name) => Op($"/{PdfName.Escape(name)} CS");

    public ContentStream SetFillColorN(params double[] components) =>
        Op($"{string.Join(' ', System.Array.ConvertAll(components, N))} scn");
    public ContentStream SetStrokeColorN(params double[] components) =>
        Op($"{string.Join(' ', System.Array.ConvertAll(components, N))} SCN");

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

    public ContentStream MoveTo(double x, double y) => Op($"{N(x)} {N(y)} m");
    public ContentStream LineTo(double x, double y) => Op($"{N(x)} {N(y)} l");

    public ContentStream CurveTo(double x1, double y1, double x2, double y2, double x3, double y3) =>
        Op($"{N(x1)} {N(y1)} {N(x2)} {N(y2)} {N(x3)} {N(y3)} c");

    public ContentStream CurveToV(double x2, double y2, double x3, double y3) =>
        Op($"{N(x2)} {N(y2)} {N(x3)} {N(y3)} v");

    public ContentStream CurveToY(double x1, double y1, double x3, double y3) =>
        Op($"{N(x1)} {N(y1)} {N(x3)} {N(y3)} y");

    public ContentStream Rectangle(double x, double y, double width, double height) =>
        Op($"{N(x)} {N(y)} {N(width)} {N(height)} re");

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

    /// <summary>Build a path via <paramref name="build"/> and stroke it (S).</summary>
    public ContentStream StrokePath(Action<ContentStream> build)
    {
        build(this);
        return Stroke();
    }

    /// <summary>Build a path via <paramref name="build"/> and fill it (f / f*).</summary>
    public ContentStream FillPath(Action<ContentStream> build, FillRule rule = FillRule.NonZero)
    {
        build(this);
        return rule == FillRule.EvenOdd ? FillEvenOdd() : Fill();
    }

    /// <summary>Build a path via <paramref name="build"/> and fill + stroke it (B / B*).</summary>
    public ContentStream FillAndStrokePath(Action<ContentStream> build, FillRule rule = FillRule.NonZero)
    {
        build(this);
        return rule == FillRule.EvenOdd ? FillStrokeEvenOdd() : FillStroke();
    }

    /// <summary>Build a path via <paramref name="build"/> and use it as a clip (W / W* + n).</summary>
    public ContentStream ClipPath(Action<ContentStream> build, FillRule rule = FillRule.NonZero)
    {
        build(this);
        if (rule == FillRule.EvenOdd) ClipEvenOdd(); else Clip();
        return EndPath();
    }

    // ===== Shape conveniences (self-contained: own q/Q wrap) ==================

    public ContentStream DrawRectangle(double x, double y, double width, double height,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        Rectangle(x, y, width, height);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawRoundedRectangle(double x, double y, double width, double height, double radius,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        TraceRoundedRect(x, y, width, height, radius);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawCircle(double cx, double cy, double radius,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        Circle(cx, cy, radius);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawEllipse(double cx, double cy, double rx, double ry,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        Save();
        ApplyFillStroke(fill, stroke, strokeWidth);
        Ellipse(cx, cy, rx, ry);
        PaintByStyle(fill, stroke);
        return Restore();
    }

    public ContentStream DrawLine(double x1, double y1, double x2, double y2, Color stroke, double strokeWidth = 1)
    {
        Save();
        SetRgbStroke(stroke.R, stroke.G, stroke.B);
        SetLineWidth(strokeWidth);
        MoveTo(x1, y1);
        LineTo(x2, y2);
        Stroke();
        return Restore();
    }

    public ContentStream DrawPolygon(ReadOnlySpan<Point> points,
        Color? fill = null, Color? stroke = null, double strokeWidth = 1)
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

    public ContentStream DrawPolyline(ReadOnlySpan<Point> points, Color stroke, double strokeWidth = 1)
    {
        if (points.Length == 0) return this;
        Save();
        SetRgbStroke(stroke.R, stroke.G, stroke.B);
        SetLineWidth(strokeWidth);
        MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) LineTo(points[i].X, points[i].Y);
        Stroke();
        return Restore();
    }

    // ===== XObjects ===========================================================

    public ContentStream PaintXObject(string name) => Op($"/{PdfName.Escape(name)} Do");

    public ContentStream DrawImage(string name, double x, double y, double width, double height) =>
        Save().Transform(width, 0, 0, height, x, y).PaintXObject(name).Restore();

    /// <summary>Draw a <see cref="PdfImage"/> into the box (x, y, w, h) — embeds once, paints with Do (or inline for small images).</summary>
    public ContentStream DrawImage(PdfImage image, double x, double y, double width, double height)
    {
        var page = RequirePage(nameof(DrawImage));
        if (image.PreferInline && image.EncodedSize < 4096 && image.CanInline)
        {
            Save().Transform(width, 0, 0, height, x, y).Raw(image.BuildInlineBody());
            return Restore();
        }
        var reference = image.EmbedIn(page.Document);
        return DrawImage(UseXObjectByRef(page, reference), x, y, width, height);
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
        return Save().Transform(sx, 0, 0, sy, x, y).PaintXObject(name).Restore();
    }

    public ContentStream DrawComponent(ReuseComponent component, double x, double y) => DrawComponent(component, x, y, 1, 1);
    public ContentStream DrawComponent(ReuseComponent component, double x, double y, double scale) => DrawComponent(component, x, y, scale, scale);
    public ContentStream DrawComponent(ReuseComponent component, double x, double y, double sx, double sy)
    {
        var page = RequirePage(nameof(DrawComponent));
        var reference = component.EmbedIn(page.Document);
        var name = UseXObjectByRef(page, reference);
        return Save().Transform(sx, 0, 0, sy, x, y).PaintXObject(name).Restore();
    }

    public ContentStream DrawInlineImageRgb(byte[] samples, int pixelWidth, int pixelHeight,
        double x, double y, double width, double height)
    {
        Save().Transform(width, 0, 0, height, x, y);
        _sb.Append("BI\n")
            .Append($"/W {pixelWidth} /H {pixelHeight} /CS /RGB /BPC 8\n")
            .Append("ID ")
            .Append(Encoding.Latin1.GetString(samples))
            .Append("\nEI\n");
        return Restore();
    }

    // ===== Text ===============================================================
    // All text operators (BT/ET-only state, positioning, showing) live on
    // the Text class; obtain one via AddText(), build it up, and call Build.

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

    /// <summary>BMC…EMC — wrap <paramref name="body"/> in a marked-content sequence.</summary>
    public ContentStream MarkedContent(string tag, Action<ContentStream> body)
    {
        BeginMarkedContent(tag);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    /// <summary>BDC…EMC — marked-content sequence with an associated property-list dictionary.</summary>
    public ContentStream MarkedContent(string tag, PdfDictionary properties, Action<ContentStream> body)
    {
        BeginMarkedContent(tag, properties);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    /// <summary>BDC…EMC over an OCG/OCMD registered in the page's Properties.</summary>
    public ContentStream OptionalContent(string registeredPropertyName, Action<ContentStream> body)
    {
        BeginOptionalContent(registeredPropertyName);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    /// <summary>BDC…EMC carrying a structure MCID — links page content to a structure element.</summary>
    public ContentStream StructureContent(string tag, int mcid, Action<ContentStream> body)
    {
        BeginStructureContent(tag, mcid);
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    /// <summary>BMC…EMC under the <c>Artifact</c> tag — content that isn't part of the logical structure.</summary>
    public ContentStream Artifact(Action<ContentStream> body)
    {
        BeginArtifact();
        try { body(this); }
        finally { EndMarkedContent(); }
        return this;
    }

    // ===== Text objects =======================================================

    private Text? _openText;

    /// <summary>
    /// Start a <see cref="Text"/> block — accumulates BT/ET-valid operators
    /// (text state, positioning, showing, colour, gstate, marked content).
    /// The block auto-flushes onto this stream — wrapped in <c>q BT … ET Q</c>
    /// by default — when the next of these happens: another
    /// <see cref="AddText"/>, any other content-stream operator, or
    /// <see cref="ToBytes"/>. To skip the surrounding save/restore (e.g. for
    /// Tr=7 clipping that needs to leak past the block) call
    /// <see cref="Text.NoSaveRestore"/> on the returned text.
    /// </summary>
    public Text AddText()
    {
        FlushOpenText();
        _openText = new Text(this);
        return _openText;
    }

    /// <summary>
    /// Flush any text object started by <see cref="AddText"/> but not yet
    /// emitted. Invoked automatically before every other stream operation
    /// and before serialization; safe to call repeatedly.
    /// </summary>
    private void FlushOpenText()
    {
        if (_openText is null) return;
        var body = _openText.Buffer;
        if (body.Length > 0)
        {
            if (_openText.SaveRestoreEnabled) _sb.Append("q\nBT\n").Append(body).Append("ET\nQ\n");
            else _sb.Append("BT\n").Append(body).Append("ET\n");
        }
        _openText.MarkClosed();
        _openText = null;
    }

    // ===== Helpers ============================================================

    internal PdfPage RequirePage(string methodName) => _page ?? throw new InvalidOperationException(
        $"{methodName} requires a page-attached content stream (PdfPage.Content). " +
        $"Free-standing streams (e.g. FormXObject.Content) must use the raw-name overload after registering the resource themselves.");

    private string UseXObjectByRef(PdfPage page, PdfReference image)
    {
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
        FlushOpenText();
        _sb.Append(text).Append('\n');
        return this;
    }

    internal static string N(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.######", CultureInfo.InvariantCulture);

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

    private void ApplyFillStroke(Color? fill, Color? stroke, double strokeWidth)
    {
        if (fill is { } f) SetRgbFill(f.R, f.G, f.B);
        if (stroke is { } s) { SetRgbStroke(s.R, s.G, s.B); SetLineWidth(strokeWidth); }
    }

    private void PaintByStyle(Color? fill, Color? stroke)
    {
        if (fill is not null && stroke is not null) FillStroke();
        else if (fill is not null) Fill();
        else Stroke();
    }

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

    private static string RenderingIntentName(RenderingIntent intent) => intent switch
    {
        RenderingIntent.AbsoluteColorimetric => "AbsoluteColorimetric",
        RenderingIntent.RelativeColorimetric => "RelativeColorimetric",
        RenderingIntent.Saturation => "Saturation",
        _ => "Perceptual",
    };
}
