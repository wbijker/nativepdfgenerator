using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Objects;
using ImperativeDoc = PdfSpec.PdfDoc;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent document builder — wraps an imperative <see cref="ImperativeDoc"/>
/// and exposes chainable setters for the doc-level metadata + default
/// font / page size + page addition. <see cref="Save(string)"/> /
/// <see cref="Save(Stream)"/> are the terminal calls.
///
/// <para>
/// Two ways to add a page: a direct one-shot
/// <see cref="AddPage(Element, Element?, Element?)"/> for the common
/// case (one body, optional header / footer), and a closure form
/// <see cref="AddPage(Action{PdfPage})"/> for multi-body pages and
/// fine-grained per-page configuration.
/// </para>
/// </summary>
public sealed class PdfDoc
{
    private readonly ImperativeDoc _doc = new();

    private PdfDoc() { }

    /// <summary>Start a new fluent document.</summary>
    public static PdfDoc Create() => new();

    /// <summary>The underlying imperative document. Internal — the fluent layer keeps the imperative side hidden from callers.</summary>
    internal ImperativeDoc Imperative => _doc;

    public PdfDoc Info(string? title = null, string? creator = null, string? producer = null,
        string? subject = null, string? author = null, string? keywords = null)
    {
        if (title is not null)    _doc.Info.Title = title;
        if (creator is not null)  _doc.Info.Creator = creator;
        if (producer is not null) _doc.Info.Producer = producer;
        if (subject is not null)  _doc.Info.Subject = subject;
        if (author is not null)   _doc.Info.Author = author;
        if (keywords is not null) _doc.Info.Keywords = keywords;
        return this;
    }

    public PdfDoc DefaultFont(Font font, double size)
    {
        _doc.SetDefaultFont(font, size);
        return this;
    }

    public PdfDoc DefaultPageSize(PdfRectangle mediaBox)
    {
        _doc.DefaultMediaBox = mediaBox;
        return this;
    }

    /// <summary>
    /// Add a page with a single <paramref name="body"/> element and the
    /// (optional) <paramref name="header"/> / <paramref name="footer"/>
    /// shared chrome. The body paginates across overflow PDF pages, and
    /// the chrome rebuilds fresh on each one.
    /// </summary>
    public PdfDoc AddPage(Element body, Element? header = null, Element? footer = null)
    {
        var page = _doc.AddPage();
        if (header is not null) page.Header = header.Build();
        if (footer is not null) page.Footer = footer.Build();
        page.Body(body.Build());
        return this;
    }

    /// <summary>
    /// Add a page configured through a closure — set
    /// <see cref="PdfPage.Header"/> / <see cref="PdfPage.Footer"/>, add
    /// one or more bodies via <see cref="PdfPage.AddBody(Element)"/>.
    /// Bodies render once the closure returns.
    /// </summary>
    public PdfDoc AddPage(Action<PdfPage> configure)
    {
        var page = new PdfPage(this, _doc.AddPage());
        configure(page);
        page.Flush();
        return this;
    }

    /// <summary>
    /// Register a named destination resolving to <paramref name="pageIndex"/>
    /// (0-based) with the given <paramref name="fit"/> zoom mode (default
    /// <c>"Fit"</c>). The name can be referenced by
    /// <c>Navigation.PdfAction.GoToNamed(name)</c> from anywhere in the
    /// document.
    /// </summary>
    public PdfDoc AddNamedDestination(string name, int pageIndex, string fit = "Fit")
    {
        _doc.AddNamedDestination(name,
            new PdfArray(_doc.Pages[pageIndex].Reference, new PdfName(fit)));
        return this;
    }

    /// <summary>
    /// Build an explicit destination array <c>[pageRef /<paramref name="fit"/>]</c>
    /// for <paramref name="pageIndex"/> (0-based). Pass directly to
    /// <c>Navigation.PdfAction.GoTo(...)</c> to wire a link target without
    /// reaching into the imperative page list.
    /// </summary>
    public PdfArray PageDestination(int pageIndex, string fit = "Fit") =>
        new(_doc.Pages[pageIndex].Reference, new PdfName(fit));

    public void Save(string path) => _doc.Save(path);
    public void Save(Stream stream) => _doc.Save(stream);
}
