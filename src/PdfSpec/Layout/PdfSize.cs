namespace PdfSpec.Layout;

/// <summary>
/// Because coordinates are relative to the current translate matrix.
/// Each component always start at 0,0 (top, left) in its own space
/// </summary>
/// <param name="width"></param>
/// <param name="height"></param>
public class PdfSize(double width, double height)
{
    public double Width { get; set; } = width;
    public double Height { get; set; } = height;
}