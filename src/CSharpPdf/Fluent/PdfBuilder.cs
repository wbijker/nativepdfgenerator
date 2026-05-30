using CSharpPdf.Geometry;
using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>QuestPDF-style entry point. <c>PdfBuilder.Create()</c> returns a <see cref="DocumentBuilder"/>.</summary>
public static class PdfBuilder
{
    public static DocumentBuilder Create() => new();
}

/// <summary>
/// Wraps a <see cref="PdfDocument"/> and a <see cref="LayoutEngine"/>: lets the
/// caller set page size / margin, build a header and a footer, then add one or
/// more content sections — all by handing a <see cref="FluentContainer"/> to a
/// lambda. The underlying programmatic API does all the work.
/// </summary>
public sealed class DocumentBuilder
{
    private readonly PdfDocument _doc;
    private readonly LayoutEngine _engine;

    public DocumentBuilder()
    {
        _doc = new PdfDocument();
        _engine = new LayoutEngine(_doc) { PageSize = PageSizes.Letter, Margin = 54 };
    }

    public PdfDocument Document => _doc;
    public LayoutEngine Engine => _engine;

    public DocumentBuilder PageSize(PdfRectangle size) { _engine.PageSize = size; return this; }
    public DocumentBuilder Margin(double margin) { _engine.Margin = margin; return this; }

    public DocumentBuilder Header(System.Action<FluentContainer> build)
    {
        var c = new FluentContainer();
        build(c);
        _engine.Header = c.Slot.Content is null ? c.Slot : c.Slot;
        return this;
    }

    public DocumentBuilder Footer(System.Action<FluentContainer> build)
    {
        var c = new FluentContainer();
        build(c);
        _engine.Footer = c.Slot;
        return this;
    }

    /// <summary>Add a section. May be called multiple times — sections flow in order.</summary>
    public DocumentBuilder Content(System.Action<FluentContainer> build)
    {
        var c = new FluentContainer();
        build(c);
        // If the user only set styling on the root and no content method was called,
        // there's nothing to add. Otherwise hand the slot (with its content) to the engine.
        if (c.Slot.Content is not null) _engine.Add(c.Slot);
        return this;
    }

    /// <summary>Finalise outline (from BookmarkElements) and write the PDF.</summary>
    public void Save(string path)
    {
        _engine.Finish();
        _doc.Save(path);
    }
}
