using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// The single, universal layout element. Every element carries the full
/// box model — optional explicit <see cref="Width(double)"/> /
/// <see cref="Height(double)"/>, four-sided padding, per-side borders,
/// corner radii, a background fill, and horizontal / vertical alignment —
/// plus an optional single child (<see cref="Content(Element)"/>). A bare
/// <c>new Element()</c> is a styled-chrome container around its
/// <see cref="Content(Element)"/>; specialised elements (text, stacks,
/// columns, canvas, …) subclass this and override <see cref="Draw"/> to
/// supply their own inner drawing while inheriting the same chrome.
///
/// <para>
/// <b>Render pipeline.</b> <see cref="Render"/> is the single entry point:
/// it runs the box pipeline when any chrome is present (<see cref="HasBox"/>)
/// — sizing the outer box, rendering the inner content into a deferred
/// sub-stream via <see cref="Draw"/>, then painting background + borders —
/// or, when the element carries no chrome, calls <see cref="Draw"/> directly
/// on the stream (a zero-overhead fast path that keeps coordinate-sensitive
/// leaves such as <see cref="DeferredComponent"/> drawing straight onto the
/// page). Either way it fires <see cref="OnRendered(Action{RenderedData})"/>
/// against the rectangle the element actually occupied.
/// </para>
/// </summary>
public class Element
{
    // ===== OnRendered hook ===================================================

    /// <summary>
    /// Backing field for the post-render hook (see
    /// <see cref="OnRendered(Action{RenderedData})"/>). Fires once per
    /// <see cref="Render"/>, just after the element is drawn, with the page
    /// it landed on, that page's 1-based number, and its bounding rectangle
    /// in PDF user coords. <c>null</c> by default → zero cost when unused;
    /// the firing path also early-outs when the stream isn't attached to a
    /// page (e.g. inside a Form XObject body).
    /// </summary>
    protected internal Action<RenderedData>? _onRendered;

    // ===== Box-model state ===================================================

    /// <summary>Outer width as a <see cref="Length"/>; <c>null</c> = full available width. Set via <see cref="Width(double)"/>.</summary>
    protected internal Length? _width;

    /// <summary>Outer height as a <see cref="Length"/>; <c>null</c> = shrink to content + chrome. Set via <see cref="Height(double)"/>.</summary>
    protected internal Length? _height;

    /// <summary>Resolve <see cref="_width"/> to points against <paramref name="availableWidth"/>; <c>null</c> when unset.</summary>
    public double? ResolveWidth(double availableWidth) =>
        _width is { } w ? w.ToPoints(availableWidth) : null;

    /// <summary>Resolve <see cref="_height"/> to points against <paramref name="availableHeight"/>; <c>null</c> when unset.</summary>
    public double? ResolveHeight(double availableHeight) =>
        _height is { } h ? h.ToPoints(availableHeight) : null;

    protected internal double _paddingTop;
    protected internal double _paddingRight;
    protected internal double _paddingBottom;
    protected internal double _paddingLeft;

    protected internal PdfColor? _background;

    /// <summary>
    /// Render-time flag — when true, the box pipeline forces the outer
    /// height to <c>available.Height</c> even with no explicit
    /// <see cref="_height"/>. Used by slot-allocating parents
    /// (<see cref="VFrame"/>) to make a chrome-only band fill its slot.
    /// Set transiently around the <see cref="Render"/> call.
    /// </summary>
    protected internal bool _fillSlotHeight;

    /// <summary>Where content sits horizontally inside the inner area when narrower than it. Slack distributes 0 / slack/2 / slack for Left / Center / Right.</summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>Where content sits vertically inside the inner area when <see cref="Height(double)"/> is explicit.</summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    public double BorderTopWidth { get; set; }
    public double BorderRightWidth { get; set; }
    public double BorderBottomWidth { get; set; }
    public double BorderLeftWidth { get; set; }
    public PdfColor? BorderTopColor { get; set; }
    public PdfColor? BorderRightColor { get; set; }
    public PdfColor? BorderBottomColor { get; set; }
    public PdfColor? BorderLeftColor { get; set; }

    /// <summary>Per-corner circular border radii (points). 0 = square corner; any non-zero value selects the rounded paint path.</summary>
    public double BorderRadiusTopLeft { get; set; }
    public double BorderRadiusTopRight { get; set; }
    public double BorderRadiusBottomRight { get; set; }
    public double BorderRadiusBottomLeft { get; set; }

