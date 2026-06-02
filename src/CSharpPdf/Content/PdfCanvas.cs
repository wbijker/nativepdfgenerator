using CSharpPdf.Geometry;
using CSharpPdf.Images;
using CSharpPdf.Layout;
using CSharpPdf.Objects;
using CSharpPdf.Text;

namespace CSharpPdf.Content;

/// <summary>
/// Concrete per-page drawing surface implementing <see cref="IPdfCanvas"/>.
/// Each call to <see cref="Graphics"/> or <see cref="Text"/> emits the
/// matching opener (q / BT) and returns a fresh disposable scope; disposing
/// the scope emits the closer (Q / ET). Use with <c>using</c>.
///
/// Coordinates are PDF user space (origin bottom-left, Y increases upward).
/// </summary>
public sealed class PdfCanvas : IPdfCanvas
{
    private readonly PdfPage _page;
    private readonly PdfDoc _doc;
    private readonly ContentStream _cs;
    private readonly PathSurface _pathSurface;

    // Layout origin in PDF user space. The canvas's local (0, 0) maps to this
    // (_absLeft, _absBottomY) in PDF absolute coords; local Y stays Y-up (the
    // same direction PDF uses), so converting between the two is just adding
    // these offsets. For a root page canvas these are both 0, making local ==
    // absolute and existing direct-canvas callers see no change.
    private readonly double _absLeft;
    private readonly double _absBottomY;

    // Per-canvas typed-handle → registered-name caches. Sub-canvases share the
    // parent's dictionaries (passed in via the internal sub-canvas constructor)
    // so a font/image registered through a sub-canvas is still resolved on the
    // owning page rather than reissued.
    private readonly Dictionary<PdfReference, string> _xobjectNames;
    private readonly Dictionary<FormXObject, string> _formNames;
    private readonly Dictionary<PdfReference, string> _shadingNames;
    private readonly Dictionary<PdfDictionary, string> _extGStateNames;

    // Per-canvas naming counters. Layout flows that span multiple pages
    // (LayoutEngine) hand a shared counter array in so the resource numbering
    // accumulates across pages — matches the historical PdfContext behaviour.
    // Direct-canvas callers get a fresh array per page.
    private readonly int[] _seqBox; // [imgSeq, formSeq, shSeq, gsSeq, layoutImgSeq]

    /// <summary>Width of the canvas in points (the local x extent: 0 to <see cref="Width"/>).</summary>
    public double Width { get; }

    /// <summary>Height of the canvas in points (the local y extent: 0 to <see cref="Height"/>, Y-up).</summary>
    public double Height { get; }

    /// <summary>Page-level cursor in local canvas coordinates. Layout components read/advance this; not used by direct-canvas callers.</summary>
    public Point Cursor { get; set; }

    /// <summary>1-based number of the current page (mirrors the engine's counter; set by the layout engine).</summary>
    public int PageNumber { get; internal set; }

    /// <summary>Total page count, populated in the render phase of <see cref="LayoutEngine.SaveTwoPhase"/> (0 in measure).</summary>
    public int TotalPages { get; internal set; }

    /// <summary>Current rendering phase. In Measure all drawing primitives are no-ops so the engine just paginates.</summary>
    public RenderMode Mode { get; internal set; } = RenderMode.Render;

    /// <summary>
    /// When true, <see cref="UIElement.Render"/> bypasses its "doesn't fit, defer
    /// to next page" check and renders the element regardless. Set by the engine
    /// when an element deferred on a fresh empty page; auto-clears after the retry.
    /// </summary>
    public bool ForceRender { get; internal set; }

    /// <summary>Page this canvas writes to (shared across sub-canvases).</summary>
    public PdfPage Page => _page;

    /// <summary>Document the page belongs to.</summary>
    public PdfDoc Document => _doc;

    // Two-phase capture store: written during measure, read in either phase.
    // Shared across sub-canvases (same dictionary instance).
    private Dictionary<string, object> _captured;

    /// <summary>Internal access to the capture dictionary (engine assigns it between phases).</summary>
    internal Dictionary<string, object> Captured
    {
        get => _captured;
        set => _captured = value;
    }

    // Bookmark queue collected during render; flushed by the engine into the
    // document's outline. Shared across sub-canvases.
    private List<(string Title, PdfArray Destination)> _pendingBookmarks;

    /// <summary>Outline entries collected during rendering (engine flushes them into <c>PdfDoc.SetOutline</c> at <c>Finish()</c>). The engine reassigns the underlying list between pages so the bookmark queue survives page transitions.</summary>
    internal List<(string Title, PdfArray Destination)> PendingBookmarks
    {
        get => _pendingBookmarks;
        set => _pendingBookmarks = value;
    }

    // Per-word measurement cache. Shared across sub-canvases. Keyed by
    // (font-PostScript-name, font-size, word). Populated by text components
    // that opt in via SaveMetric=true and consulted on subsequent measurements.
    private readonly Dictionary<(string Font, double Size, string Word), double> _wordWidthCache;

    /// <summary>Per-word width cache. Layout components may store and look up word widths here to skip remeasurement.</summary>
    internal Dictionary<(string Font, double Size, string Word), double> WordWidthCache => _wordWidthCache;

    // Deferred-render queue. Components that depend on document-wide state
    // (page numbers, anchor positions) register a sub-canvas + closure here
    // during the single layout pass; the engine drains the queue after the
    // pass completes — at which point TotalPages is known and every
    // NamedAnchorElement has recorded its page. Shared across sub-canvases so
    // a deferral at any depth lands on the engine-owned list.
    private List<(PdfCanvas Sub, Action<PdfCanvas> Render)> _deferredRenders;

