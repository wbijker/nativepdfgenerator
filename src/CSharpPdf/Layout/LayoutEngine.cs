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
    /// Two-phase save: run <paramref name="build"/> once in measure mode against a
    /// throwaway document to count pages and capture document-level totals, then
    /// run it again in render mode against the real document with
    /// <see cref="PdfCanvas.TotalPages"/> populated so "Page X of Y" footers
    /// resolve. The build delegate should construct UI element trees freshly each
    /// call (since the throwaway document is dropped between phases).
    /// </summary>
    public void SaveTwoPhase(string path, System.Action<LayoutEngine> build)
    {
        // Shared between the two phases so values captured during measure are
        // still available for lookup during render.
        var captured = new Dictionary<string, object>();

        // Phase 1: measure pass against a throwaway document.
        var throwaway = new PdfDoc();
        _activeDoc = throwaway;
        _mode = RenderMode.Measure;
        _captured = captured;
        ResetForPhase();
        build(this);
        int totalPages = _pageNumber;

        // Phase 2: real render against the engine's actual document.
        _activeDoc = Document;
        _mode = RenderMode.Render;
        _totalPages = totalPages;
        _captured = captured;
        ResetForPhase();
        build(this);

        Finish();
        Document.Save(path);
    }

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
    /// Flush any collected bookmarks into a document outline. Call this before
    /// <c>PdfDoc.Save</c> so the resulting PDF carries the outline tree.
    /// </summary>
    public void Finish()
    {
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