    /// <summary>True if any corner has a non-zero radius — selects the rounded paint path.</summary>
    public bool HasRoundedCorners =>
        BorderRadiusTopLeft > 0 || BorderRadiusTopRight > 0 ||
        BorderRadiusBottomRight > 0 || BorderRadiusBottomLeft > 0;

    public double HorizontalChrome => _paddingLeft + _paddingRight + BorderLeftWidth + BorderRightWidth;
    public double VerticalChrome => _paddingTop + _paddingBottom + BorderTopWidth + BorderBottomWidth;

    /// <summary>The wrapped child, when this element is used as a styled container. <c>null</c> for leaf / multi-child subclasses.</summary>
    private Element? _content;

    /// <summary>
    /// Copy all chrome state — sizing, padding, background, borders,
    /// alignment — onto <paramref name="other"/>. Used by breakable
    /// containers (<see cref="VStack"/>, <see cref="MultiColumn"/>,
    /// <see cref="VFrame"/>) to hand their continuation a clone of the
    /// outer box's chrome so the partial render paints the same border on
    /// every page.
    /// </summary>
    protected internal void CopyChromeTo(Element other)
    {
        other._width = _width;
        other._height = _height;
        other._paddingTop = _paddingTop;
        other._paddingRight = _paddingRight;
        other._paddingBottom = _paddingBottom;
        other._paddingLeft = _paddingLeft;
        other._background = _background;
        other.BorderTopWidth = BorderTopWidth;
        other.BorderRightWidth = BorderRightWidth;
        other.BorderBottomWidth = BorderBottomWidth;
        other.BorderLeftWidth = BorderLeftWidth;
        other.BorderTopColor = BorderTopColor;
        other.BorderRightColor = BorderRightColor;
        other.BorderBottomColor = BorderBottomColor;
        other.BorderLeftColor = BorderLeftColor;
        other.BorderRadiusTopLeft = BorderRadiusTopLeft;
        other.BorderRadiusTopRight = BorderRadiusTopRight;
        other.BorderRadiusBottomRight = BorderRadiusBottomRight;
        other.BorderRadiusBottomLeft = BorderRadiusBottomLeft;
        other.HorizontalAlignment = HorizontalAlignment;
        other.VerticalAlignment = VerticalAlignment;
    }

    // ===== Chrome setters (fluent) ===========================================

    /// <summary>Uniform padding on every side.</summary>
    public Element Padding(double all)
    {
        _paddingTop = _paddingRight = _paddingBottom = _paddingLeft = all;
        return this;
    }

    /// <summary>Vertical + horizontal padding pair.</summary>
    public Element Padding(double vertical, double horizontal)
    {
        _paddingTop = _paddingBottom = vertical;
        _paddingLeft = _paddingRight = horizontal;
        return this;
    }

    public Element PaddingTop(double value)    { _paddingTop = value;    return this; }
    public Element PaddingRight(double value)  { _paddingRight = value;  return this; }
    public Element PaddingBottom(double value) { _paddingBottom = value; return this; }
    public Element PaddingLeft(double value)   { _paddingLeft = value;   return this; }

    /// <summary>Uniform padding on every side, in the given unit.</summary>
    public Element Padding(double value, Unit unit)
    {
        double pt = new Length(value, unit).ToPoints();
        _paddingTop = _paddingRight = _paddingBottom = _paddingLeft = pt;
        return this;
    }

    /// <summary>Vertical + horizontal padding pair, in the given unit.</summary>
    public Element Padding(double vertical, double horizontal, Unit unit)
    {
        _paddingTop = _paddingBottom = new Length(vertical, unit).ToPoints();
        _paddingLeft = _paddingRight = new Length(horizontal, unit).ToPoints();
        return this;
    }

    public Element PaddingTop(double value, Unit unit)    { _paddingTop    = new Length(value, unit).ToPoints(); return this; }
    public Element PaddingRight(double value, Unit unit)  { _paddingRight  = new Length(value, unit).ToPoints(); return this; }
    public Element PaddingBottom(double value, Unit unit) { _paddingBottom = new Length(value, unit).ToPoints(); return this; }
    public Element PaddingLeft(double value, Unit unit)   { _paddingLeft   = new Length(value, unit).ToPoints(); return this; }

    /// <summary>Uniform border on every side.</summary>
    public Element Border(double width, PdfColor color)
    {
        BorderTopWidth = BorderRightWidth = BorderBottomWidth = BorderLeftWidth = width;
        BorderTopColor = BorderRightColor = BorderBottomColor = BorderLeftColor = color;
        return this;
    }

