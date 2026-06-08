using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class AxisItem(AxisSize size, Element content, VerticalAlign? verticalAlign = null)
{
    public AxisSize Size { get; } = size;
    public Element Content { get; } = content;

    /// <summary>
    /// Per-item override for the row's <see cref="Rows.DefaultVerticalAlign"/>.
    /// <c>null</c> means "fall back to the row default".
    /// </summary>
    public VerticalAlign? VerticalAlign { get; } = verticalAlign;
}
