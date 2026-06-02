using CSharpPdf.Content;
using CSharpPdf.Geometry;

namespace CSharpPdf.Layout;

/// <summary>
/// Drives layout: owns the page cursor and remaining space, hands each element a
/// <see cref="PdfCanvas"/> positioned at the cursor plus the space available, then
/// advances by the returned next position and starts a new page to render any
/// overflow. Optionally draws a header at the top and a footer at the bottom of
/// every page, with the content area between them. Treats components that don't
/// fit on a fresh page as a hint, not an error — flips ForceRender for one retry
/// and otherwise drops them rather than throwing.
/// </summary>
public sealed class LayoutEngine
{
    public PdfDoc Document { get; }
    public PdfRectangle PageSize { get; set; } = PageSizes.Letter;
    public double Margin { get; set; } = 0;

    /// <summary>Drawn at the top of every page (re-rendered per page).</summary>
    public UIElement? Header { get; set; }

    /// <summary>Drawn at the bottom of every page (re-rendered per page).</summary>
    public UIElement? Footer { get; set; }

    // The current per-page canvas. Reassigned every time NewPage runs; its
    // Captured / PendingBookmarks / Mode / TotalPages are wired to the
    // persistent state dictionaries below so cross-page accumulation works.
    private PdfCanvas? _canvas;

    // Persistent engine state — survives page transitions. Each new PdfCanvas
    // is initialised with these references.
    private PdfDoc _activeDoc;
    private RenderMode _mode = RenderMode.Render;
    private int _pageNumber;
    private int _totalPages;
    private Dictionary<string, object> _captured = new();
    private readonly List<(string Title, Objects.PdfArray Destination)> _pendingBookmarks = new();
    // Engine-owned deferred-render queue. Wired into each new PdfCanvas so
    // every depth of the canvas tree shares the same list. Drained after the
    // single build pass completes.
    private readonly List<(PdfCanvas Sub, Action<PdfCanvas> Render)> _deferredRenders = new();

    private double _cursorTop;
    private double _contentBottom;
    private bool _atPageTop;

    // Shared "layout image" counter that survives page transitions — matches
    // the pre-refactor PdfContext behaviour where DrawImage names accumulated
    // across the document.
    private int _layoutImgSeq;

    public LayoutEngine(PdfDoc document)
    {
        Document = document;
        _activeDoc = document;
    }

    public int PageNumber => _pageNumber;

    private double ContentLeft => PageSize.Left + Margin;
    private double ContentWidth => PageSize.Width - 2 * Margin;
    private double PageTop => PageSize.Top - Margin;
    private double PageBottom => PageSize.Bottom + Margin;

    /// <summary>Place an element, flowing onto new pages as needed.</summary>
    public void Add(UIElement element)
    {
        EnsurePage();

        UIElement? current = element;
        int iter = 0;
        while (current is not null)
        {
            // Page break: skip rendering, start a fresh page (unless already at top).
            if (current is PageBreakElement)
            {
                if (!_atPageTop) NewPage();
                current = null;
                continue;
            }
            iter++;
            LayoutTrace.Mark($"Engine.Add iter={iter} page={_pageNumber} cursorTop={_cursorTop:F1} type={current.GetType().Name}");
            _canvas!.Cursor = new Point(ContentLeft, _cursorTop);
            var available = new Size(ContentWidth, _cursorTop - _contentBottom);
            var result = current.Render(_canvas, available);

            bool progressed = result.Next.Y < _cursorTop - 0.01;
            if (progressed)
            {
                _atPageTop = false;
            }
            _cursorTop = result.Next.Y;

            if (result.Overflow is { } overflow)
            {
                int rowsCount = overflow is RowsElement re ? re.Slots.Count : -1;
                LayoutTrace.Mark($"Engine overflow type={overflow.GetType().Name} rows.Slots={rowsCount} nextY={result.Next.Y:F1} progressed={progressed}");
                if (!progressed && _atPageTop)
                {
                    // Deferred on a fresh empty page — there is nowhere to defer
                    // to. Treat VerticalBreakable=false as a hint, not a rule:
                    // flip ForceRender so the same element renders here anyway
                    // (its own RenderCore now decides what to draw and what to
                    // hand back as a continuation). If even the forced retry
                    // makes no progress, drop the element so the engine never
                    // hangs or throws.
                    if (!_canvas.ForceRender)
                    {
                        _canvas.ForceRender = true;
                        current = overflow;
                        continue;
                    }
                    _canvas.ForceRender = false;
                    current = null;
                    continue;
                }
                _canvas.ForceRender = false;
                NewPage();
                current = overflow;
                continue;
            }
            _canvas.ForceRender = false;
            _atPageTop = false;
            current = null;
        }
    }

