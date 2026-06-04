using System.Text;
using PdfSpec.Geometry;
using PdfSpec.Images;
using PdfSpec.Objects;
using PdfSpec.Text;

namespace PdfSpec.Content;

/// <summary>
/// A fluent builder for a PDF content stream (ISO 32000-1 §8.2; full operator
/// list in Annex A). Emits the page-description operators in postfix
/// (operands-then-operator) form: path construction and painting (§8.5),
/// colour (§8.6), shadings (§8.7.4.5), XObjects (§8.8/§8.10), marked content
/// (§14.6), plus the device-independent graphics-state operators (§8.4.4).
/// Text (§9.4) lives on the dedicated <see cref="Text"/> child class; obtain
/// one via <see cref="AddText"/>.
///
/// <para>
/// The graphics-state stack (q/Q, §8.4.2) is modelled hierarchically as a
/// tree of <see cref="PdfContentPart"/> children. <see cref="Push"/> opens a
/// nested <c>ContentStream</c> whose content auto-flushes wrapped in
/// <c>q…Q</c>; <see cref="AddText"/> opens a <see cref="Text"/> whose content
/// auto-flushes wrapped in <c>q BT…ET Q</c>. A part may hold one open child
/// at a time — opening another, emitting any operator on this part, or
/// serialising the stream first flushes the open child.
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
public sealed class ContentStream : PdfContentPart
{
    private readonly ContentStream? _parent;
    private readonly PdfPage? _page;

    // Resource dedup tables — populated only on the root of a Push() chain.
    // Nested streams forward to root via the Root accessor.
    private readonly Dictionary<PdfReference, string>? _xobjectNames;
    private readonly Dictionary<FormXObject, string>? _formNames;
    private readonly Dictionary<PdfReference, string>? _shadingNames;
    private int _imgSeq, _formSeq, _shSeq;

    /// <summary>Construct a free-standing root content stream. Typed image/form/component/ExtGState overloads will throw — use the raw-name variants.</summary>
    public ContentStream()
    {
        _xobjectNames = new();
        _formNames = new();
        _shadingNames = new();
    }

    /// <summary>Construct a page-attached root content stream so typed overloads can auto-register resources on the page.</summary>
    internal ContentStream(PdfPage page) : this() => _page = page;

    /// <summary>Construct a nested (Push'd) scope — shares its root's page and resource dedup.</summary>
    private ContentStream(ContentStream parent) => _parent = parent;

    private ContentStream Root => _parent?.Root ?? this;

    internal override void FlushOnto(StringBuilder parentBuffer)
    {
        FlushChild();
        if (Buffer.Length == 0) return;
        parentBuffer.Append("q\n").Append(Buffer).Append("Q\n");
    }

    /// <summary>
    /// Serialise this content stream. Flushes any open child first; an
    /// unbalanced state stack is impossible by construction since every
    /// <see cref="Push"/> scope auto-flushes wrapped in <c>q…Q</c>.
    /// </summary>
    public byte[] ToBytes()
    {
        FlushChild();
        return Encoding.Latin1.GetBytes(Buffer.ToString());
    }

    /// <summary>Append a raw line of content-stream text (escape hatch). Flushes any open child first.</summary>
    public ContentStream Raw(string line)
    {
        EnsureOpen();
        FlushChild();
        Buffer.Append(line);
        if (!line.EndsWith('\n')) Buffer.Append('\n');
        return this;
    }

    // ===== Graphic state scope ================================================

    /// <summary>
    /// Open a nested graphics-state scope. The returned stream buffers its
    /// operators independently and auto-flushes wrapped in <c>q…Q</c> when
    /// this parent next accepts another operator, opens a sibling child, or
    /// is serialised. Equivalent in effect to the old <c>Save()</c>/<c>Restore()</c>
    /// pair, but hierarchical and lexically scoped — the inner state cannot
    /// leak past the scope.
    /// </summary>
    public ContentStream Push()
    {
        EnsureOpen();
        var child = new ContentStream(this);
        OpenChild(child);
        return child;
    }

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

    /// <summary>cm — concatenate <paramref name="m"/> onto the current transformation matrix.</summary>
    public ContentStream Transform(PdfMatrix m) => Transform(m.A, m.B, m.C, m.D, m.E, m.F);

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

