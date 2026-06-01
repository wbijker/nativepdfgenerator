using CSharpPdf.Images;
using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// A raster image (8-bit DeviceRGB) drawn at a fixed display size. The image
/// XObject is embedded once (cached) and reused if the element re-renders.
/// </summary>
public sealed class ImageElement : UIElement
{
    public byte[] Rgb { get; set; } = System.Array.Empty<byte>();
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }

    /// <summary>Width on the page, in points.</summary>
    public double DisplayWidth { get; set; }

    /// <summary>Height on the page, in points.</summary>
    public double DisplayHeight { get; set; }

    private PdfReference? _imageRef;

    public ImageElement() { }
    public ImageElement(byte[] rgb, int pixelWidth, int pixelHeight, double displayWidth, double displayHeight)
    {
        Rgb = rgb;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        DisplayWidth = displayWidth;
        DisplayHeight = displayHeight;
    }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var inner = InnerAvailable(available);
        double w = DisplayWidth > 0 ? DisplayWidth : inner.Width;
        double h = DisplayHeight > 0 ? DisplayHeight : (inner.Height ?? 0);
        var size = new SizeRect(w, h);
        return WithOwnInset(new SpaceDimension(size, size, verticalBreakable: false));
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        _imageRef ??= context.Document.AddObject(PdfImage.Rgb(Rgb, PixelWidth, PixelHeight));
        Point start = context.Cursor;
        // Display size of 0 = "fill the available box" (useful for backgrounds /
        // layer fills); a positive value pins the image to that exact size.
        double w = DisplayWidth > 0 ? DisplayWidth : available.Width;
        double h = DisplayHeight > 0 ? DisplayHeight : available.Height;
        context.DrawImage(_imageRef, start.X, start.Y, w, h);
        return new RenderResult(null, new Point(start.X, start.Y - h));
    }
}
