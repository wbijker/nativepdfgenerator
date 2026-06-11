using PdfSpec.Elements;
using PdfSpec.Layout;

namespace PdfSpec.Layout;

/// <summary>
/// One entry inside a <see cref="VFrame"/>. Mirrors
/// <see cref="VStackItem"/> but admits all three sizing modes —
/// <see cref="AxisSize.Fixed"/>, <see cref="AxisSize.Auto"/>, and
/// <see cref="AxisSize.Relative"/> — because VFrame always claims the
/// full available height and can divide leftover space proportionally
/// across relative slots.
///
/// <para>
/// Constructed via the static factories <see cref="Fixed"/>,
/// <see cref="Auto"/>, and <see cref="Relative"/>; the constructor is
/// private so the API can't express anything else.
/// </para>
/// </summary>
public sealed class VFrameItem
{
    public AxisSize Size { get; }
    public Element Content { get; }

    /// <summary>Where the item sits horizontally inside the frame's width. <c>null</c> falls back to <see cref="VFrame.DefaultHorizontalAlignment"/>.</summary>
    public HorizontalAlignment? HorizontalAlignment { get; }

    private VFrameItem(AxisSize size, Element content, HorizontalAlignment? horizontalAlignment)
    {
        Size = size;
        Content = content;
        HorizontalAlignment = horizontalAlignment;
    }

    /// <summary>A <see cref="AxisSize.Fixed"/> slot — the item gets exactly <paramref name="height"/> points.</summary>
    public static VFrameItem Fixed(double height, Element content, HorizontalAlignment? horizontalAlignment = null) =>
        new(AxisSize.Fixed((float)height), content, horizontalAlignment);

    /// <summary>A <see cref="AxisSize.Auto"/> slot — the item gets the height its content reports as its desired max.</summary>
    public static VFrameItem Auto(Element content, HorizontalAlignment? horizontalAlignment = null) =>
        new(AxisSize.Auto(), content, horizontalAlignment);

    /// <summary>A <see cref="AxisSize.Relative"/> slot — the item gets <paramref name="units"/> shares of whatever height is left after Fixed and Auto slots are placed.</summary>
    public static VFrameItem Relative(double units, Element content, HorizontalAlignment? horizontalAlignment = null) =>
        new(AxisSize.Relative((float)units), content, horizontalAlignment);
}
