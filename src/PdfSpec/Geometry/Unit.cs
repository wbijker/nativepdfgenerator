namespace PdfSpec.Geometry;

public enum Unit
{
    /// <summary>PDF point — 1/72 inch. The native unit, no conversion.</summary>
    Pt,

    /// <summary>Screen pixel — converted using <see cref="Units.DefaultDpi"/>.</summary>
    Px,

    /// <summary>Millimetre.</summary>
    Mm,

    /// <summary>Centimetre.</summary>
    Cm,

    /// <summary>Inch.</summary>
    Inch,

    /// <summary>
    /// Percentage of the parent's available extent along the relevant axis
    /// (width for horizontal lengths, height for vertical). 100 means
    /// "match the available", 50 means "half of it".
    /// </summary>
    Percent,

    // Lowercase aliases — let call sites read in the conventional unit
    // casing (e.g. Unit.mm, Unit.cm). Each maps to the same value as its
    // capitalised twin, so they're interchangeable everywhere the enum is
    // consumed.

    /// <summary>Alias for <see cref="Pt"/>.</summary>
    pt = Pt,

    /// <summary>Alias for <see cref="Px"/>.</summary>
    px = Px,

    /// <summary>Alias for <see cref="Mm"/>.</summary>
    mm = Mm,

    /// <summary>Alias for <see cref="Cm"/>.</summary>
    cm = Cm,

    /// <summary>Alias for <see cref="Inch"/>.</summary>
    inch = Inch,

    /// <summary>Alias for <see cref="Percent"/>.</summary>
    percent = Percent,
}
