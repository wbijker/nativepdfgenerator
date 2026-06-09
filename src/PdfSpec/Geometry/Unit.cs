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
}
