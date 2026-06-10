using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Three-slot vertical layout: an optional header at the top, an
/// optional body filling the middle, and an optional footer at the
/// bottom. Mirrors a <see cref="VFrame"/> with the slot sizing locked
/// down — <see cref="Header"/> and <see cref="Footer"/> are
/// <see cref="VFrameItem.Auto"/> (claim their natural rendered height
/// and no more), <see cref="Body"/> is
/// <see cref="VFrameItem.Relative(double, Element, Alignment?)"/> with
/// a single unit (it absorbs everything left between header and
/// footer).
///
/// <para>
/// Any constructor argument may be <c>null</c>: the corresponding
/// slot is simply omitted. A <c>HeaderFooterPage(null, body, null)</c>
/// collapses to a single full-height body; <c>HeaderFooterPage(header,
/// null, footer)</c> stacks header on top and footer on bottom with
/// the gap between them empty.
/// </para>
///
/// <para>
/// Inherits all of <see cref="VFrame"/>'s behaviour through the
/// underlying instance: always fills <c>available.Height</c>, not
/// breakable (the page is the unit), composes with the
/// <see cref="BoxElement"/> chrome on the outer frame.
/// </para>
/// </summary>
public sealed class HeaderFooterPage : Element
{
    public Element? Header { get; }
    public Element? Body { get; }
    public Element? Footer { get; }

    private readonly VFrame _frame;

    public HeaderFooterPage(Element? header, Element? body, Element? footer)
    {
        Header = header;
        Body = body;
        Footer = footer;

        _frame = new VFrame();
        if (header is not null) _frame.AddAuto(header);
        if (body   is not null) _frame.AddRelative(1, body);
        if (footer is not null) _frame.AddAuto(footer);
    }

    public override PdfSizeHint SizeHint(PdfSize available) => _frame.SizeHint(available);

    public override RenderResult Render(ContentStream cs, PdfSize available) => _frame.Render(cs, available);
}
