namespace CSharpPdf.Layout;

/// <summary>
/// A 2-D extent. <see cref="Width"/> is always known; <see cref="Height"/> is
/// <c>null</c> when the height can only be determined once a width has been
/// chosen (the typical case for reflowable content such as a paragraph that
/// hasn't been wrapped yet).
/// </summary>
public sealed class SizeRect
{
    public double Width { get; }
    public double? Height { get; }

    public SizeRect(double width, double? height = null)
    {
        Width = width;
        Height = height;
    }

    public static readonly SizeRect Zero = new(0, 0);
}