    /// <summary>Pending deferred renders (engine-owned; sub-canvases share the same list).</summary>
    internal List<(PdfCanvas Sub, Action<PdfCanvas> Render)> DeferredRenders
    {
        get => _deferredRenders;
        set => _deferredRenders = value;
    }

    internal PdfCanvas(PdfPage page, PdfDoc doc)
    {
        _page = page;
        _doc = doc;
        _cs = page.Content;
        _pathSurface = new PathSurface(this);
        _absLeft = 0;
        _absBottomY = 0;
        // Root-canvas width/height are informational only; direct-canvas
        // callers (e.g. sample code) pass absolute PDF coords and never read
        // these. The layout engine creates sub-canvases with explicit sizes.
        Width = 0;
        Height = 0;
        _xobjectNames = new();
        _formNames = new();
        _shadingNames = new();
        _extGStateNames = new();
        _seqBox = new int[5];
        _captured = new();
        _pendingBookmarks = new();
        _wordWidthCache = new();
        _deferredRenders = new();
    }

    /// <summary>
    /// Engine-side initialisation hook for the LayoutEngine: lets it inject the
    /// shared layout-image counter so resource names continue numbering across
    /// pages (preserving the historical sequence that pre-dated the refactor).
    /// </summary>
    internal int[] SeqBox => _seqBox;

    // Sub-canvas constructor: shares the parent's page, doc, content stream,
    // resource caches, capture store, bookmark queue and word-width cache so
    // every canvas in the tree sees the same accumulated state.
    private PdfCanvas(PdfCanvas parent, double absLeft, double absBottomY, double width, double height)
    {
        _page = parent._page;
        _doc = parent._doc;
        _cs = parent._cs;
        _pathSurface = new PathSurface(this);
        _absLeft = absLeft;
        _absBottomY = absBottomY;
        Width = width;
        Height = height;
        _xobjectNames = parent._xobjectNames;
        _formNames = parent._formNames;
        _shadingNames = parent._shadingNames;
        _extGStateNames = parent._extGStateNames;
        _seqBox = parent._seqBox;
        _captured = parent._captured;
        _pendingBookmarks = parent._pendingBookmarks;
        _wordWidthCache = parent._wordWidthCache;
        _deferredRenders = parent._deferredRenders;
        Mode = parent.Mode;
        PageNumber = parent.PageNumber;
        TotalPages = parent.TotalPages;
        ForceRender = parent.ForceRender;
        Cursor = new Point(0, height);
    }

    /// <summary>Create a sub-canvas at <paramref name="localX"/>,<paramref name="localTopY"/> in this canvas's local coords (Y-up; localTopY is the top edge), sized <paramref name="width"/>×<paramref name="height"/>.</summary>
    public PdfCanvas Sub(double localX, double localTopY, double width, double height)
    {
        // Child's bottom-left in parent's local coords = (localX, localTopY - height).
        // Translate to absolute by adding the parent's own origin offset.
        double absLeft = _absLeft + localX;
        double absBottom = _absBottomY + localTopY - height;
        return new PdfCanvas(this, absLeft, absBottom, width, height);
    }

    /// <summary>Translate a local X to PDF absolute X.</summary>
    public double ToAbsoluteX(double localX) => _absLeft + localX;

    /// <summary>Translate a local Y (Y-up, where 0 is canvas bottom and <see cref="Height"/> is the top) to PDF absolute Y.</summary>
    public double ToAbsoluteY(double localY) => _absBottomY + localY;

    /// <summary>Translate a PDF absolute X back to this canvas's local X (Y-up, X grows right).</summary>
    public double ToLocalX(double absoluteX) => absoluteX - _absLeft;

    /// <summary>Translate a PDF absolute Y back to this canvas's local Y (Y-up; 0 is the canvas bottom).</summary>
    public double ToLocalY(double absoluteY) => absoluteY - _absBottomY;

    // ===== Two-phase capture store ====================================

    /// <summary>
    /// Record a value into the document-wide capture store. In the
    /// single-phase model this always records (the old measure-only guard is
    /// gone) so elements visited during the main pass can publish state for
    /// later deferred-render closures to read.
    /// </summary>
    public void Capture(string key, object value) => _captured[key] = value;

    /// <summary>Read a captured value (returns default if not captured yet).</summary>
    public T? Lookup<T>(string key) =>
        _captured.TryGetValue(key, out var v) && v is T t ? t : default;

    /// <summary>Variant of <see cref="Lookup{T}"/> that signals presence.</summary>
    public bool TryLookup<T>(string key, out T value)
    {
        if (_captured.TryGetValue(key, out var v) && v is T t)
        {
            value = t;
            return true;
        }
        value = default!;
        return false;
    }

    // ===== Layout draw helpers (local coords; translate to absolute) ==
    // These mirror the high-level helpers UIElement subclasses used to get
    // through PdfContext. Each accepts coordinates in this canvas's LOCAL
    // space (Y-up; top edge is <see cref="Height"/>) and translates to the
    // page's absolute PDF coords by adding the canvas origin. Existing direct-
    // canvas callers see absolute coords because the root canvas has origin
    // (0, 0).

    /// <summary>Draw a single line of text at local (x, baselineY).</summary>
    public void DrawText(Font font, double size, double x, double baselineY, string text, Color color)
    {
        if (Mode == RenderMode.Measure) return;
        using var g = Graphics();
        g.SetFillRgb(color.R, color.G, color.B);
        g.DrawText(font, size, _absLeft + x, _absBottomY + baselineY, text);
    }

