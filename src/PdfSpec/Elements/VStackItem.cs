using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// One entry inside a <see cref="VStack"/>. Carries the slot height —
/// <see cref="AxisSize.Fixed"/> or <see cref="AxisSize.Auto"/> only, by
/// construction — the content, and an optional horizontal alignment
/// override (for slack between the item's natural width and the column
/// width).
///
/// <para>
/// Constructed via the static factories <see cref="Fixed"/> and
/// <see cref="Auto"/>, so there is no path through the API that admits
/// a <see cref="AxisType.Relative"/> size — VStack doesn't support that
/// case and the type system enforces it without a runtime check.
/// </para>
/// </summary>
public sealed class VStackItem
{
    public AxisSize Size { get; }
    public Element Content { get; }

    /// <summary>Where the item sits horizontally inside the column width. <c>null</c> falls back to <see cref="VStack.DefaultHorizontalAlignment"/>.</summary>
    public HorizontalAlignment? HorizontalAlignment { get; }

    private VStackItem(AxisSize size, Element content, HorizontalAlignment? horizontalAlignment)
    {
        Size = size;
        Content = content;
        HorizontalAlignment = horizontalAlignment;
    }

    /// <summary>A <see cref="AxisSize.Fixed"/> slot — the item gets exactly <paramref name="height"/> points along the vertical axis.</summary>
    public static VStackItem Fixed(double height, Element content, HorizontalAlignment? horizontalAlignment = null) =>
        new(AxisSize.Fixed((float)height), content, horizontalAlignment);

    /// <summary>A <see cref="AxisSize.Auto"/> slot — the item gets exactly the height it renders into.</summary>
    public static VStackItem Auto(Element content, HorizontalAlignment? horizontalAlignment = null) =>
        new(AxisSize.Auto(), content, horizontalAlignment);
}
