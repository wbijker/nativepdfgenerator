using CSharpPdf.Geometry;

namespace CSharpPdf.Layout;

/// <summary>
/// Drives layout: owns the page cursor and remaining space, hands each element a
/// <see cref="PdfContext"/> positioned at the cursor plus the space available, then
/// advances by the returned next position and starts a new page to render any
/// overflow. Optionally draws a header at the top and a footer at the bottom of
/// every page, with the content area between them. Guards against a component that
/// can never fit on an empty page.
/// </summary>
public sealed class LayoutEngine
{
    public PdfDocument Document { get; }
    public PdfRectangle PageSize { get; set; } = PageSizes.Letter;
    public double Margin { get; set; } = 0;

    /// <summary>Drawn at the top of every page (re-rendered per page).</summary>
    public UIElement? Header { get; set; }

    /// <summary>Drawn at the bottom of every page (re-rendered per page).</summary>
    public UIElement? Footer { get; set; }

    private PdfContext _context;
    private double _cursorTop;
    private double _contentBottom;
    private bool _atPageTop;

    public LayoutEngine(PdfDocument document)
    {
        Document = document;
        _context = new PdfContext(document);
    }

    public int PageNumber => _context.PageNumber;

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
            LayoutTrace.Mark($"Engine.Add iter={iter} page={_context.PageNumber} cursorTop={_cursorTop:F1} type={current.GetType().Name}");
            _context.Cursor = new Point(ContentLeft, _cursorTop);
            var available = new Size(ContentWidth, _cursorTop - _contentBottom);
            var result = current.Render(_context, available);

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
                    if (!_context.ForceRender)
                    {
                        _context.ForceRender = true;
                        current = overflow;
                        continue;
                    }
                    _context.ForceRender = false;
                    current = null;
                    continue;
                }
                _context.ForceRender = false;
                NewPage();
                current = overflow;
                continue;
            }
            _context.ForceRender = false;
            _atPageTop = false;
            current = null;
        }
    }

    private void EnsurePage()
    {
        if (_context.Page is null)
        {
            NewPage();
        }
    }

    /// <summary>
    /// Two-phase save: run <paramref name="build"/> once in measure mode against a
    /// throwaway document to count pages and capture document-level totals, then
    /// run it again in render mode against the real document with
    /// <see cref="PdfContext.TotalPages"/> populated so "Page X of Y" footers
    /// resolve. The build delegate should construct UI element trees freshly each
    /// call (since the throwaway document is dropped between phases).
    /// </summary>
    public void SaveTwoPhase(string path, System.Action<LayoutEngine> build)
    {
        // Shared between the two phases so values captured during measure are
        // still available for lookup during render.
        var captured = new Dictionary<string, object>();

        // Phase 1: measure pass against a throwaway document.
        var throwaway = new PdfDocument();
        _context = new PdfContext(throwaway) { Mode = RenderMode.Measure, Captured = captured };
        ResetForPhase();
        build(this);
        int totalPages = _context.PageNumber;

        // Phase 2: real render against the engine's actual document.
        _context = new PdfContext(Document)
        {
            Mode = RenderMode.Render,
            TotalPages = totalPages,
            Captured = captured,
        };
        ResetForPhase();
        build(this);

        Finish();
        Document.Save(path);
    }

    private void ResetForPhase()
    {
        _context.Page = null!;
        _context.PageNumber = 0;
        _context.PendingBookmarks.Clear();
        _cursorTop = 0;
        _contentBottom = 0;
        _atPageTop = false;
    }

    /// <summary>
    /// Flush any collected bookmarks into a document outline. Call this before
    /// <c>PdfDocument.Save</c> so the resulting PDF carries the outline tree.
    /// </summary>
    public void Finish()
    {
        if (_context.PendingBookmarks.Count > 0)
        {
            var items = new List<Navigation.PdfOutlineItem>(_context.PendingBookmarks.Count);
            foreach (var (title, dest) in _context.PendingBookmarks)
            {
                items.Add(new Navigation.PdfOutlineItem(title, dest));
            }
            Document.SetOutline(items);
        }
    }

    private void NewPage()
    {
        _context.Page = _context.Document.AddPage(PageSize);
        _context.PageNumber++;

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
            _context.Cursor = new Point(ContentLeft, PageTop);
            Header.Render(_context, new Size(ContentWidth, headerHeight));
        }
        if (Footer is not null)
        {
            _context.Cursor = new Point(ContentLeft, PageBottom + footerHeight);
            Footer.Render(_context, new Size(ContentWidth, footerHeight));
        }

        _cursorTop = PageTop - headerHeight;
        _contentBottom = PageBottom + footerHeight;
        _atPageTop = true;
    }
}
