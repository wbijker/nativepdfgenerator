using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// One entry inside an <see cref="HStack"/>. Carries the axis size and
/// the child element, plus optional per-item alignment overrides.
/// <c>null</c> on either alignment slot means "fall back to the
/// container's default" (e.g.
/// <see cref="HStack.DefaultHorizontalAlignment"/> /
/// <see cref="HStack.DefaultVerticalAlignment"/>).
/// </summary>
public class HStackItem(
    AxisSize size,
    Element content,
    Alignment? horizontalAlignment = null,
    Alignment? verticalAlignment = null)
{
    public AxisSize Size { get; } = size;
    public Element Content { get; } = content;

    /// <summary>Where the column's natural content sits horizontally inside its allocated width. <c>null</c> → container default.</summary>
    public Alignment? HorizontalAlignment { get; } = horizontalAlignment;

    /// <summary>Where the column's natural content sits vertically inside the row's band height. <c>null</c> → container default.</summary>
    public Alignment? VerticalAlignment { get; } = verticalAlignment;
}
