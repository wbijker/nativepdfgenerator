using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Imperative single-body page: a <see cref="Header"/> pinned to the top
/// (Auto height), a <see cref="Footer"/> pinned to the bottom (Auto
/// height), and a <see cref="Body"/> that fills the slot in between.
/// Built by assigning the three members directly:
/// <code>
/// var page = new Page
/// {
///     Header = () => new Paragraph("Title"),
///     Body   = someColumn,
///     Footer = () => new Paragraph("…"),
/// };
/// pdfPage.RenderTopLevel(page);
/// </code>
///
/// <para>
/// <b>Only the <see cref="Body"/> is eligible for overflow.</b> The
/// header and footer are fixed chrome — measured, drawn once per physical
/// page, and never paginate. The body is rendered into the middle slot;
/// if its top element can't fit and returns a
/// <see cref="RenderResult.NextElement"/> continuation, this page emits a
/// <see cref="RenderResult"/> whose continuation is a fresh
/// <see cref="Page"/> carrying the <i>same</i> header / footer factories
/// and the body remainder. The page-loop in
/// <see cref="PdfPage.RenderTopLevel(Element)"/> then breaks to a new
/// physical page, redraws the header / footer, and continues drawing the
/// remainder — repeating until the body is fully laid out.
/// </para>
///
/// <para>
/// Overflow <i>within</i> the body propagates the conventional way: every
/// container that supports overflow (e.g. <see cref="VStack"/>,
/// <see cref="MultiColumn"/>) hands back a new instance of itself with the
/// unrendered remainder — including a child's own continuation — injected,
/// so the body's top element ultimately surfaces a single continuation up
/// to this <see cref="Page"/>.
/// </para>
///
/// <para>
/// <b>Why the chrome is a factory.</b> Layout elements are single-use:
/// once an element has fully rendered it is exhausted, so the very same
/// instance cannot be re-drawn on the next page. Because the header /
/// footer must appear identically on <i>every</i> physical page, they are
/// supplied as <see cref="Func{Element}"/> factories — each page invokes
/// the factory to obtain a fresh element to draw. The body, by contrast,
/// is a single value: it is never redrawn, only continued via the
/// remainder threaded into the next page.
/// </para>
///
/// <para>
/// Any of the three slots may be <c>null</c>: a null Header / Footer is
/// skipped (the body fills its space); a null Body produces a page with
/// just the chrome.
/// </para>
/// </summary>
public sealed class Page : Element
{
    /// <summary>Factory for the fixed chrome drawn at the top of every physical page. Invoked fresh per page; auto-sized to its content height. <c>null</c> = no header.</summary>
    public Func<Element>? Header { get; set; }

    /// <summary>The single overflow-eligible element. When it returns a continuation, the page paginates onto a fresh physical page carrying the body remainder. <c>null</c> = empty middle.</summary>
    public Element? Body { get; set; }

    /// <summary>Factory for the fixed chrome drawn at the bottom of every physical page. Invoked fresh per page; auto-sized to its content height. <c>null</c> = no footer.</summary>
    public Func<Element>? Footer { get; set; }

    public override PdfSizeHint SizeHint(PdfSize available) =>
        new(available.Width, available.Height, available.Width, available.Height);

    protected override RenderResult RenderCore(ContentStream cs, PdfSize available)
    {
        // Fresh chrome instances for this physical page (see class remarks).
        Element? header = Header?.Invoke();
        Element? footer = Footer?.Invoke();

        // Measure header / footer first so the body slot knows its ceiling.
        // Auto convention: MaxHeight if known, else MinHeight.
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

        // Only the body overflows. Render it into the middle slot; the
        // continuation (if any) is the remainder that didn't fit.
        Element? remainder = null;
        if (Body is not null && bodyH > 0)
        {
            var bodySub = cs.CreateSubStream(0, headerH, available.Width, bodyH);
            var bodyResult = Body.Render(bodySub, new PdfSize(available.Width, bodyH));
            bodySub.Build();
            remainder = bodyResult.NextElement;
        }

        if (remainder is null) return RenderResult.Done(available.Height);

        // Body overflowed: spill onto a fresh physical page that rebuilds
        // the same header / footer chrome and continues with the remainder.
        var next = new Page { Header = Header, Body = remainder, Footer = Footer };
        return new RenderResult(available.Height, next);
    }

    private static double SlotHeight(Element? slot, PdfSize available)
    {
        if (slot is null) return 0;
        var hint = slot.SizeHint(available);
        double h = hint.MaxHeight ?? hint.MinHeight;
        return Math.Min(h, available.Height);
    }
}