    public Element BorderTop(double width, PdfColor color)
        { BorderTopWidth = width;    BorderTopColor = color;    return this; }
    public Element BorderRight(double width, PdfColor color)
        { BorderRightWidth = width;  BorderRightColor = color;  return this; }
    public Element BorderBottom(double width, PdfColor color)
        { BorderBottomWidth = width; BorderBottomColor = color; return this; }
    public Element BorderLeft(double width, PdfColor color)
        { BorderLeftWidth = width;   BorderLeftColor = color;   return this; }

    /// <summary>Round all four corners uniformly to <paramref name="radius"/> pt.</summary>
    public Element Rounded(double radius)
    {
        BorderRadiusTopLeft = BorderRadiusTopRight = BorderRadiusBottomRight = BorderRadiusBottomLeft = radius;
        return this;
    }

    public Element RoundedTop(double radius)    { BorderRadiusTopLeft = BorderRadiusTopRight = radius;       return this; }
    public Element RoundedBottom(double radius) { BorderRadiusBottomLeft = BorderRadiusBottomRight = radius; return this; }
    public Element RoundedLeft(double radius)   { BorderRadiusTopLeft = BorderRadiusBottomLeft = radius;     return this; }
    public Element RoundedRight(double radius)  { BorderRadiusTopRight = BorderRadiusBottomRight = radius;   return this; }
    public Element RoundedX(double radius)      { BorderRadiusTopLeft = BorderRadiusBottomRight = radius;    return this; }
    public Element RoundedY(double radius)      { BorderRadiusTopRight = BorderRadiusBottomLeft = radius;    return this; }

    public Element Background(PdfColor color) { _background = color; return this; }

    public Element Width(double points) { _width = new Length(points, Unit.Pt); return this; }
    public Element Width(double value, Unit unit) { _width = new Length(value, unit); return this; }
    public Element Height(double points) { _height = new Length(points, Unit.Pt); return this; }
    public Element Height(double value, Unit unit) { _height = new Length(value, unit); return this; }

    public Element HAlign(HorizontalAlignment alignment) { HorizontalAlignment = alignment; return this; }
    public Element VAlign(VerticalAlignment alignment)   { VerticalAlignment = alignment;   return this; }

    public Element AlignLeft()   { HorizontalAlignment = HorizontalAlignment.Left;   return this; }
    public Element AlignCenter() { HorizontalAlignment = HorizontalAlignment.Center; return this; }
    public Element AlignRight()  { HorizontalAlignment = HorizontalAlignment.Right;  return this; }

    public Element AlignTop()    { VerticalAlignment = VerticalAlignment.Top;    return this; }
    public Element AlignMiddle() { VerticalAlignment = VerticalAlignment.Middle; return this; }
    public Element AlignBottom() { VerticalAlignment = VerticalAlignment.Bottom; return this; }

    /// <summary>
    /// Wire a post-render hook (page, page number, on-page bounds) —
    /// canonical use: a Link annotation matched to the rendered box without
    /// hand-tracking coordinates. Replaces any previously-installed hook;
    /// use <see cref="AddRenderedListener"/> to chain. Chainable.
    /// </summary>
    public Element OnRendered(Action<RenderedData> hook) { _onRendered = hook; return this; }

    /// <summary>Compose <paramref name="hook"/> with any existing handler — both fire, in install order. Used by anchor / link helpers so they don't trample a user-installed handler.</summary>
    internal Element AddRenderedListener(Action<RenderedData> hook)
    {
        var existing = _onRendered;
        _onRendered = existing is null
            ? hook
            : data => { existing(data); hook(data); };
        return this;
    }

    /// <summary>Set the wrapped child element.</summary>
    public Element Content(Element child)
    {
        _content = child;
        return this;
    }

    // ===== Sizing ============================================================

