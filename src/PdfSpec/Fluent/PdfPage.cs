using ImperativeElement = PdfSpec.Layout.Element;
using ImperativePage = PdfSpec.PdfPage;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent page builder — wraps an imperative <see cref="ImperativePage"/>
/// and accumulates header / footer / body elements during the
/// <see cref="PdfDoc.AddPage(Action{PdfPage})"/> closure. The collected
/// bodies are committed in a single <see cref="ImperativePage.Body"/>
/// call when <see cref="Flush"/> runs at end of closure.
/// </summary>
public sealed class PdfPage
{
    private readonly ImperativePage _page;
    private readonly List<ImperativeElement> _bodies = new();

    internal PdfPage(ImperativePage page) => _page = page;

    /// <summary>Hand back the underlying imperative page. Use only when escaping the fluent layer.</summary>
    public ImperativePage UnderlyingPage => _page;

    public PdfPage Header(Element element)
    {
        _page.Header = element.Build();
        return this;
    }

    public PdfPage Footer(Element element)
    {
        _page.Footer = element.Build();
        return this;
    }

    public PdfPage AddBody(Element element)
    {
        _bodies.Add(element.Build());
        return this;
    }

    public PdfPage AddBody(params Element[] elements)
    {
        foreach (var e in elements) _bodies.Add(e.Build());
        return this;
    }

    /// <summary>Commit accumulated bodies to the underlying page. Called by <see cref="PdfDoc.AddPage(Action{PdfPage})"/>.</summary>
    internal void Flush()
    {
        if (_bodies.Count > 0) _page.Body(_bodies.ToArray());
    }
}
