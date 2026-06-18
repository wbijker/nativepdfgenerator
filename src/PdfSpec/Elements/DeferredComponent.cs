using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Two-phase content: reserve a box during the normal layout pass using
/// <paramref name="sizeHint"/>'s reported extent, then render the real
/// content into that box later — once the document's page count is
/// final — by calling <paramref name="render"/> with a
/// <see cref="PageData"/> snapshot. Use for page numbers, headers /
/// footers that reference total pages, or any element whose content
/// can't be known until everything else has been laid out.
///
/// <para>
/// First phase, during <see cref="Render"/>:
/// </para>
/// <list type="number">
/// <item><description>Query <c>sizeHint.SizeHint(available)</c> for the
/// box's intended extent. Typically the caller passes a worst-case
/// instance — e.g. a Paragraph with <c>"Page 999 of 999"</c> — so the
/// reservation is wide enough to fit the maximum possible content.</description></item>
/// <item><description>Walk to the owning page (<see cref="ContentStream.OwningPage"/>)
/// and the absolute page-user coords (<see cref="ContentStream.ToPageUserPoint"/>)
/// of the box's top-left.</description></item>
/// <item><description>Register the box with <see cref="PdfDoc"/>'s
/// deferred queue and return <see cref="RenderResult.Done(double)"/>
/// for the reserved height — the surrounding layout treats it as a
/// rendered slot of that size, even though no glyphs land yet.</description></item>
/// </list>
///
/// <para>
/// Second phase, inside <see cref="PdfDoc"/>'s PrepareForSave (after
/// every page has been laid out and the page count is known):
/// </para>
/// <list type="number">
/// <item><description>The doc walks each registered entry, builds a
/// <see cref="PageData"/> (page index → 1-based PageNumber, total page
/// count), and calls <paramref name="render"/> to get the actual
/// content element.</description></item>
/// <item><description>A fresh sub-stream is created on the recorded
/// page at the recorded coords, the returned element is rendered into
/// it, and the sub is flushed. The content appends to whatever was
/// already painted on the page, so it lands on top.</description></item>
/// </list>
/// </summary>
public sealed class DeferredComponent : Element
{
    private readonly Element _sizeHint;
    private readonly Func<PageData, Element> _render;

    public DeferredComponent(Element sizeHint, Func<PageData, Element> render)
    {
        _sizeHint = sizeHint;
        _render = render;
    }

    public override PdfSizeHint SizeHint(PdfSize available) => _sizeHint.SizeHint(available);

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        // Use the size hint's reported extent to claim the reservation.
        // MaxWidth ?? available.Width handles content that "wants the
        // whole row"; MaxHeight ?? MinHeight handles content that
        // reports an unknown max (e.g. paragraphs whose typographic
        // line count isn't surfaced upfront).
        var hint = _sizeHint.SizeHint(available);
        double w = Math.Min(hint.MaxWidth ?? available.Width, available.Width);
        double h = Math.Min(hint.MaxHeight ?? hint.MinHeight, available.Height);

        if (cs.OwningPage is { } page)
        {
            var (px, py) = cs.ToPageUserPoint(0, 0);
            page.Document.RegisterDeferred(page, px, py, w, h, _render);
        }

        return RenderResult.Done(h);
    }
}