    /// <summary>Fill a rectangle whose upper-left corner is at local (x, top).</summary>
    public void FillRectangle(double x, double top, double width, double height, Color color)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0) return;
        double absX = _absLeft + x;
        double absTop = _absBottomY + top;
        _cs.Save().SetRgbFill(color.R, color.G, color.B)
            .Rectangle(absX, absTop - height, width, height).Fill().Restore();
    }

    /// <summary>Stroke a rectangle outline at local (x, top).</summary>
    public void StrokeRectangle(double x, double top, double width, double height, Color color, double lineWidth)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0 || lineWidth <= 0) return;
        double absX = _absLeft + x;
        double absTop = _absBottomY + top;
        double half = lineWidth / 2;
        _cs.Save().SetRgbStroke(color.R, color.G, color.B).SetLineWidth(lineWidth)
            .Rectangle(absX + half, absTop - height + half, width - lineWidth, height - lineWidth).Stroke().Restore();
    }

    /// <summary>
    /// Draw a <see cref="PdfImage"/> into the box whose upper-left corner is local
    /// <c>(x, top)</c>. The underlying XObject is embedded once on the document
    /// (<see cref="PdfImage.EmbedIn"/>) and registered on the page once; subsequent
    /// calls with the same instance just emit another <c>Do</c>.
    /// </summary>
    public void DrawImage(PdfImage image, double x, double top, double width, double height)
    {
        if (Mode == RenderMode.Measure) return;
        double absX = _absLeft + x;
        double absTop = _absBottomY + top;
        EmitImage(image, absX, absTop - height, width, height);
    }

    /// <summary>
    /// Draw a <see cref="ReuseComponent"/> with its upper-left corner at local
    /// <c>(x, top)</c>, no scaling. The underlying Form XObject is embedded
    /// once on the document and registered on the page once; subsequent calls
    /// with the same instance emit another positioned <c>Do</c>.
    /// </summary>
    public void DrawComponent(ReuseComponent component, double x, double top) =>
        DrawComponent(component, x, top, 1, 1);

    /// <summary>Draw a component at local <c>(x, top)</c> with uniform scale.</summary>
    public void DrawComponent(ReuseComponent component, double x, double top, double scale) =>
        DrawComponent(component, x, top, scale, scale);

    /// <summary>Draw a component at local <c>(x, top)</c> with independent x/y scaling.</summary>
    public void DrawComponent(ReuseComponent component, double x, double top, double sx, double sy)
    {
        if (Mode == RenderMode.Measure) return;
        double absX = _absLeft + x;
        double absTop = _absBottomY + top;
        EmitComponent(component, absX, absTop - component.Height * sy, sx, sy);
    }

    /// <summary>Fill a rounded rectangle at local (x, top). <paramref name="radius"/> is clamped to half the smaller side.</summary>
    public void FillRoundedRectangle(double x, double top, double width, double height, Color color, double radius)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0) return;
        if (radius <= 0) { FillRectangle(x, top, width, height, color); return; }
        double absX = _absLeft + x;
        double absTop = _absBottomY + top;
        _cs.Save().SetRgbFill(color.R, color.G, color.B);
        TraceRoundedRectFromTop(_cs, absX, absTop, width, height, radius);
        _cs.Fill().Restore();
    }

    /// <summary>Stroke a rounded rectangle outline at local (x, top), with an optional dash pattern.</summary>
    public void StrokeRoundedRectangle(double x, double top, double width, double height,
        Color color, double lineWidth, double radius, double[]? dash = null)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0 || lineWidth <= 0) return;
        double absX = _absLeft + x;
        double absTop = _absBottomY + top;
        double half = lineWidth / 2;
        _cs.Save().SetRgbStroke(color.R, color.G, color.B).SetLineWidth(lineWidth);
        if (dash is { Length: > 0 }) _cs.SetDash(dash);
        if (radius <= 0)
        {
            _cs.Rectangle(absX + half, absTop - height + half, width - lineWidth, height - lineWidth).Stroke();
        }
        else
        {
            TraceRoundedRectFromTop(_cs, absX + half, absTop - half, width - lineWidth, height - lineWidth, System.Math.Max(0, radius - half));
            _cs.Stroke();
        }
        _cs.Restore();
    }

    // Trace a rounded rectangle starting at its TOP edge (matches the pre-
    // refactor PdfContext trace order so the emitted operators are byte-
    // identical). (x, top) is the upper-left corner; the path goes clockwise.
    private static void TraceRoundedRectFromTop(ContentStream cs,
        double x, double top, double width, double height, double radius)
    {
        double r = System.Math.Min(radius, System.Math.Min(width, height) / 2);
        const double K = 0.5522847498; // bezier ⇄ quarter-circle constant
        double c = r * K;
        double bottom = top - height;
        double right = x + width;
        cs.MoveTo(x + r, top)
          .LineTo(right - r, top)
          .CurveTo(right - r + c, top, right, top - r + c, right, top - r)
          .LineTo(right, bottom + r)
          .CurveTo(right, bottom + r - c, right - r + c, bottom, right - r, bottom)
          .LineTo(x + r, bottom)
          .CurveTo(x + r - c, bottom, x, bottom + r - c, x, bottom + r)
          .LineTo(x, top - r)
          .CurveTo(x, top - r + c, x + r - c, top, x + r, top)
          .ClosePath();
    }

    /// <summary>
    /// Reserve a <paramref name="width"/>×<paramref name="height"/> region at the current cursor and
    /// defer its painting until the document layout is complete. Nothing is
    /// emitted during this call; instead a sub-canvas pinned to this position
    /// + size is captured along with <paramref name="render"/>, queued onto the
    /// engine's deferred list, and replayed by <c>LayoutEngine.Save</c> once
    /// <see cref="TotalPages"/> is known and every <see cref="NamedAnchorElement"/>
    /// has recorded its page.
    ///
    /// The deferred render is <b>non-reflowable</b>: it draws into the fixed
    /// (width, height) reserved here and must not exceed it. This is the
    /// single-phase replacement for the old measure → render two-pass.
    /// </summary>
    public void Defer(double width, double height, Action<PdfCanvas> render)
    {
        var sub = Sub(Cursor.X, Cursor.Y, width, height);
        _deferredRenders.Add((sub, render));
    }

    /// <summary>
    /// Render <paramref name="element"/> at local (<paramref name="x"/>, <paramref name="topY"/>) — a sub-canvas
    /// is constructed at that position (sized to <paramref name="element"/>'s SpaceHint) and
    /// <see cref="UIElement.Render"/> is invoked on it. The caller's cursor and absolute origin are untouched.
    /// </summary>
    public RenderResult Draw(double x, double topY, UIElement element)
    {
        if (element is null) throw new System.ArgumentNullException(nameof(element));
        // Determine the slot the child will occupy: as wide as remains to our right,
        // as tall as the local space above the canvas bottom.
        double remainingWidth = System.Math.Max(0, Width - x);
        double remainingHeight = System.Math.Max(0, topY);
        var size = new Size(remainingWidth, remainingHeight);
        var sub = Sub(x, topY, remainingWidth, remainingHeight);
        return element.Render(sub, size);
    }

    /// <summary>Underlying content stream — escape hatch for operators not surfaced here.</summary>
    public ContentStream Raw => _cs;

    // ===== Scope entries (imperative — return disposable scopes) ======

    public PdfGraphics Graphics()
    {
        _cs.Save();
        return new GraphicsScope(this);
    }

    public PdfTextObject Text()
    {
        _cs.BeginText();
        return new TextObjectScope(this);
    }

    // ===== Marked content / structure / optional content (callbacks) ==

    public void MarkedContent(string tag, Action<IPdfCanvas> body)
    {
        _cs.BeginMarkedContent(tag);
        try { body(this); }
        finally { _cs.EndMarkedContent(); }
    }

    public void MarkedContent(string tag, PdfDictionary properties, Action<IPdfCanvas> body)
    {
        _cs.BeginMarkedContent(tag, properties);
        try { body(this); }
        finally { _cs.EndMarkedContent(); }
    }

    public void OptionalContent(string registeredPropertyName, Action<IPdfCanvas> body)
    {
        _cs.BeginOptionalContent(registeredPropertyName);
        try { body(this); }
        finally { _cs.EndMarkedContent(); }
    }

    public void StructureContent(string tag, int mcid, Action<IPdfCanvas> body)
    {
        _cs.BeginStructureContent(tag, mcid);
        try { body(this); }
        finally { _cs.EndMarkedContent(); }
    }

    public void Artifact(Action<IPdfCanvas> body)
    {
        _cs.BeginArtifact();
        try { body(this); }
        finally { _cs.EndMarkedContent(); }
    }

    public void MarkPoint(string tag) => _cs.MarkPoint(tag);
    public void MarkPoint(string tag, PdfDictionary properties) => _cs.MarkPoint(tag, properties);

    // ===== Annotations =================================================

    public PdfReference AddAnnotation(PdfDictionary annotation) => _page.AddAnnotation(annotation);

    public PdfReference AddLink(PdfRectangle rect, PdfDictionary action) =>
        _page.AddLinkAnnotation(rect, action);

    public PdfReference AddUrlLink(PdfRectangle rect, string url) => AddLink(rect, new PdfDictionary
    {
        ["Type"] = new PdfName("Action"),
        ["S"] = new PdfName("URI"),
        ["URI"] = new PdfString(url),
    });

    public PdfReference AddGoToLink(PdfRectangle rect, PdfArray destination) => AddLink(rect, new PdfDictionary
    {
        ["Type"] = new PdfName("Action"),
        ["S"] = new PdfName("GoTo"),
        ["D"] = destination,
    });

    public PdfReference AddGoToLink(PdfRectangle rect, string namedDestination) => AddLink(rect, new PdfDictionary
    {
        ["Type"] = new PdfName("Action"),
        ["S"] = new PdfName("GoTo"),
        ["D"] = new PdfString(namedDestination),
    });

    public void AddTextNote(PdfRectangle iconRect, string contents, string icon,
        PdfRectangle popupRect, bool open = true) =>
        _page.AddTextNote(iconRect, contents, icon, popupRect, open);

    // ===== Internals (shared by nested surfaces) =======================

    private string UseFont(Font font)
    {
        var (name, reference) = _doc.UseFont(font);
        _page.AddFont(name, reference);
        return name;
    }

    private string UseXObject(PdfReference image)
    {
        if (!_xobjectNames.TryGetValue(image, out var name))
        {
            name = $"Img{++_seqBox[0]}";
            _page.AddXObject(name, image);
            _xobjectNames[image] = name;
        }
        return name;
    }

    // Emit a PdfImage at (absX, absY-bottom-left, width, height) in absolute
    // user space. Common entrypoint shared by the canvas (layout-top coords)
    // and GraphicsScope (raw user-space coords). Picks between inline
    // (BI/ID/EI) and XObject (Do) emission based on PdfImage.PreferInline +
    // payload size — see PdfImage docs.
    private void EmitImage(PdfImage image, double absX, double absY, double width, double height)
    {
        // Inline path: caller opted in and the payload fits the inline budget.
        // We re-emit the bytes at every paint site, so this is only worth doing
        // for tiny images the caller is confident won't be reused.
        if (image.PreferInline && image.EncodedSize < 4096 && image.CanInline)
        {
            _cs.Save().Transform(width, 0, 0, height, absX, absY)
               .Raw(image.BuildInlineBody())
               .Restore();
            return;
        }

        // XObject path: embed once on the document, register once on the page,
        // emit Do for every paint site. The doc-level cache lives on the
        // PdfImage itself, the page-level cache on _xobjectNames.
        var reference = image.EmbedIn(_doc);
        string name = UseXObject(reference);
        _cs.DrawImage(name, absX, absY, width, height);
    }

    // Emit a ReuseComponent at absolute user-space (absX, absY) (form's local
    // origin), scaled by (sx, sy). Common entrypoint shared by the canvas
    // (layout-top coords) and GraphicsScope (raw user-space coords).
    // Doc-level dedup lives on the ReuseComponent (its cached PdfReference);
    // page-level dedup lives in _xobjectNames.
    private void EmitComponent(ReuseComponent component, double absX, double absY, double sx, double sy)
    {
        var reference = component.EmbedIn(_doc);
        string name = UseXObject(reference);
        _cs.Save().Transform(sx, 0, 0, sy, absX, absY).PaintXObject(name).Restore();
    }

    private string UseForm(FormXObject form)
    {
        if (!_formNames.TryGetValue(form, out var name))
        {
            name = $"Fm{++_seqBox[1]}";
            _page.AddXObject(name, _doc.AddObject(form.Build()));
            _formNames[form] = name;
        }
        return name;
    }

    private void RegisterShading(PdfReference shading, out string name)
    {
        if (!_shadingNames.TryGetValue(shading, out name!))
        {
            name = $"Sh{++_seqBox[2]}";
            _page.AddShading(name, shading);
            _shadingNames[shading] = name;
        }
    }

    private void RegisterExtGState(PdfDictionary gs, out string name)
    {
        if (!_extGStateNames.TryGetValue(gs, out name!))
        {
            name = $"GS{++_seqBox[3]}";
            _page.AddExtGState(name, gs);
            _extGStateNames[gs] = name;
        }
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

    private static void TraceRoundedRect(ContentStream cs,
        double x, double y, double width, double height, double radius)
    {
        double r = System.Math.Min(radius, System.Math.Min(width, height) / 2);
        if (r <= 0) { cs.Rectangle(x, y, width, height); return; }
        const double K = 0.5522847498307936;
        double c = r * K;
        double right = x + width, top = y + height;
        cs.MoveTo(x + r, y)
          .LineTo(right - r, y)
          .CurveTo(right - r + c, y, right, y + r - c, right, y + r)
          .LineTo(right, top - r)
          .CurveTo(right, top - r + c, right - r + c, top, right - r, top)
          .LineTo(x + r, top)
          .CurveTo(x + r - c, top, x, top - r + c, x, top - r)
          .LineTo(x, y + r)
          .CurveTo(x, y + r - c, x + r - c, y, x + r, y)
          .ClosePath();
    }

    // ===== Nested surfaces ============================================

    /// <summary>
    /// PdfGraphics scope returned by <see cref="Graphics"/>. Constructor emits
    /// q; Dispose emits Q. A fresh instance is created per scope so nesting
    /// works through ordinary <c>using</c> blocks.
    /// </summary>
    private sealed class GraphicsScope : PdfGraphics
    {
        private readonly PdfCanvas _canvas;
        private ContentStream Cs => _canvas._cs;
        private bool _disposed;

        public GraphicsScope(PdfCanvas canvas) => _canvas = canvas;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Cs.Restore();
        }

        // ---- nested scopes ----

        public PdfGraphics Graphics() => _canvas.Graphics();
        public PdfTextObject Text() => _canvas.Text();

        // ---- graphics state ----

        public void SetLineWidth(double width) => Cs.SetLineWidth(width);
        public void SetLineCap(LineCap cap) => Cs.SetLineCap((int)cap);
        public void SetLineJoin(LineJoin join) => Cs.SetLineJoin((int)join);
        public void SetMiterLimit(double limit) => Cs.SetMiterLimit(limit);
        public void SetDashPattern(double[] pattern, double phase = 0) => Cs.SetDash(pattern, phase);
        public void SetFlatness(double tolerance) => Cs.SetFlatness(tolerance);
        public void SetRenderingIntent(RenderingIntent intent) => Cs.SetRenderingIntent(RenderingIntentName(intent));

        public void ApplyExtGState(PdfDictionary gs)
        {
            _canvas.RegisterExtGState(gs, out var name);
            Cs.SetExtGState(name);
        }

        public void SetFillOpacity(double alpha) =>
            ApplyExtGState(new PdfDictionary { ["ca"] = new PdfNumber(alpha) });
        public void SetStrokeOpacity(double alpha) =>
            ApplyExtGState(new PdfDictionary { ["CA"] = new PdfNumber(alpha) });
        public void SetBlendMode(BlendMode mode) =>
            ApplyExtGState(new PdfDictionary { ["BM"] = new PdfName(BlendModeName(mode)) });

        // ---- transforms ----

        public void Transform(double a, double b, double c, double d, double e, double f) =>
            Cs.Transform(a, b, c, d, e, f);
        public void Translate(double tx, double ty) => Cs.Translate(tx, ty);
        public void Scale(double sx, double sy) => Cs.Scale(sx, sy);
        public void Rotate(double degrees) => Cs.Rotate(degrees);

        // ---- colour ----

        public void SetFillGray(double gray) => Cs.SetGrayFill(gray);
        public void SetStrokeGray(double gray) => Cs.SetGrayStroke(gray);
        public void SetFillRgb(double r, double g, double b) => Cs.SetRgbFill(r, g, b);
        public void SetStrokeRgb(double r, double g, double b) => Cs.SetRgbStroke(r, g, b);
        public void SetFillCmyk(double c, double m, double y, double k) => Cs.SetCmykFill(c, m, y, k);
        public void SetStrokeCmyk(double c, double m, double y, double k) => Cs.SetCmykStroke(c, m, y, k);
        public void SetFillColor(Color color) => Cs.SetRgbFill(color.R, color.G, color.B);
        public void SetStrokeColor(Color color) => Cs.SetRgbStroke(color.R, color.G, color.B);
        public void SetFillColorSpace(string name) => Cs.SetFillColorSpace(name);
        public void SetStrokeColorSpace(string name) => Cs.SetStrokeColorSpace(name);
        public void SetFillColorN(params double[] components) => Cs.SetFillColorN(components);
        public void SetStrokeColorN(params double[] components) => Cs.SetStrokeColorN(components);
        public void SetFillPattern(string patternName) => Cs.SetFillPattern(patternName);
        public void SetStrokePattern(string patternName) => Cs.SetStrokePattern(patternName);

        // ---- path drawing (build-then-paint callbacks) ----

        public void StrokePath(Action<PdfPath> build)
        {
            build(_canvas._pathSurface);
            Cs.Stroke();
        }

        public void FillPath(Action<PdfPath> build, FillRule rule = FillRule.NonZero)
        {
            build(_canvas._pathSurface);
            if (rule == FillRule.EvenOdd) Cs.FillEvenOdd(); else Cs.Fill();
        }

        public void FillAndStrokePath(Action<PdfPath> build, FillRule rule = FillRule.NonZero)
        {
            build(_canvas._pathSurface);
            if (rule == FillRule.EvenOdd) Cs.FillStrokeEvenOdd(); else Cs.FillStroke();
        }

        public void ClipPath(Action<PdfPath> build, FillRule rule = FillRule.NonZero)
        {
            build(_canvas._pathSurface);
            if (rule == FillRule.EvenOdd) Cs.ClipEvenOdd(); else Cs.Clip();
            Cs.EndPath();
        }

        public void ClipAndStrokePath(Action<PdfPath> build, FillRule rule = FillRule.NonZero)
        {
            build(_canvas._pathSurface);
            if (rule == FillRule.EvenOdd) Cs.ClipEvenOdd(); else Cs.Clip();
            Cs.Stroke();
        }

        public void ClipAndFillPath(Action<PdfPath> build, FillRule rule = FillRule.NonZero)
        {
            build(_canvas._pathSurface);
            if (rule == FillRule.EvenOdd) Cs.ClipEvenOdd(); else Cs.Clip();
            if (rule == FillRule.EvenOdd) Cs.FillEvenOdd(); else Cs.Fill();
        }

        public void ClipAndFillAndStrokePath(Action<PdfPath> build, FillRule rule = FillRule.NonZero)
        {
            build(_canvas._pathSurface);
            if (rule == FillRule.EvenOdd) Cs.ClipEvenOdd(); else Cs.Clip();
            if (rule == FillRule.EvenOdd) Cs.FillStrokeEvenOdd(); else Cs.FillStroke();
        }

        // ---- shape conveniences (self-contained: own q/Q wrap) ----

        public void DrawRectangle(double x, double y, double width, double height,
            Color? fill = null, Color? stroke = null, double strokeWidth = 1)
        {
            if (fill is null && stroke is null) return;
            Cs.Save();
            ApplyFillStroke(fill, stroke, strokeWidth);
            Cs.Rectangle(x, y, width, height);
            PaintByStyle(fill, stroke);
            Cs.Restore();
        }

        public void DrawRoundedRectangle(double x, double y, double width, double height, double radius,
            Color? fill = null, Color? stroke = null, double strokeWidth = 1)
        {
            if (fill is null && stroke is null) return;
            Cs.Save();
            ApplyFillStroke(fill, stroke, strokeWidth);
            TraceRoundedRect(Cs, x, y, width, height, radius);
            PaintByStyle(fill, stroke);
            Cs.Restore();
        }

        public void DrawCircle(double cx, double cy, double radius,
            Color? fill = null, Color? stroke = null, double strokeWidth = 1)
        {
            if (fill is null && stroke is null) return;
            Cs.Save();
            ApplyFillStroke(fill, stroke, strokeWidth);
            Cs.Circle(cx, cy, radius);
            PaintByStyle(fill, stroke);
            Cs.Restore();
        }

        public void DrawEllipse(double cx, double cy, double rx, double ry,
            Color? fill = null, Color? stroke = null, double strokeWidth = 1)
        {
            if (fill is null && stroke is null) return;
            Cs.Save();
            ApplyFillStroke(fill, stroke, strokeWidth);
            Cs.Ellipse(cx, cy, rx, ry);
            PaintByStyle(fill, stroke);
            Cs.Restore();
        }

        public void DrawLine(double x1, double y1, double x2, double y2,
            Color stroke, double strokeWidth = 1)
        {
            Cs.Save();
            Cs.SetRgbStroke(stroke.R, stroke.G, stroke.B).SetLineWidth(strokeWidth);
            Cs.MoveTo(x1, y1).LineTo(x2, y2).Stroke();
            Cs.Restore();
        }

        public void DrawPolygon(ReadOnlySpan<Point> points,
            Color? fill = null, Color? stroke = null, double strokeWidth = 1)
        {
            if (points.Length == 0 || (fill is null && stroke is null)) return;
            Cs.Save();
            ApplyFillStroke(fill, stroke, strokeWidth);
            Cs.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) Cs.LineTo(points[i].X, points[i].Y);
            Cs.ClosePath();
            PaintByStyle(fill, stroke);
            Cs.Restore();
        }

        public void DrawPolyline(ReadOnlySpan<Point> points, Color stroke, double strokeWidth = 1)
        {
            if (points.Length == 0) return;
            Cs.Save();
            Cs.SetRgbStroke(stroke.R, stroke.G, stroke.B).SetLineWidth(strokeWidth);
            Cs.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) Cs.LineTo(points[i].X, points[i].Y);
            Cs.Stroke();
            Cs.Restore();
        }

        // ---- shadings ----

        public void PaintShading(PdfReference shading)
        {
            _canvas.RegisterShading(shading, out var name);
            Cs.PaintShading(name);
        }

        public void PaintShading(string registeredName) => Cs.PaintShading(registeredName);

        // ---- text state setters ----

        public void SetFont(Font font, double size) => Cs.SetFont(_canvas.UseFont(font), size);
        public void SetCharSpacing(double tc) => Cs.SetCharSpacing(tc);
        public void SetWordSpacing(double tw) => Cs.SetWordSpacing(tw);
        public void SetHorizontalScaling(double percent) => Cs.SetHorizontalScaling(percent);
        public void SetLeading(double leading) => Cs.SetLeading(leading);
        public void SetTextRise(double rise) => Cs.SetTextRise(rise);
        public void SetTextRenderMode(TextRenderMode mode) => Cs.SetTextRenderMode((int)mode);

        // ---- atomic text helpers ----

        public void DrawText(Font font, double size, double x, double baselineY, string text) =>
            Cs.DrawText(_canvas.UseFont(font), size, x, baselineY, text);

        public void DrawTextCentered(Font font, double size, double centerX, double baselineY, string text) =>
            Cs.DrawText(_canvas.UseFont(font), size, centerX - font.MeasureText(text, size) / 2, baselineY, text);

        public void DrawTextRight(Font font, double size, double rightX, double baselineY, string text) =>
            Cs.DrawText(_canvas.UseFont(font), size, rightX - font.MeasureText(text, size), baselineY, text);

        public double DrawWrappedText(Font font, double size, double x, double baselineY,
            double maxWidth, double leading, string text) =>
            Cs.DrawWrappedText(_canvas.UseFont(font), font.BaseFont, size, x, baselineY, maxWidth, leading, text);

        // ---- XObject painting ----

        public void DrawImage(PdfImage image, double x, double y, double width, double height) =>
            _canvas.EmitImage(image, x, y, width, height);

        public void DrawForm(FormXObject form, double x, double y) => DrawForm(form, x, y, 1, 1);
        public void DrawForm(FormXObject form, double x, double y, double scale) => DrawForm(form, x, y, scale, scale);
        public void DrawForm(FormXObject form, double x, double y, double sx, double sy)
        {
            string name = _canvas.UseForm(form);
            Cs.Save().Transform(sx, 0, 0, sy, x, y).PaintXObject(name).Restore();
        }

        // Reuse-component painting: same model as DrawForm but routed through
        // the canvas's EmitComponent helper so doc-level dedup is honoured.
        public void DrawComponent(ReuseComponent component, double x, double y) =>
            _canvas.EmitComponent(component, x, y, 1, 1);
        public void DrawComponent(ReuseComponent component, double x, double y, double scale) =>
            _canvas.EmitComponent(component, x, y, scale, scale);
        public void DrawComponent(ReuseComponent component, double x, double y, double sx, double sy) =>
            _canvas.EmitComponent(component, x, y, sx, sy);

        public void PaintXObject(string name) => Cs.PaintXObject(name);

        // ---- helpers ----

        private void ApplyFillStroke(Color? fill, Color? stroke, double strokeWidth)
        {
            if (fill is { } f) Cs.SetRgbFill(f.R, f.G, f.B);
            if (stroke is { } s) Cs.SetRgbStroke(s.R, s.G, s.B).SetLineWidth(strokeWidth);
        }

        private void PaintByStyle(Color? fill, Color? stroke)
        {
            if (fill is not null && stroke is not null) Cs.FillStroke();
            else if (fill is not null) Cs.Fill();
            else Cs.Stroke();
        }
    }

    /// <summary>
    /// PdfTextObject scope returned by <see cref="Text"/>. Constructor emits
    /// BT; Dispose emits ET.
    /// </summary>
    private sealed class TextObjectScope : PdfTextObject
    {
        private readonly PdfCanvas _canvas;
        private bool _disposed;

        public TextObjectScope(PdfCanvas canvas) => _canvas = canvas;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _canvas._cs.EndText();
        }

        // Text state
        public void SetFont(Font font, double size) =>
            _canvas._cs.SetFont(_canvas.UseFont(font), size);
        public void SetCharSpacing(double tc) => _canvas._cs.SetCharSpacing(tc);
        public void SetWordSpacing(double tw) => _canvas._cs.SetWordSpacing(tw);
        public void SetHorizontalScaling(double percent) => _canvas._cs.SetHorizontalScaling(percent);
        public void SetLeading(double leading) => _canvas._cs.SetLeading(leading);
        public void SetTextRise(double rise) => _canvas._cs.SetTextRise(rise);
        public void SetTextRenderMode(TextRenderMode mode) => _canvas._cs.SetTextRenderMode((int)mode);

        // Positioning
        public void SetTextMatrix(double a, double b, double c, double d, double e, double f) =>
            _canvas._cs.SetTextMatrix(a, b, c, d, e, f);
        public void MoveText(double tx, double ty) => _canvas._cs.MoveText(tx, ty);
        public void MoveTextSetLeading(double tx, double ty) => _canvas._cs.MoveTextSetLeading(tx, ty);
        public void NextLine() => _canvas._cs.NextLine();

        // Showing
        public void ShowText(string text) => _canvas._cs.ShowText(text);
        public void NextLineShowText(string text) => _canvas._cs.NextLineShowText(text);
        public void NextLineShowText(double wordSpacing, double charSpacing, string text) =>
            _canvas._cs.NextLineShowText(wordSpacing, charSpacing, text);
        public void ShowTextWithKerning(params object[] items) =>
            _canvas._cs.ShowTextWithKerning(items);

        // Colour
        public void SetFillGray(double gray) => _canvas._cs.SetGrayFill(gray);
        public void SetStrokeGray(double gray) => _canvas._cs.SetGrayStroke(gray);
        public void SetFillRgb(double r, double g, double b) => _canvas._cs.SetRgbFill(r, g, b);
        public void SetStrokeRgb(double r, double g, double b) => _canvas._cs.SetRgbStroke(r, g, b);
        public void SetFillCmyk(double c, double m, double y, double k) =>
            _canvas._cs.SetCmykFill(c, m, y, k);
        public void SetStrokeCmyk(double c, double m, double y, double k) =>
            _canvas._cs.SetCmykStroke(c, m, y, k);
        public void SetFillColor(Color color) => _canvas._cs.SetRgbFill(color.R, color.G, color.B);
        public void SetStrokeColor(Color color) => _canvas._cs.SetRgbStroke(color.R, color.G, color.B);

        // Marked content (nested stays inside the text object)
        public void MarkedContent(string tag, Action<PdfTextObject> body)
        {
            _canvas._cs.BeginMarkedContent(tag);
            try { body(this); }
            finally { _canvas._cs.EndMarkedContent(); }
        }

        public void MarkedContent(string tag, PdfDictionary properties, Action<PdfTextObject> body)
        {
            _canvas._cs.BeginMarkedContent(tag, properties);
            try { body(this); }
            finally { _canvas._cs.EndMarkedContent(); }
        }

        public void MarkPoint(string tag) => _canvas._cs.MarkPoint(tag);
        public void MarkPoint(string tag, PdfDictionary properties) =>
            _canvas._cs.MarkPoint(tag, properties);
    }

    /// <summary>
    /// PdfPath sub-state handed to build-then-paint callbacks. Construction
    /// only — the enclosing graphics method emits the terminator after the
    /// callback returns.
    /// </summary>
    private sealed class PathSurface : PdfPath
    {
        private readonly PdfCanvas _canvas;
        public PathSurface(PdfCanvas canvas) => _canvas = canvas;

        public void MoveTo(double x, double y) => _canvas._cs.MoveTo(x, y);
        public void LineTo(double x, double y) => _canvas._cs.LineTo(x, y);
        public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3) =>
            _canvas._cs.CurveTo(x1, y1, x2, y2, x3, y3);
        public void CurveToV(double x2, double y2, double x3, double y3) =>
            _canvas._cs.CurveToV(x2, y2, x3, y3);
        public void CurveToY(double x1, double y1, double x3, double y3) =>
            _canvas._cs.CurveToY(x1, y1, x3, y3);
        public void ClosePath() => _canvas._cs.ClosePath();
        public void Rectangle(double x, double y, double width, double height) =>
            _canvas._cs.Rectangle(x, y, width, height);
        public void Circle(double cx, double cy, double r) => _canvas._cs.Circle(cx, cy, r);
        public void Ellipse(double cx, double cy, double rx, double ry) =>
            _canvas._cs.Ellipse(cx, cy, rx, ry);
        public void RoundedRectangle(double x, double y, double width, double height, double radius) =>
            TraceRoundedRect(_canvas._cs, x, y, width, height, radius);
        public void Polygon(ReadOnlySpan<Point> points)
        {
            if (points.Length == 0) return;
            _canvas._cs.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) _canvas._cs.LineTo(points[i].X, points[i].Y);
            _canvas._cs.ClosePath();
        }
        public void Polyline(ReadOnlySpan<Point> points)
        {
            if (points.Length == 0) return;
            _canvas._cs.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) _canvas._cs.LineTo(points[i].X, points[i].Y);
        }
    }
}

// ===== Supporting enums ===============================================

public enum LineCap { Butt = 0, Round = 1, Square = 2 }
public enum LineJoin { Miter = 0, Round = 1, Bevel = 2 }
public enum FillRule { NonZero, EvenOdd }
public enum RenderingIntent { AbsoluteColorimetric, RelativeColorimetric, Saturation, Perceptual }

/// <summary>Tr operator values: combinations of fill / stroke / clip on glyphs.</summary>
public enum TextRenderMode
{
    Fill = 0, Stroke = 1, FillStroke = 2, Invisible = 3,
    FillClip = 4, StrokeClip = 5, FillStrokeClip = 6, Clip = 7,
}

public enum BlendMode
{
    Normal, Multiply, Screen, Overlay, Darken, Lighten,
    ColorDodge, ColorBurn, HardLight, SoftLight, Difference, Exclusion,
}
