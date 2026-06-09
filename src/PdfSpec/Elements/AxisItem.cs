using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// One entry inside an axis-laid-out container (Rows, eventually Columns).
/// Carries the axis size and the child element. The container does not
/// concern itself with alignment — wrap the child in a <see cref="BorderElement"/>
/// and use its <see cref="BorderElement.HorizontalAlignment"/> /
/// <see cref="BorderElement.VerticalAlignment"/> when alignment inside the
/// allocated box matters.
/// </summary>
public class AxisItem(AxisSize size, Element content)
{
    public AxisSize Size { get; } = size;
    public Element Content { get; } = content;
}
