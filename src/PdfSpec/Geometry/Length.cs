namespace PdfSpec.Geometry;

/// <summary>
/// A scalar length tagged with a <see cref="Unit"/> — so callers can
/// express sizes in their own units (mm, cm, inch, px, percent…) and let
/// the layout pass resolve them to points at the right moment. Percentage
/// lengths resolve against the axis-specific parent extent; absolute
/// units convert straight through.
///
/// Implicit construction from <c>double</c> assumes points, so existing
/// numeric-only code keeps working: <c>Width = 100</c> means 100 pt.
/// Use the static helpers — <c>Length.Mm(10)</c>, <c>Length.Percent(50)</c>
/// — for typed construction.
/// </summary>
public readonly struct Length(double value, Unit unit)
{
    public double Value { get; } = value;
    public Unit Unit { get; } = unit;

    /// <summary>
    /// Resolve to PDF points. <paramref name="available"/> is the parent
    /// extent that <see cref="Unit.Percent"/> lengths resolve against;
    /// it's ignored for absolute units.
    /// </summary>
    public double ToPoints(double available = 0) => Units.ToPoints(Value, Unit, available);

    public static implicit operator Length(double value) => new(value, Unit.Pt);

    public static Length Pt(double v) => new(v, Unit.Pt);
    public static Length Px(double v) => new(v, Unit.Px);
    public static Length Mm(double v) => new(v, Unit.Mm);
    public static Length Cm(double v) => new(v, Unit.Cm);
    public static Length Inch(double v) => new(v, Unit.Inch);
    public static Length Percent(double v) => new(v, Unit.Percent);

    public override string ToString() => $"{Value} {Unit}";
}