    /// <summary>
    /// Apply <paramref name="color"/> as the non-stroking colour, emitting
    /// the matching <c>g</c>/<c>rg</c>/<c>k</c> operator for its mode and
    /// — if <see cref="PdfColor.HasAlpha"/> is true — first a <c>gs</c> for
    /// the fill alpha.
    /// </summary>
    public ContentStream SetFillColor(PdfColor color)
    {
        if (color.HasAlpha) SetFillOpacity(color.Alpha);
        return color.Mode switch
        {
            ColorMode.Gray => SetGrayFill(color.C1),
            ColorMode.Cmyk => SetCmykFill(color.C1, color.C2, color.C3, color.C4),
            _ => SetRgbFill(color.C1, color.C2, color.C3),
        };
    }

    /// <summary>
    /// Apply <paramref name="color"/> as the stroking colour, emitting the
    /// matching <c>G</c>/<c>RG</c>/<c>K</c> operator for its mode and — if
    /// <see cref="PdfColor.HasAlpha"/> is true — first a <c>gs</c> for the
    /// stroke alpha.
    /// </summary>
    public ContentStream SetStrokeColor(PdfColor color)
    {
        if (color.HasAlpha) SetStrokeOpacity(color.Alpha);
        return color.Mode switch
        {
            ColorMode.Gray => SetGrayStroke(color.C1),
            ColorMode.Cmyk => SetCmykStroke(color.C1, color.C2, color.C3, color.C4),
            _ => SetRgbStroke(color.C1, color.C2, color.C3),
        };
    }

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
        var root = Root;
        if (!root._shadingNames!.TryGetValue(shading, out var name))
        {
            name = $"Sh{++root._shSeq}";
            page.Resources.AddShading(name, shading);
            root._shadingNames[shading] = name;
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

    // ===== Shape conveniences (self-contained: own Push scope) ================

    public ContentStream DrawRectangle(double x, double y, double width, double height,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        var scope = Push();
        scope.ApplyFillStroke(fill, stroke, strokeWidth);
        scope.Rectangle(x, y, width, height);
        scope.PaintByStyle(fill, stroke);
        scope.Flush();
        return this;
    }

    public ContentStream DrawRoundedRectangle(double x, double y, double width, double height, double radius,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        var scope = Push();
        scope.ApplyFillStroke(fill, stroke, strokeWidth);
        scope.TraceRoundedRect(x, y, width, height, radius);
        scope.PaintByStyle(fill, stroke);
        scope.Flush();
        return this;
    }

    public ContentStream DrawCircle(double cx, double cy, double radius,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        var scope = Push();
        scope.ApplyFillStroke(fill, stroke, strokeWidth);
        scope.Circle(cx, cy, radius);
        scope.PaintByStyle(fill, stroke);
        scope.Flush();
        return this;
    }

    public ContentStream DrawEllipse(double cx, double cy, double rx, double ry,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (fill is null && stroke is null) return this;
        var scope = Push();
        scope.ApplyFillStroke(fill, stroke, strokeWidth);
        scope.Ellipse(cx, cy, rx, ry);
        scope.PaintByStyle(fill, stroke);
        scope.Flush();
        return this;
    }

    public ContentStream DrawLine(double x1, double y1, double x2, double y2, PdfColor stroke, double strokeWidth = 1)
    {
        var scope = Push();
        scope.SetStrokeColor(stroke);
        scope.SetLineWidth(strokeWidth);
        scope.MoveTo(x1, y1);
        scope.LineTo(x2, y2);
        scope.Stroke();
        scope.Flush();
        return this;
    }

    public ContentStream DrawPolygon(ReadOnlySpan<Point> points,
        PdfColor? fill = null, PdfColor? stroke = null, double strokeWidth = 1)
    {
        if (points.Length == 0 || (fill is null && stroke is null)) return this;
        var scope = Push();
        scope.ApplyFillStroke(fill, stroke, strokeWidth);
        scope.MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) scope.LineTo(points[i].X, points[i].Y);
        scope.ClosePath();
        scope.PaintByStyle(fill, stroke);
        scope.Flush();
        return this;
    }