    /// <summary>
    /// Default size hint for a chrome container around <see cref="Content(Element)"/>:
    /// measures the child inside the chrome-inset area and adds the chrome
    /// back. Subclasses (text, stacks, columns, …) override with their own
    /// measurement.
    /// </summary>
    public virtual PdfSizeHint SizeHint(PdfSize available)
    {
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;

        var inner = new PdfSize(
            Math.Max(0, (explicitW ?? available.Width) - chromeW),
            Math.Max(0, (explicitH ?? available.Height) - chromeH));

        var hint = _content?.SizeHint(inner) ?? new PdfSizeHint(0, 0, null, null);

        double minW = explicitW ?? (_content is null ? chromeW : hint.MinWidth + chromeW);
        double minH = explicitH ?? (_content is null ? chromeH : hint.MinHeight + chromeH);
        double? maxW = explicitW ?? (hint.MaxWidth is null ? null : hint.MaxWidth.Value + chromeW);
        double? maxH = explicitH ?? (hint.MaxHeight is null ? null : hint.MaxHeight.Value + chromeH);

        return new PdfSizeHint(minW, minH, maxW, maxH);
    }

    /// <summary>
    /// Optional natural drawing width inside the inner area. When narrower
    /// than the inner width, <see cref="HorizontalAlignment"/> distributes
    /// the horizontal slack. Default = the wrapped child's preferred max
    /// width (<c>null</c> when there's no child or it wants the full width).
    /// </summary>
    protected virtual double? DrawNaturalWidth(PdfSize innerAvailable) =>
        _content?.SizeHint(innerAvailable).MaxWidth;

    // ===== Render ============================================================

    /// <summary>
    /// Single render entry point. Runs the box pipeline when chrome is
    /// present, else calls <see cref="Draw"/> directly; then fires
    /// <see cref="OnRendered(Action{RenderedData})"/>.
    /// </summary>
    public RenderResult Render(ContentStream cs, PdfSize available)
    {
        var result = HasBox ? RenderBox(cs, available) : Draw(cs, available);
        FireOnRendered(cs, available, result);
        return result;
    }

