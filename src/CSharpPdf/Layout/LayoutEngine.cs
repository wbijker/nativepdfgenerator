using CSharpPdf.Geometry;

namespace CSharpPdf.Layout;

/// <summary>
/// Drives layout: owns the page cursor and remaining space, hands each element a
/// <see cref="PdfContext"/> positioned at the cursor plus the space available, then
/// advances by the returned next position and starts a new page to render any
/// overflow. Guards against a component that can never fit on an empty page.
/// </summary>
public sealed class LayoutEngine
{
    private readonly PdfRectangle _pageSize;
    private readonly double _margin;
    private readonly PdfContext _context;
    private double _cursorTop;
    private bool _atPageTop;

    public LayoutEngine(PdfDocument document, PdfRectangle pageSize, double margin = 54)
    {
        _pageSize = pageSize;
        _margin = margin;
        _context = new PdfContext(document);
    }

    public int PageNumber => _context.PageNumber;

    private double ContentLeft => _pageSize.Left + _margin;
    private double ContentWidth => _pageSize.Width - 2 * _margin;
    private double ContentBottom => _pageSize.Bottom + _margin;

    /// <summary>Render the document content (one root element), paginating as needed.</summary>
    public LayoutEngine Content(UIElement root) => Add(root);

    /// <summary>Place an element, flowing onto new pages as needed.</summary>
    public LayoutEngine Add(UIElement element)
    {
        EnsurePage();

        UIElement? current = element;
        while (current is not null)
        {
            _context.Cursor = new Point(ContentLeft, _cursorTop);
            var available = new Size(ContentWidth, _cursorTop - ContentBottom);
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
        return this;
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
        _context.Page = _context.Document.AddPage(_pageSize);
        _context.PageNumber++;
        _cursorTop = _pageSize.Top - _margin;
        _atPageTop = true;
    }
}