    public ContentStream DrawPolyline(ReadOnlySpan<Point> points, PdfColor stroke, double strokeWidth = 1)
    {
        if (points.Length == 0) return this;
        var scope = Push();
        scope.SetStrokeColor(stroke);
        scope.SetLineWidth(strokeWidth);
        scope.MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++) scope.LineTo(points[i].X, points[i].Y);
        scope.Stroke();
        scope.Flush();
        return this;
    }

    // ===== XObjects ===========================================================

    public ContentStream PaintXObject(string name) => Op($"/{PdfName.Escape(name)} Do");

    public ContentStream DrawImage(string name, double x, double y, double width, double height)
    {
        var scope = Push();
        scope.Transform(width, 0, 0, height, x, y);
        scope.PaintXObject(name);
        scope.Flush();
        return this;
    }

    /// <summary>Draw a <see cref="PdfImage"/> into the box (x, y, w, h) — embeds once, paints with Do (or inline for small images).</summary>
    public ContentStream DrawImage(PdfImage image, double x, double y, double width, double height)
    {
        var page = RequirePage(nameof(DrawImage));
        if (image.PreferInline && image.EncodedSize < 4096 && image.CanInline)
        {
            var scope = Push();
            scope.Transform(width, 0, 0, height, x, y);
            scope.Raw(image.BuildInlineBody());
            scope.Flush();
            return this;
        }
        var reference = image.EmbedIn(page.Document);
        return DrawImage(UseXObjectByRef(reference), x, y, width, height);
    }

    public ContentStream DrawForm(FormXObject form, double x, double y) => DrawForm(form, x, y, 1, 1);
    public ContentStream DrawForm(FormXObject form, double x, double y, double scale) => DrawForm(form, x, y, scale, scale);
    public ContentStream DrawForm(FormXObject form, double x, double y, double sx, double sy)
    {
        var page = RequirePage(nameof(DrawForm));
        var root = Root;
        if (!root._formNames!.TryGetValue(form, out var name))
        {
            name = $"Fm{++root._formSeq}";
            page.Resources.AddXObject(name, page.Document.AddObject(form.Build()));
            root._formNames[form] = name;
        }
        var scope = Push();
        scope.Transform(sx, 0, 0, sy, x, y);
        scope.PaintXObject(name);
        scope.Flush();
        return this;
    }

    public ContentStream DrawComponent(ReuseComponent component, double x, double y) => DrawComponent(component, x, y, 1, 1);
    public ContentStream DrawComponent(ReuseComponent component, double x, double y, double scale) => DrawComponent(component, x, y, scale, scale);
    public ContentStream DrawComponent(ReuseComponent component, double x, double y, double sx, double sy)
    {
        var page = RequirePage(nameof(DrawComponent));
        var reference = component.EmbedIn(page.Document);
        var name = UseXObjectByRef(reference);
        var scope = Push();
        scope.Transform(sx, 0, 0, sy, x, y);
        scope.PaintXObject(name);
        scope.Flush();
        return this;
    }

    public ContentStream DrawInlineImageRgb(byte[] samples, int pixelWidth, int pixelHeight,
        double x, double y, double width, double height)
    {
        var scope = Push();
        scope.Transform(width, 0, 0, height, x, y);
        scope.Buffer.Append("BI\n")
            .Append($"/W {pixelWidth} /H {pixelHeight} /CS /RGB /BPC 8\n")
            .Append("ID ")
            .Append(Encoding.Latin1.GetString(samples))
            .Append("\nEI\n");
        scope.Flush();
        return this;
    }

    // ===== Text ===============================================================
    // All text operators (BT/ET-only state, positioning, showing) live on
    // the Text class; obtain one via AddText(), build it up, and the next
    // operator on this stream auto-flushes it wrapped in BT/ET (and q/Q).

    /// <summary>
    /// Open a <see cref="Text"/> child — accumulates BT/ET-valid operators
    /// (text state, positioning, showing, colour, gstate, marked content).
    /// Auto-flushes onto this stream — wrapped in <c>q BT … ET Q</c> by
    /// default — when this stream next opens another child, accepts a
    /// non-text operator, or is serialised. To skip the surrounding
    /// save/restore (e.g. for Tr=7 clipping that needs to leak past the
    /// block) call <see cref="Text.NoSaveRestore"/> on the returned text.
    /// </summary>
    public Text AddText()
    {
        EnsureOpen();
        var text = new Text(this);
        OpenChild(text);
        return text;
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

    // ===== Helpers ============================================================

    internal PdfPage RequirePage(string methodName)
    {
        var page = Root._page;
        return page ?? throw new InvalidOperationException(
            $"{methodName} requires a page-attached content stream (PdfPage.Content). " +
            $"Free-standing streams (e.g. FormXObject.Content) must use the raw-name overload after registering the resource themselves.");
    }

    private string UseXObjectByRef(PdfReference image)
    {
        var root = Root;
        var page = RequirePage(nameof(DrawImage));
        if (!root._xobjectNames!.TryGetValue(image, out var name))
        {
            name = $"Img{++root._imgSeq}";
            page.Resources.AddXObject(name, image);
            root._xobjectNames[image] = name;
        }
        return name;
    }

    private ContentStream Op(string text)
    {
        EnsureOpen();
        FlushChild();
        Buffer.Append(text).Append('\n');
        return this;
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
