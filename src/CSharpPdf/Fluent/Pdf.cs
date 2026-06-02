using CSharpPdf.Geometry;
using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>
/// Entry point for the fluent API. <c>Pdf.Create()</c> returns a
/// <see cref="Document"/> you chain page-level settings, header / footer,
/// and content lambdas onto, ending with <c>.Save(path)</c>:
/// <code>
/// Pdf.Create()
///    .PageSize(PageSizes.A4)
///    .Margin(54)
///    .Header(c => c.Text("Title").Bold().FontSize(20))
///    .Footer(c => c.AlignCenter().PageNumber("Page {0} of {1}"))
///    .Content(c => c.Column(col =>
///    {
///        col.Item().Text("Welcome");
///        col.Item().Image(rgb, w, h);
///    }))
///    .Save("out.pdf");
/// </code>
/// </summary>
public static class Pdf
{
    public static Document Create() => new();
}

/// <summary>
/// Document-level builder. Holds the page settings, the header / footer / content
/// lambdas, and orchestrates the single-pass <c>LayoutEngine.Save</c> call.
/// The lambdas are stored and replayed once during save — they're not executed
/// at registration time.
/// </summary>
public sealed class Document
{
    private readonly PdfDoc _doc;
    private readonly LayoutEngine _engine;
    private System.Action<Container>? _headerBuild;
    private System.Action<Container>? _footerBuild;
    private readonly System.Collections.Generic.List<System.Action<Container>> _contentBuilds = new();

    internal Document()
    {
        _doc = new PdfDoc();
        _engine = new LayoutEngine(_doc) { PageSize = PageSizes.Letter, Margin = 54 };
    }

    /// <summary>The underlying PdfDoc (read-only access for advanced scenarios).</summary>
    public PdfDoc PdfDoc => _doc;

    /// <summary>The underlying LayoutEngine (read-only access for advanced scenarios).</summary>
    public LayoutEngine Engine => _engine;

    // ===== Page setup =====

    public Document PageSize(PdfRectangle size) { _engine.PageSize = size; return this; }
    public Document Margin(double margin) { _engine.Margin = margin; return this; }

    // ===== Header / footer / content =====

    /// <summary>Element drawn at the top of every page (re-rendered per page).</summary>
    public Document Header(System.Action<Container> build) { _headerBuild = build; return this; }

    /// <summary>Element drawn at the bottom of every page (re-rendered per page).</summary>
    public Document Footer(System.Action<Container> build) { _footerBuild = build; return this; }

    /// <summary>
    /// A content section. May be called multiple times — sections flow in order
    /// and paginate naturally. Use a single <c>Content</c> with a <c>Column</c>
    /// inside if you want the layout to compose nested sections.
    /// </summary>
    public Document Content(System.Action<Container> build) { _contentBuilds.Add(build); return this; }

    // ===== Save =====

    /// <summary>
    /// Run the layout once (single-phase model) and write the resulting PDF
    /// to <paramref name="path"/>. Dynamic content (page numbers, anchor
    /// references) is captured during the pass and patched in after the page
    /// count is final — see <see cref="PdfCanvas.Defer"/>.
    /// </summary>
    public void Save(string path)
    {
        _engine.Save(path, eng =>
        {
            if (_headerBuild is not null)
            {
                var hc = new Container();
                _headerBuild(hc);
                if (hc.Slot.Content is not null) eng.Header = hc.Slot;
            }
            if (_footerBuild is not null)
            {
                var fc = new Container();
                _footerBuild(fc);
                if (fc.Slot.Content is not null) eng.Footer = fc.Slot;
            }
            foreach (var build in _contentBuilds)
            {
                var c = new Container();
                build(c);
                if (c.Slot.Content is not null) eng.Add(c.Slot);
            }
        });
    }
}
