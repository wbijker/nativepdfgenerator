namespace PdfSpec.Geometry;

/// <summary>A width/height pair in points.</summary>
public readonly record struct Size(double Width, double Height)
{
    public static readonly Size Zero = new(0, 0);
}
