namespace CSharpPdf.Layout;

/// <summary>
/// The single sizing answer a <see cref="Element"/> gives the engine. Contains
/// the floor (<see cref="Minimal"/>) and the natural target (<see cref="Recommended"/>)
/// for the element's outer box at the queried available space, plus
/// <see cref="VerticalBreakable"/> — whether the element can split across
/// pages or must move as a whole.
/// </summary>
public sealed class SpaceDimension
{
    public SizeRect Minimal { get; }
    public SizeRect Recommended { get; }

    /// <summary>True if this element can be paginated; false for atomic blocks (images, transformed labels, …).</summary>
    public bool VerticalBreakable { get; }

    public SpaceDimension(SizeRect minimal, SizeRect recommended, bool verticalBreakable = true)
    {
        Minimal = minimal;
        Recommended = recommended;
        VerticalBreakable = verticalBreakable;
    }

    public static readonly SpaceDimension Empty =
        new(SizeRect.Zero, SizeRect.Zero, verticalBreakable: false);
}
