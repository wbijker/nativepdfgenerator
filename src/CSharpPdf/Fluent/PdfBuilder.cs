using CSharpPdf.Geometry;
using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>QuestPDF-style entry point. <c>PdfBuilder.Create()</c> returns a <see cref="DocumentBuilder"/>.</summary>
public static class PdfBuilder
{
    public static DocumentBuilder Create() => new();
}

/// <summary>
/// Wraps a <see cref="PdfDoc"/> and a <see cref="LayoutEngine"/>: lets the
/// caller set page size / margin, build a header and a footer, then add one or
/// more content sections — all by handing a <see cref="FluentContainer"/> to a
/// lambda. The underlying programmatic API does all the work.
/// </summary>
public sealed class DocumentBuilder
{
    private readonly PdfDoc _doc;
    private readonly LayoutEngine _engine;
    private System.Action<FluentContainer>? _headerBuild;
    private System.Action<FluentContainer>? _footerBuild;
    private readonly System.Collections.Generic.List<System.Action<FluentContainer>> _contentBuilds = new();

    public DocumentBuilder()
    {
        _doc = new PdfDoc();
        _engine = new LayoutEngine(_doc) { PageSize = PageSizes.Letter, Margin = 54 };
    }

    public PdfDoc Document => _doc;
    public LayoutEngine Engine => _engine;

    public DocumentBuilder PageSize(PdfRectangle size) { _engine.PageSize = size; return this; }
    public DocumentBuilder Margin(double margin) { _engine.Margin = margin; return this; }

    public DocumentBuilder Header(System.Action<FluentContainer> build) { _headerBuild = build; return this; }
    public DocumentBuilder Footer(System.Action<FluentContainer> build) { _footerBuild = build; return this; }

    /// <summary>Add a section. May be called multiple times — sections flow in order.</summary>
    public DocumentBuilder Content(System.Action<FluentContainer> build) { _contentBuilds.Add(build); return this; }

    /// <summary>
    /// Save the document. Runs a two-phase render (measure → render) so any
    /// PageNumberElement with a "Page {0} of {1}" format gets the right total
    /// page count. Header / Footer / Content builders are re-invoked each phase
    /// to construct fresh element trees.
    /// </summary>
    public void Save(string path)
    {
        _engine.SaveTwoPhase(path, eng =>
        {
            if (_headerBuild is not null)
            {
                var hc = new FluentContainer();
                _headerBuild(hc);
                eng.Header = hc.Slot;
            }
            if (_footerBuild is not null)
            {
                var fc = new FluentContainer();
                _footerBuild(fc);
                eng.Footer = fc.Slot;
            }
            foreach (var build in _contentBuilds)
            {
                var c = new FluentContainer();
                build(c);
                if (c.Slot.Content is not null) eng.Add(c.Slot);
            }
        });
    }
}
