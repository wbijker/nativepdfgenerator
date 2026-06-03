namespace CSharpPdf.Layout;

/// <summary>
/// Length units accepted by fluent sizing/padding methods. All values are
/// converted to PDF user-space points (1/72 inch) before being handed to the
/// layout engine. PDF itself has no intrinsic DPI; <see cref="Px"/> assumes
/// <see cref="Units.DefaultDpi"/> (96 by default, matching CSS).
/// </summary>
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
}

/// <summary>Unit conversion helpers used by the fluent API overloads.</summary>
public static class Units
{
    /// <summary>
    /// DPI assumed when converting <see cref="Unit.Px"/> to points. Defaults
    /// to 96 (CSS convention). Change once at program start if your target
    /// device uses a different DPI.
    /// </summary>
    public static double DefaultDpi = 96.0;

    /// <summary>Convert <paramref name="value"/> in <paramref name="unit"/> to PDF points.</summary>
    public static double ToPoints(double value, Unit unit) => unit switch
    {
        Unit.Pt   => value,
        Unit.Px   => value * 72.0 / DefaultDpi,
        Unit.Mm   => value * 72.0 / 25.4,
        Unit.Cm   => value * 72.0 / 2.54,
        Unit.Inch => value * 72.0,
        _ => value,
    };
}
