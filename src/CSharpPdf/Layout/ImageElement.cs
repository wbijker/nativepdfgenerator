using CSharpPdf.Images;
using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// A raster image (8-bit DeviceRGB) drawn at a fixed display size. The image
/// XObject is embedded once (cached) and reused if the element re-renders.
/// </summary>
public sealed class ImageElement : UIElement<ImageElement>
{
    private readonly byte[] _rgb;
    private readonly int _pixelWidth;
    private readonly int _pixelHeight;
    private readonly double _width;
    private readonly double _height;
    private PdfReference? _imageRef;

    public ImageElement(byte[] rgb, int pixelWidth, int pixelHeight, double width, double height)
    {
        _rgb = rgb;
        _pixelWidth = pixelWidth;
        _pixelHeight = pixelHeight;
        _width = width;
        _height = height;
    }

    public override Size MinimalSpaceRequired => new(_width, _height);
    public override Size PreferredSize => new(_width, _height);

    protected override Size MeasureCore(Size available) => new(_width, _height);

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        _imageRef ??= context.Document.AddObject(PdfImage.Rgb(_rgb, _pixelWidth, _pixelHeight));
        Point start = context.Cursor;
        context.DrawImage(_imageRef, start.X, start.Y, _width, _height);
        return new RenderResult(null, new Point(start.X, start.Y - _height));
    }
}
