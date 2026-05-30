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
    public double Margin { get; set; } = 54;

    /// <summary>Drawn at the top of every page (re-rendered per page).</summary>
    public UIElement? Header { get; set; }

    /// <summary>Drawn at the bottom of every page (re-rendered per page).</summary>
    public UIElement? Footer { get; set; }

    private readonly PdfContext _context;
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
        while (current is not null)
        {
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
                if (!progressed && _atPageTop)
                {
                    throw new InvalidOperationException(
                        "A component does not fit on an empty page; it cannot be paginated.");
                }
                NewPage();
                current = overflow;
                continue;
            }
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

    private void NewPage()
    {
        _context.Page = _context.Document.AddPage(PageSize);
        _context.PageNumber++;

        double headerHeight = 0;
        double footerHeight = 0;
        if (Header is not null)
        {
            headerHeight = Header.Measure(new Size(ContentWidth, double.MaxValue)).Height;
        }
        if (Footer is not null)
        {
            footerHeight = Footer.Measure(new Size(ContentWidth, double.MaxValue)).Height;
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