    private void EnsurePage()
    {
        if (_canvas is null)
        {
            NewPage();
        }
    }

    /// <summary>
    /// Single-pass save with deferred patches for document-wide state.
    /// <paramref name="build"/> runs exactly once, populating the document
    /// page-by-page; elements that need a finalised view of the layout
    /// (page numbers, anchor positions) call <see cref="PdfCanvas.Defer"/>
    /// instead of relying on a measure pass — those closures are replayed
    /// here, in registration order, after <see cref="PdfCanvas.TotalPages"/>
    /// is set to the final page count and every <see cref="NamedAnchorElement"/>
    /// has recorded its page into the capture store.
    /// </summary>
    public void Save(string path, System.Action<LayoutEngine> build)
    {
        _activeDoc = Document;
        _mode = RenderMode.Render;
        _captured = new Dictionary<string, object>();
        _deferredRenders.Clear();
        ResetForPhase();

        // Main pass: build the document. Each NamedAnchorElement records its
        // page into _captured as it's rendered; each PageNumberElement /
        // PageReferenceElement reserves space and queues a closure here in
        // _deferredRenders without drawing anything.
        build(this);

        // Finish() drains the deferred queue (and flushes the outline).
        Finish();
        Document.Save(path);
    }

    /// <summary>
    /// Back-compat wrapper for the historical two-phase API. Delegates to
    /// <see cref="Save"/>; the implementation is single-pass with deferred
    /// regions for content that depends on the finalised page count.
    /// </summary>
    public void SaveTwoPhase(string path, System.Action<LayoutEngine> build) =>
        Save(path, build);

    private void ResetForPhase()
    {
        _canvas = null;
        _pageNumber = 0;
        _pendingBookmarks.Clear();
        _cursorTop = 0;
        _contentBottom = 0;
        _atPageTop = false;
        _layoutImgSeq = 0;
    }

    /// <summary>
    /// Wrap up the layout pass: replay every deferred-render closure that was
    /// queued during the build (with <see cref="PdfCanvas.TotalPages"/> set to
    /// the final page count), then flush collected bookmarks into the
    /// document outline. Call this before <c>PdfDoc.Save</c>.
    /// </summary>
    public void Finish()
    {
        // Drain the deferred queue. By now _pageNumber is the final total —
        // make it visible on every queued sub-canvas before its closure runs,
        // then clear so subsequent Finish() calls are no-ops.
        if (_deferredRenders.Count > 0)
        {
            _totalPages = _pageNumber;
            foreach (var (sub, render) in _deferredRenders)
            {
                sub.TotalPages = _totalPages;
                render(sub);
            }
            _deferredRenders.Clear();
        }

        if (_pendingBookmarks.Count > 0)
        {
            var items = new List<Navigation.PdfOutlineItem>(_pendingBookmarks.Count);
            foreach (var (title, dest) in _pendingBookmarks)
            {
                items.Add(new Navigation.PdfOutlineItem(title, dest));
            }
            Document.SetOutline(items);
        }
    }

    private void NewPage()
    {
        // Persist the layout-image counter from the outgoing canvas so it
        // continues across the page break rather than restarting at 1.
        if (_canvas is not null)
        {
            _layoutImgSeq = _canvas.SeqBox[4];
        }

        // Build a fresh PdfCanvas for the new page and wire its persistent
        // state (Mode / counters / capture store / bookmarks queue) back to
        // the engine-owned dictionaries so accumulation survives page breaks.
        var page = _activeDoc.AddPage(PageSize);
        _pageNumber++;
        _canvas = new PdfCanvas(page, _activeDoc)
        {
            Mode = _mode,
            PageNumber = _pageNumber,
            TotalPages = _totalPages,
            Captured = _captured,
            PendingBookmarks = _pendingBookmarks,
            DeferredRenders = _deferredRenders,
        };
        _canvas.SeqBox[4] = _layoutImgSeq;

        double headerHeight = 0;
        double footerHeight = 0;
        if (Header is not null)
        {
            headerHeight = Header.SpaceHint(new SizeRect(ContentWidth, null)).Recommended.Height ?? 0;
        }
        if (Footer is not null)
        {
            footerHeight = Footer.SpaceHint(new SizeRect(ContentWidth, null)).Recommended.Height ?? 0;
        }

        if (Header is not null)
        {
            _canvas.Cursor = new Point(ContentLeft, PageTop);
            Header.Render(_canvas, new Size(ContentWidth, headerHeight));
        }
        if (Footer is not null)
        {
            _canvas.Cursor = new Point(ContentLeft, PageBottom + footerHeight);
            Footer.Render(_canvas, new Size(ContentWidth, footerHeight));
        }

        _cursorTop = PageTop - headerHeight;
        _contentBottom = PageBottom + footerHeight;
        _atPageTop = true;
    }
}
