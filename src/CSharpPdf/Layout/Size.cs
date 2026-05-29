namespace CSharpPdf.Layout;

/// <summary>A width/height pair in points, used by the layout engine.</summary>
public readonly record struct Size(double Width, double Height)
{
    public static readonly Size Zero = new(0, 0);
}
