using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Multi-page layout with a shared header + footer. Composes each
/// element of <see cref="Pages"/> as one PDF page wrapped in a
/// <see cref="VFrame"/>:
///
/// <list type="bullet">
/// <item><description><see cref="Header"/> → <see cref="VFrame.AddAuto"/></description></item>
/// <item><description><see cref="Pages"/>[i] → <see cref="VFrame.AddRelative"/> (one unit — absorbs the gap)</description></item>
/// <item><description><see cref="Footer"/> → <see cref="VFrame.AddAuto"/></description></item>
/// </list>
///
/// <para>
/// <see cref="Render"/> processes the first page, then if there are
/// more pages left returns a <see cref="RenderResult"/> whose
/// <see cref="RenderResult.NextElement"/> is a fresh HeaderFooterPage
/// carrying the tail. <see cref="PdfPage.Body"/>'s pagination loop
/// already handles that — it calls <c>PageBreak()</c> and re-renders
/// the continuation, so the header / footer get rebuilt fresh on
/// every page (which also means a <see cref="DeferredComponent"/>
/// in the footer registers a separate entry per page, with its own
/// page-data snapshot).
/// </para>
///
/// <para>
/// Any of the three slots may be <c>null</c>: a null Header / Footer
/// is simply not added to the frame, a null Body slot for an entry in
/// the pages list still produces a page with header + footer and an
/// empty gap between them.
/// </para>
/// </summary>
public sealed class HeaderFooterPage : Element
{
    public Element? Header { get; }
    public IReadOnlyList<Element> Pages { get; }
    public Element? Footer { get; }

    public HeaderFooterPage(Element? header, IReadOnlyList<Element> pages, Element? footer)
    {
        Header = header;
        Pages = pages;
        Footer = footer;
    }

    public HeaderFooterPage(Element? header, Element body, Element? footer)
        : this(header, new[] { body }, footer) { }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        if (Pages.Count == 0) return new PdfSizeHint(0, 0, null, null);
        return BuildFrame(Pages[0]).SizeHint(available);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        if (Pages.Count == 0) return RenderResult.Done(0);

        var frame = BuildFrame(Pages[0]);
        var result = frame.Render(cs, available);

        if (Pages.Count == 1)
        {
            return result;
        }

        // More pages left — return a Partial with a fresh
        // HeaderFooterPage carrying the tail. PdfPage.Body's
        // pagination loop calls PageBreak() and renders the
        // continuation on the next page.
        var tail = new Element[Pages.Count - 1];
        for (int i = 1; i < Pages.Count; i++) tail[i - 1] = Pages[i];
        var remainder = new HeaderFooterPage(Header, tail, Footer);
        return new RenderResult(result.NextY, remainder);
    }

    private VFrame BuildFrame(Element body)
    {
        var frame = new VFrame();
        if (Header is not null) frame.AddAuto(Header);
        frame.AddRelative(1, body);
        if (Footer is not null) frame.AddAuto(Footer);
        return frame;
    }
}
