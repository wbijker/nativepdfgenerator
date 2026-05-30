namespace CSharpPdf.Layout;

/// <summary>
/// Defines the columns of a Cols element with explicit sizing intent. Each call
/// adds a slot and returns it so styling and optional .Content(...) can be chained.
/// </summary>
public sealed class ColsBuilder
{
    internal readonly List<SlotElement> Slots = new();

    /// <summary>A column with a fixed width (in points when unit is Px).</summary>
    public SlotElement Fixed(double size, Unit unit = Unit.Px)
    {
        var slot = new SlotElement { Sizing = SlotSizing.Fixed, SizeValue = size, SizeUnit = unit };
        Slots.Add(slot);
        return slot;
    }

    /// <summary>A column sized to its content's natural width.</summary>
    public SlotElement Auto()
    {
        var slot = new SlotElement { Sizing = SlotSizing.Auto };
        Slots.Add(slot);
        return slot;
    }

    /// <summary>A column that shares the remaining width with other relative columns by weight.</summary>
    public SlotElement Relative(double weight = 1)
    {
        var slot = new SlotElement { Sizing = SlotSizing.Relative, SizeValue = weight };
        Slots.Add(slot);
        return slot;
    }
}
