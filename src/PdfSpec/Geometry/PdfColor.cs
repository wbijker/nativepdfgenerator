namespace PdfSpec.Geometry;

/// <summary>
/// A colour in one of the device colour spaces supported by PDF
/// (ISO 32000-1 §8.6.3): <see cref="ColorSpace.Gray"/> (DeviceGray, 1
/// component), <see cref="ColorSpace.Rgb"/> (DeviceRGB, 3 components), or
/// <see cref="ColorSpace.Cmyk"/> (DeviceCMYK, 4 components). All components
/// are 0..1. Optional <see cref="Alpha"/> (0..1) — when less than 1 the
/// content-stream applies it via an ExtGState <c>ca</c>/<c>CA</c> entry.
/// </summary>
public sealed class PdfColor
{
    public ColorSpace Space { get; }
    public double C1 { get; }
    public double C2 { get; }
    public double C3 { get; }
    public double C4 { get; }
    public double Alpha { get; }

    private PdfColor(ColorSpace space, double c1, double c2, double c3, double c4, double alpha)
    {
        Space = space;
        C1 = c1; C2 = c2; C3 = c3; C4 = c4;
        Alpha = alpha;
    }

    /// <summary>True when <see cref="Alpha"/> is less than 1.</summary>
    public bool HasAlpha => Alpha < 1.0;

    /// <summary>DeviceGray colour. <paramref name="gray"/> is 0..1.</summary>
    public static PdfColor Gray(double gray, double alpha = 1.0) =>
        new(ColorSpace.Gray, gray, 0, 0, 0, alpha);

    /// <summary>DeviceRGB colour. Each component is 0..1.</summary>
    public static PdfColor Rgb(double r, double g, double b, double alpha = 1.0) =>
        new(ColorSpace.Rgb, r, g, b, 0, alpha);

    /// <summary>DeviceCMYK colour. Each component is 0..1.</summary>
    public static PdfColor Cmyk(double c, double m, double y, double k, double alpha = 1.0) =>
        new(ColorSpace.Cmyk, c, m, y, k, alpha);

    /// <summary>DeviceRGB from a 24-bit hex value (0xRRGGBB).</summary>
    public static PdfColor FromHex(int rgb, double alpha = 1.0) => Rgb(
        ((rgb >> 16) & 0xFF) / 255.0,
        ((rgb >> 8) & 0xFF) / 255.0,
        (rgb & 0xFF) / 255.0,
        alpha);

    /// <summary>Same colour with a different <paramref name="alpha"/>.</summary>
    public PdfColor WithAlpha(double alpha) => new(Space, C1, C2, C3, C4, alpha);
}

/// <summary>
/// PDF device colour space (ISO 32000-1 §8.6.3) — also the colour mode of a
/// <see cref="PdfColor"/>. Each name corresponds to the PDF name object
/// emitted in the file: <c>/DeviceGray</c>, <c>/DeviceRGB</c>,
/// <c>/DeviceCMYK</c>.
/// </summary>
public enum ColorSpace
{
    Gray,
    Rgb,
    Cmyk,
}