    /// <summary>
    /// Subclass-specific inner drawing, into <paramref name="cs"/> at (0, 0)
    /// of the (chrome-inset) area. Default renders the wrapped
    /// <see cref="Content(Element)"/>; override for text, stacks, columns,
    /// canvases, etc. Return the rendered height via
    /// <see cref="RenderResult.Done(double)"/> (plus a continuation when the
    /// content overflows its slot).
    /// </summary>
    protected virtual RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (_content is null) return RenderResult.Done(0);
        return _content.Render(cs, available);
    }

    /// <summary>
    /// True when the element carries any box chrome (padding, border,
    /// background, radius, explicit size, fill-slot, non-default alignment)
    /// or wraps a child — i.e. when the full box pipeline is needed. A bare
    /// leaf element with none of these skips straight to <see cref="Draw"/>.
    /// </summary>
    private bool HasBox =>
        _content is not null
        || _paddingTop != 0 || _paddingRight != 0 || _paddingBottom != 0 || _paddingLeft != 0
        || BorderTopWidth != 0 || BorderRightWidth != 0 || BorderBottomWidth != 0 || BorderLeftWidth != 0
        || _background is not null
        || HasRoundedCorners
        || _width is not null || _height is not null
        || _fillSlotHeight
        || HorizontalAlignment != HorizontalAlignment.Left
        || VerticalAlignment != VerticalAlignment.Top;

    /// <summary>
    /// Report the on-page rectangle as the outer box (chrome included), not
    /// the slot it was placed into. Width is recomputed from
    /// <paramref name="available"/>; height comes back via
    /// <see cref="RenderResult.NextY"/>.
    /// </summary>
    protected virtual (double Width, double Height) GetRenderedExtent(PdfSize available, RenderResult result) =>
        (Math.Min(ResolveWidth(available.Width) ?? available.Width, available.Width), result.NextY);

    private RenderResult RenderBox(ContentStream cs, PdfSize available)
    {
        // Outer width: explicit _width (resolved + clamped to available),
        // else the full available width.
        double outerW = Math.Min(ResolveWidth(available.Width) ?? available.Width, available.Width);

        double innerX = _paddingLeft + BorderLeftWidth;
        double innerY = _paddingTop + BorderTopWidth;
        double innerW = Math.Max(0, outerW - HorizontalChrome);

        // Inner height: explicit _height (resolved + clamped) - chrome, else
        // available.Height - chrome. The actual outer height the box settles
        // on depends on alignment + content height, computed after Draw.
        double maxOuterH = Math.Min(ResolveHeight(available.Height) ?? available.Height, available.Height);
        double innerH = Math.Max(0, maxOuterH - VerticalChrome);

        // Horizontal slack: only applies when the subclass advertises a
        // narrower natural width than innerW.
        double? natural = DrawNaturalWidth(new PdfSize(innerW, innerH));
        double drawW = natural is double nw ? Math.Min(innerW, nw) : innerW;
        double hSlack = Math.Max(0, innerW - drawW);
        double xOffset = HorizontalAlignment switch
        {
            HorizontalAlignment.Center => hSlack / 2,
            HorizontalAlignment.Right => hSlack,
            _ => 0,
        };

        // Render content into a deferred sub. We hold its buffer so we can
        // size the chrome to the actual rendered height and apply vertical
        // alignment by re-positioning the sub before flushing.
        var sub = cs.CreateSubStream(innerX + xOffset, innerY, drawW, innerH);
        var result = Draw(sub, new PdfSize(drawW, innerH));

        // Outer height + vertical slack:
        //  - Height set → box is exactly that tall (clamped). Slack lives
        //    between rendered content and inner area; VerticalAlignment
        //    distributes it.
        //  - Height null → shrink to content + chrome regardless of
        //    VerticalAlignment.
        bool fillHeight = _height is not null || _fillSlotHeight;
        double outerH = fillHeight ? maxOuterH : result.NextY + VerticalChrome;
        double vSlack = fillHeight ? Math.Max(0, innerH - result.NextY) : 0;
        double yOffset = VerticalAlignment switch
        {
            VerticalAlignment.Middle => vSlack / 2,
            VerticalAlignment.Bottom => vSlack,
            _ => 0,
        };

        if (yOffset != 0) sub.SetParentPosition(innerX + xOffset, innerY + yOffset);

        PaintBackgroundAndBorders(cs, outerW, outerH);
        sub.Build();

        // Propagate any continuation Draw produced so the page-level Body
        // loop can add a new page and keep going.
        return new RenderResult(outerH, result.NextElement);
    }

    private void PaintBackgroundAndBorders(ContentStream cs, double width, double height)
    {
        if (!HasRoundedCorners)
        {
            if (_background is { } bg)
                cs.DrawRectangle(0, 0, width, height, fill: bg);
            PaintBordersPerSide(cs, width, height);
            return;
        }

        // Rounded path: establish a clip to the rounded outline, then paint
        // background + borders inside it.
        cs.Save();
        cs.ClipPath(c => c.RoundedRectangle(0, 0, width, height,
            BorderRadiusTopLeft, BorderRadiusTopRight,
            BorderRadiusBottomRight, BorderRadiusBottomLeft));

        if (_background is { } bgRounded)
            cs.DrawRectangle(0, 0, width, height, fill: bgRounded);

        if (TryGetUniformBorder(out var borderWidth, out var borderColor)
            && borderWidth > 0 && borderColor is not null)
        {
            cs.Save();
            cs.SetStrokeColor(borderColor);
            cs.SetLineWidth(borderWidth * 2);
            cs.RoundedRectangle(0, 0, width, height,
                BorderRadiusTopLeft, BorderRadiusTopRight,
                BorderRadiusBottomRight, BorderRadiusBottomLeft);
            cs.Stroke();
            cs.Restore();
        }
        else
        {
            PaintBordersPerSide(cs, width, height);
        }

        cs.Restore();
    }

    private void PaintBordersPerSide(ContentStream cs, double width, double height)
    {
        if (BorderTopColor is { } tc && BorderTopWidth > 0)
            cs.DrawRectangle(0, 0, width, BorderTopWidth, fill: tc);
        if (BorderRightColor is { } rc && BorderRightWidth > 0)
            cs.DrawRectangle(width - BorderRightWidth, 0, BorderRightWidth, height, fill: rc);
        if (BorderBottomColor is { } bc && BorderBottomWidth > 0)
            cs.DrawRectangle(0, height - BorderBottomWidth, width, BorderBottomWidth, fill: bc);
        if (BorderLeftColor is { } lc && BorderLeftWidth > 0)
            cs.DrawRectangle(0, 0, BorderLeftWidth, height, fill: lc);
    }

    private bool TryGetUniformBorder(out double width, out PdfColor? color)
    {
        width = BorderTopWidth;
        color = BorderTopColor;
        if (BorderRightWidth != width || BorderBottomWidth != width || BorderLeftWidth != width)
            return false;
        return SameColor(BorderRightColor, color)
            && SameColor(BorderBottomColor, color)
            && SameColor(BorderLeftColor, color);
    }

    private static bool SameColor(PdfColor? a, PdfColor? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Space == b.Space && a.C1 == b.C1 && a.C2 == b.C2 && a.C3 == b.C3 && a.C4 == b.C4;
    }

    private void FireOnRendered(ContentStream cs, PdfSize available, RenderResult result)
    {
        if (_onRendered is not { } hook) return;
        if (cs.OwningPage is not { } page) return;

        var (w, h) = GetRenderedExtent(available, result);
        var (ux, uy) = cs.ToPageUserPoint(0, 0);
        double pageH = page.PageHeight;
        var bounds = new PdfRectangle(ux, pageH - (uy + h), ux + w, pageH - uy);
        hook(new RenderedData(page, page.PageNumber, bounds));
    }

    // ===== Static composition helpers ========================================

    public static Paragraph Paragraph(string text, Font font, double size) => new(text, font, size);

    /// <summary>Helvetica 11 — the conventional body-text default.</summary>
    public static Paragraph Paragraph(string text) => new(text, StandardFont.Helvetica, 11);

    /// <summary>
    /// Multi-span paragraph (low-level lambda form). <paramref name="defaultFont"/>
    /// is used by <c>.Text(string)</c> calls in the builder; per-span fonts
    /// can be passed explicitly. All spans share <paramref name="size"/>.
    /// </summary>
    public static Paragraph Paragraph(Font defaultFont, double size, Action<Paragraph> build) =>
        new(defaultFont, size, build);

    /// <summary>
    /// Multi-span paragraph (high-level family form). Returns a
    /// <see cref="FamilyParagraph"/> onto which spans are added via
    /// <c>.Bold(...)</c>, <c>.Italic(...)</c>, <c>.BoldItalic(...)</c>,
    /// <c>.Text(...)</c>, and <c>.Newline()</c>.
    /// </summary>
    public static FamilyParagraph Paragraph(FontFamily family, double size) => new(family, size);

    /// <summary>
    /// Multi-span paragraph (family + lambda). Combines the family form's
    /// face-aware setters with a builder lambda — useful when the spans
    /// are built imperatively rather than chained.
    /// </summary>
    public static FamilyParagraph Paragraph(FontFamily family, double size, Action<FamilyParagraph> build) =>
        new(family, size, build);

    /// <summary>
    /// Reflow paragraph (family form). Returns a <see cref="ReflowParagraph"/>
    /// onto which spans and floats are added via the chainable builder.
    /// </summary>
    public static ReflowParagraph ReflowParagraph(FontFamily family, double size) => new(family, size);

    /// <summary>
    /// Reflow paragraph (family + lambda). Builds the paragraph imperatively
    /// inside <paramref name="build"/>.
    /// </summary>
    public static ReflowParagraph ReflowParagraph(FontFamily family, double size, Action<ReflowParagraph> build) =>
        new(family, size, build);

    public static VStack VStack() => new();

    public static VStack VStack(Action<VStack> build)
    {
        var v = new VStack();
        build(v);
        return v;
    }

    public static HStack HStack() => new();

    public static HStack HStack(Action<HStack> build)
    {
        var h = new HStack();
        build(h);
        return h;
    }

    public static VFrame VFrame() => new();

    public static VFrame VFrame(Action<VFrame> build)
    {
        var f = new VFrame();
        build(f);
        return f;
    }

    /// <summary>
    /// A styled-chrome container (background, padding, per-side borders,
    /// alignment) wrapping a single child — a bare <see cref="Element"/>.
    /// </summary>
    public static Element Container() => new();

    public static Element Container(Action<Element> build)
    {
        var c = new Element();
        build(c);
        return c;
    }

    /// <summary>
    /// An imperative drawing surface — inside <paramref name="draw"/> the
    /// sub-stream's (0, 0) is the surface's top-left in user coords.
    /// </summary>
    public static Canvas Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        new() { Width = width, Height = height, Paint = draw };

    /// <summary>
    /// A dropped capital sized to fill its box — float it at the start of a
    /// <see cref="ReflowParagraph"/> so the body text wraps around it.
    /// </summary>
    public static DropCap DropCap(string text, Font font, PdfColor? color = null) =>
        new(text, font, color);

    /// <summary>
    /// Two-phase deferred rendering — <paramref name="sizeHint"/> reserves
    /// the box during normal layout, <paramref name="render"/> runs once
    /// the page count is known and decides what actually paints there.
    /// </summary>
    public static DeferredComponent Deferred(Element sizeHint, Func<PageData, Element> render) =>
        new(sizeHint, render);

    /// <summary>Sentinel that forces the next item in its parent container onto a new page.</summary>
    public static PageBreak PageBreak() => new();
}
