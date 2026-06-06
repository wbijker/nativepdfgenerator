namespace PdfSpec.Layout;

public class PdfSizeHint(double minWidth, double minHeight, double? maxWidth, double? maxHeight)
{
    /// <summary>
    /// The minimum width the element can render in
    /// </summary>
    public double MinWidth { get; set; } = minWidth;

    /// <summary>
    /// The minimum space required start render or start rendering this component
    /// </summary>
    public double MinHeight { get; set; } = minHeight;

    /// <summary>
    /// The maximum width the element can render in, or null if no maximum.
    /// The element may choose to render in less than this width, but not more. 
    /// </summary>
    public double? MaxWidth { get; set; } = maxWidth;

    /// <summary>
    /// The maximum height the element can render in, or null if we don't know.
    /// Cases where we don't know is when there is no need or use to calculate the height,
    /// We render as we go, and break as we go.  
    /// </summary>
    public double? MaxHeight { get; set; } = maxHeight;

    public static PdfSizeHint Fixed(double width, double height) => new PdfSizeHint(width, height, width, height);

    public static PdfSizeHint Flexible(double minWidth, double minHeight) =>
        new PdfSizeHint(minWidth, minHeight, null, null);
}