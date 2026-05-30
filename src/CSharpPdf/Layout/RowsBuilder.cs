namespace CSharpPdf.Layout;

/// <summary>
/// Defines the rows of a Rows element with explicit sizing intent. Each call adds a
/// slot and returns it so styling (.Background, .Border, .Padding) and optional
/// .Content(...) can be chained.
/// </summary>
public sealed class RowsBuilder
{
    internal readonly List<SlotElement> Slots = new();

    /// <summary>A row with a fixed height (in points when unit is Px).</summary>
    public SlotElement Fixed(double size, Unit unit = Unit.Px)
    {
        var slot = new SlotElement { Sizing = SlotSizing.Fixed, SizeValue = size, SizeUnit = unit };
        Slots.Add(slot);
        return slot;
    }

    /// <summary>A row sized to its content's natural height.</summary>
    public SlotElement Auto()
    {
        var slot = new SlotElement { Sizing = SlotSizing.Auto };
        Slots.Add(slot);
        return slot;
    }

    /// <summary>A row that shares the remaining height with other relative rows by weight.</summary>
    public SlotElement Relative(double weight = 1)
    {
        var slot = new SlotElement { Sizing = SlotSizing.Relative, SizeValue = weight };
        Slots.Add(slot);
        return slot;
    }
}
