namespace PdfSpec.Elements;

/// <summary>
/// How an <see cref="HStack"/> spaces its items along the row.
/// </summary>
public enum GapMode
{
    /// <summary>A fixed <see cref="HStack.Gap"/> of space between adjacent items only (none at the ends).</summary>
    Between,

    /// <summary>A fixed <see cref="HStack.Gap"/> of space before the first item, between each, and after the last.</summary>
    Around,

    /// <summary>
    /// Spread the row's free width (row width − content width) into equal gaps
    /// before, between, and after every item — CSS <c>space-evenly</c>. Only
    /// visible when the row is given more width than its content needs (e.g. an
    /// explicit <see cref="Element.Width"/> or a <c>Relative</c> slot); the
    /// <see cref="HStack.Gap"/> value is ignored in this mode.
    /// </summary>
    Evenly,
}
