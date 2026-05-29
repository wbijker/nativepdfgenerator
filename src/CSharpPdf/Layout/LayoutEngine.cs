using CSharpPdf.Geometry;

namespace CSharpPdf.Layout;

/// <summary>
/// Drives layout: owns the page cursor and remaining space, hands each component a
/// region to draw into, advances down the page as space is consumed, and starts a
/// new page when a component does not fit (rendering any remainder there). This is
/// the page-loop "driver", not a component.
/// </summary>
public sealed class LayoutEngine
{
    private readonly PdfDocument _document;
    private readonly PdfRectangle _pageSize;
    private readonly double _margin;

    private PdfPage? _page;
    private double _cursorTop;   // PDF y where the next component's top edge goes
    private bool _atPageTop;     // true right after a page break (nothing drawn yet)
    private int _pageNumber;

    public LayoutEngine(PdfDocument document, PdfRectangle pageSize, double margin = 54)
    {
        _document = document;
        _pageSize = pageSize;
        _margin = margin;
    }

    /// <summary>The 1-based number of the current page (0 before anything is added).</summary>
    public int PageNumber => _pageNumber;

    private double ContentLeft => _pageSize.Left + _margin;
    private double ContentWidth => _pageSize.Width - 2 * _margin;
    private double ContentBottom => _pageSize.Bottom + _margin;

    /// <summary>Place a component, flowing onto new pages as needed.</summary>
    public LayoutEngine Add(Component component)
    {
        EnsurePage();

        Component? current = component;
        while (current is not null)
        {
            double availableHeight = _cursorTop - ContentBottom;
            var context = new RenderContext(_document, _page!, ContentLeft, _cursorTop, _pageNumber);
            var result = current.Render(context, new Size(ContentWidth, availableHeight));

            if (result.Status == RenderStatus.Empty)
            {
                if (_atPageTop)
                {
                    throw new InvalidOperationException(
                        "A component does not fit even on an empty page; it cannot be paginated.");
                }
                NewPage();
                continue; // retry the same component on a fresh page
            }

            _cursorTop -= result.Used.Height;
            _atPageTop = false;

            if (result.Status == RenderStatus.Partial)
            {
                NewPage();
                current = result.Remainder;
                continue;
            }

            current = null; // fully rendered
        }
        return this;
    }

    private void EnsurePage()
    {
        if (_page is null)
        {
            NewPage();
        }
    }

    private void NewPage()
    {
        _page = _document.AddPage(_pageSize);
        _cursorTop = _pageSize.Top - _margin;
        _atPageTop = true;
        _pageNumber++;
    }
}
