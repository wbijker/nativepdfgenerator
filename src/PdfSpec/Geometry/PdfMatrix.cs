namespace PdfSpec.Geometry;

/// <summary>
/// A PDF 2×3 affine transformation matrix <c>[a b c d e f]</c> used by the
/// <c>cm</c> (graphics CTM, ISO 32000-1 §8.3.4) and <c>Tm</c> (text matrix,
/// §9.4.2) operators. Represents the mapping
/// <c>x' = a*x + c*y + e ; y' = b*x + d*y + f</c>.
///
/// <para>
/// <b>Important quirk:</b> unlike <c>cm</c> which concatenates with the
/// current CTM, <c>Tm</c> <i>replaces</i> the text matrix — every
/// <c>Tm</c> is absolute, not relative to the previous one, and it is only
/// valid between <c>BT</c> and <c>ET</c>.
/// </para>
///
/// <para>
/// Common recipes:
/// <code>
/// ┌─────────────────────────────────────┬───────┬───────┬────────┬───────┬─────┬─────┐
/// │                Goal                 │   a   │   b   │   c    │   d   │  e  │  f  │
/// ├─────────────────────────────────────┼───────┼───────┼────────┼───────┼─────┼─────┤
/// │ Place text at (x, y)                │ 1     │ 0     │ 0      │ 1     │ x   │ y   │
/// │ Scale to N pt (no font size needed) │ N     │ 0     │ 0      │ N     │ x   │ y   │
/// │ Rotate θ around (x, y)              │ cos θ │ sin θ │ -sin θ │ cos θ │ x   │ y   │
/// │ Italic slant ~12°                   │ 1     │ 0     │ 0.21   │ 1     │ x   │ y   │
/// └─────────────────────────────────────┴───────┴───────┴────────┴───────┴─────┴─────┘
/// </code>
/// </para>
/// </summary>
public readonly struct PdfMatrix
{
    public double A { get; }
    public double B { get; }
    public double C { get; }
    public double D { get; }
    public double E { get; }
    public double F { get; }

    public PdfMatrix(double a, double b, double c, double d, double e, double f)
    {
        A = a; B = b; C = c; D = d; E = e; F = f;
    }

    /// <summary>The identity matrix <c>[1 0 0 1 0 0]</c>.</summary>
    public static readonly PdfMatrix Identity = new(1, 0, 0, 1, 0, 0);

    /// <summary>
    /// Translation by (<paramref name="x"/>, <paramref name="y"/>) —
    /// <c>[1 0 0 1 x y]</c>. As a text matrix, places text at (x, y) using
    /// the current font size from <c>Tf</c>.
    /// </summary>
    public static PdfMatrix Translate(double x, double y) => new(1, 0, 0, 1, x, y);

    /// <summary>
    /// Uniform scale by <paramref name="scale"/> with origin at
    /// (<paramref name="x"/>, <paramref name="y"/>) — <c>[s 0 0 s x y]</c>.
    /// As a text matrix, paints glyphs at <paramref name="scale"/> points
    /// regardless of the <c>Tf</c> font size.
    /// </summary>
    public static PdfMatrix Scale(double scale, double x = 0, double y = 0) =>
        new(scale, 0, 0, scale, x, y);

    /// <summary>
    /// Non-uniform scale by (<paramref name="sx"/>, <paramref name="sy"/>)
    /// with origin at (<paramref name="x"/>, <paramref name="y"/>) —
    /// <c>[sx 0 0 sy x y]</c>.
    /// </summary>
    public static PdfMatrix Scale(double sx, double sy, double x, double y) =>
        new(sx, 0, 0, sy, x, y);

    /// <summary>
    /// Rotation by <paramref name="degrees"/> with origin at
    /// (<paramref name="x"/>, <paramref name="y"/>) —
    /// <c>[cos sin -sin cos x y]</c>.
    /// </summary>
    public static PdfMatrix Rotate(double degrees, double x = 0, double y = 0)
    {
        double r = degrees * Math.PI / 180.0;
        double cos = Math.Cos(r), sin = Math.Sin(r);
        return new(cos, sin, -sin, cos, x, y);
    }

    /// <summary>
    /// Italic-style shear (default 12°, c ≈ 0.21) anchored at
    /// (<paramref name="x"/>, <paramref name="y"/>) — <c>[1 0 tan(θ) 1 x y]</c>.
    /// </summary>
    public static PdfMatrix Italic(double x = 0, double y = 0, double angleDegrees = 12)
    {
        double r = angleDegrees * Math.PI / 180.0;
        return new(1, 0, Math.Tan(r), 1, x, y);
    }
}
