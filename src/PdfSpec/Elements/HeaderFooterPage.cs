using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Multi-page layout with a shared header + footer. The header sits at
/// the top (Auto height), the footer at the bottom (Auto height), and
/// the body fills the slot in between.
///
/// <para>
/// <b>Pagination across <see cref="Pages"/>.</b> Each entry in
/// <see cref="Pages"/> is rendered as one (or more) PDF pages with the
/// shared chrome. <see cref="Render"/> handles the first body element;
/// if more remain, it returns a <see cref="RenderResult"/> whose
/// <see cref="RenderResult.NextElement"/> is a fresh HeaderFooterPage
/// carrying the tail — <see cref="PdfPage.Body"/>'s pagination loop
/// calls <c>PageBreak()</c> and re-renders, so the header / footer get
/// rebuilt fresh on every page (a <see cref="DeferredComponent"/> in
/// the footer registers a separate entry per page with its own
/// page-data snapshot).
/// </para>
///
/// <para>
/// <b>Reflow within a single <see cref="Pages"/> entry.</b> If the
/// current body element overflows its slot — its <c>Render</c> returns
/// a <see cref="RenderResult.NextElement"/> — that continuation is
/// pushed back to the front of the page queue and rendered on the
/// next page (still with the shared header / footer). So a long
/// content section can be passed as a single <see cref="Pages"/> entry
/// and it will reflow across as many pages as it needs before the
/// next entry kicks in.
/// </para>
///
/// <para>
/// Any of the three slots may be <c>null</c>: a null Header / Footer
/// is simply skipped (body fills its space); a null Body slot for an
/// entry still produces a page with header + footer and an empty
/// middle.
/// </para>
/// </summary>
public sealed class HeaderFooterPage : Element
{
    /// <summary>Factory for the fixed chrome drawn at the top of every PDF page. Invoked fresh per page so single-use elements (and per-page <see cref="DeferredComponent"/> snapshots) work. <c>null</c> = no header.</summary>
    public Func<Element>? Header { get; }
    public IReadOnlyList<Element?> Pages { get; }

    /// <summary>Factory for the fixed chrome drawn at the bottom of every PDF page. Invoked fresh per page. <c>null</c> = no footer.</summary>
    public Func<Element>? Footer { get; }

    public HeaderFooterPage(Func<Element>? header, IReadOnlyList<Element?> pages, Func<Element>? footer)
    {
        Header = header;
        Pages = pages;
        Footer = footer;
    }

    public HeaderFooterPage(Func<Element>? header, Element body, Func<Element>? footer)
        : this(header, new[] { (Element?)body }, footer) { }

    public override PdfSizeHint SizeHint(PdfSize available) =>
        new(available.Width, available.Height, available.Width, available.Height);

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (Pages.Count == 0) return RenderResult.Done(0);

        // Fresh chrome instances for this PDF page — header / footer are
        // factories so single-use elements can't carry over from the
        // previous page and a DeferredComponent in the footer registers a
        // separate per-page entry.
        Element? header = Header?.Invoke();
        Element? footer = Footer?.Invoke();

        // Measure header / footer first so the body slot knows its
        // ceiling. Same convention VFrame uses for Auto slots:
        // MaxHeight if set, else MinHeight.
        double headerH = SlotHeight(header, available);
        double footerH = SlotHeight(footer, available);
        double bodyH   = Math.Max(0, available.Height - headerH - footerH);

        if (header is not null && headerH > 0)
        {
            var headerSub = cs.CreateSubStream(0, 0, available.Width, headerH);
            header.Render(headerSub, new PdfSize(available.Width, headerH));
            headerSub.Build();
        }

        if (footer is not null && footerH > 0)
        {
            var footerSub = cs.CreateSubStream(0, headerH + bodyH, available.Width, footerH);
            footer.Render(footerSub, new PdfSize(available.Width, footerH));
            footerSub.Build();
        }

        Element? continuation = null;
        if (Pages[0] is { } body && bodyH > 0)
        {
            var bodySub = cs.CreateSubStream(0, headerH, available.Width, bodyH);
            var bodyResult = body.Render(bodySub, new PdfSize(available.Width, bodyH));
            bodySub.Build();
            continuation = bodyResult.NextElement;
        }

        // Build the next-page queue:
        // - body still has more → push continuation to front, keep tail
        // - body done           → drop Pages[0], take tail
        int remainingCount = (continuation is not null ? 1 : 0) + Pages.Count - 1;
        if (remainingCount == 0) return RenderResult.Done(available.Height);

        var nextPages = new Element?[remainingCount];
        int idx = 0;
        if (continuation is not null) nextPages[idx++] = continuation;
        for (int i = 1; i < Pages.Count; i++) nextPages[idx++] = Pages[i];

        var remainder = new HeaderFooterPage(Header, nextPages, Footer);
        return new RenderResult(available.Height, remainder);
    }

    private static double SlotHeight(Element? slot, PdfSize available)
    {
        if (slot is null) return 0;
        var hint = slot.SizeHint(available);
        double h = hint.MaxHeight ?? hint.MinHeight;
        return Math.Min(h, available.Height);
    }
}
