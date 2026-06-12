namespace PdfSpec.Svg;

/// <summary>
/// 2 × 3 affine matrix in SVG conventions (top-left origin, Y down).
/// Encodes the homogeneous transform
/// <c>[a c e; b d f; 0 0 1]</c> — applied to a point as
/// <c>(a*x + c*y + e, b*x + d*y + f)</c>. Used by the SVG renderer to
/// fold viewBox + per-node <c>transform="…"</c> chains into a single
/// matrix applied to every coordinate before it hits the
/// <see cref="Content.ContentStream"/>.
/// </summary>
internal readonly struct SvgMatrix
{
    public readonly double A, B, C, D, E, F;

    public SvgMatrix(double a, double b, double c, double d, double e, double f)
        { A = a; B = b; C = c; D = d; E = e; F = f; }

    public static SvgMatrix Identity => new(1, 0, 0, 1, 0, 0);
    public static SvgMatrix Translate(double tx, double ty) => new(1, 0, 0, 1, tx, ty);
    public static SvgMatrix Scale(double sx, double sy)     => new(sx, 0, 0, sy, 0, 0);

    public static SvgMatrix Rotate(double degrees)
    {
        double r = degrees * Math.PI / 180.0;
        double c = Math.Cos(r), s = Math.Sin(r);
        return new(c, s, -s, c, 0, 0);
    }

    public static SvgMatrix Rotate(double degrees, double cx, double cy) =>
        Translate(cx, cy).Multiply(Rotate(degrees)).Multiply(Translate(-cx, -cy));

    public static SvgMatrix SkewX(double degrees) =>
        new(1, 0, Math.Tan(degrees * Math.PI / 180.0), 1, 0, 0);

    public static SvgMatrix SkewY(double degrees) =>
        new(1, Math.Tan(degrees * Math.PI / 180.0), 0, 1, 0, 0);

    /// <summary>Return <c>this · other</c> — apply <paramref name="other"/> first, then <c>this</c>.</summary>
    public SvgMatrix Multiply(SvgMatrix o) => new(
        A * o.A + C * o.B,
        B * o.A + D * o.B,
        A * o.C + C * o.D,
        B * o.C + D * o.D,
        A * o.E + C * o.F + E,
        B * o.E + D * o.F + F);

    public (double X, double Y) Apply(double x, double y) =>
        (A * x + C * y + E, B * x + D * y + F);

    /// <summary>Scalar scale this matrix applies to a stroke width — <c>sqrt(|det|)</c>, which collapses to the uniform scale factor for similarities.</summary>
    public double LinearScale() => Math.Sqrt(Math.Abs(A * D - B * C));
}
