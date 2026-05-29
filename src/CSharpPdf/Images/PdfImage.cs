using CSharpPdf.Filters;
using CSharpPdf.Objects;

namespace CSharpPdf.Images;

/// <summary>
/// Factory for image XObjects (Chapter 3, "Raster Images"). An image XObject is
/// a stream whose dictionary describes a 2D array of pixels: its dimensions,
/// color space, and bits per component. Sample data is optionally compressed
/// with FlateDecode (zlib), which the .NET BCL produces directly.
/// </summary>
public static class PdfImage
{
    /// <summary>An 8-bit-per-component DeviceRGB image (3 bytes per pixel, row-major).</summary>
    public static PdfStream Rgb(byte[] samples, int width, int height, bool compress = true) =>
        Build(samples, width, height, new PdfName("DeviceRGB"), bitsPerComponent: 8, imageMask: false, compress);

    /// <summary>An 8-bit DeviceGray image (1 byte per pixel). Also used for soft masks.</summary>
    public static PdfStream Gray(byte[] samples, int width, int height, bool compress = true) =>
        Build(samples, width, height, new PdfName("DeviceGray"), bitsPerComponent: 8, imageMask: false, compress);

    /// <summary>
    /// A JPEG (JFIF) image embedded verbatim via the DCTDecode filter — the one
    /// raster format PDF accepts without unwrapping. Caller supplies the known
    /// dimensions and color space.
    /// </summary>
    public static PdfStream Jpeg(byte[] jpegData, int width, int height, string colorSpace = "DeviceRGB")
    {
        var image = new PdfStream(jpegData);
        var d = image.Dictionary;
        d["Type"] = new PdfName("XObject");
        d["Subtype"] = new PdfName("Image");
        d["Width"] = new PdfNumber(width);
        d["Height"] = new PdfNumber(height);
        d["ColorSpace"] = new PdfName(colorSpace);
        d["BitsPerComponent"] = new PdfNumber(8);
        d["Filter"] = new PdfName("DCTDecode");
        return image;
    }

    /// <summary>
    /// A grayscale soft mask (Chapter 3, "Soft Masks"): per-pixel alpha where 0
    /// is fully transparent and 255 fully opaque. Attach via the parent image's
    /// SMask key.
    /// </summary>
    public static PdfStream SoftMask(byte[] alpha, int width, int height, bool compress = true) =>
        Gray(alpha, width, height, compress);

    /// <summary>
    /// A 1-bit stencil mask (Chapter 3, "Stencil Masks"): packed bits, MSB first,
    /// rows padded to a byte. 0 paints, 1 leaves alone (set <paramref name="invert"/>
    /// to flip via a [1 0] Decode array). Used either as an ImageMask painted in
    /// the current fill color, or as a parent image's Mask.
    /// </summary>
    public static PdfStream StencilMask(byte[] bits, int width, int height, bool invert = false)
    {
        var mask = Build(bits, width, height, colorSpace: null, bitsPerComponent: 1, imageMask: true, compress: false);
        if (invert)
        {
            mask.Dictionary["Decode"] = new PdfArray(new PdfNumber(1), new PdfNumber(0));
        }
        return mask;
    }

    private static PdfStream Build(byte[] data, int width, int height, PdfObject? colorSpace,
        int bitsPerComponent, bool imageMask, bool compress)
    {
        byte[] payload = compress ? FlateFilter.Encode(data) : data;
        var image = new PdfStream(payload);
        var d = image.Dictionary;
        d["Type"] = new PdfName("XObject");
        d["Subtype"] = new PdfName("Image");
        d["Width"] = new PdfNumber(width);
        d["Height"] = new PdfNumber(height);
        d["BitsPerComponent"] = new PdfNumber(bitsPerComponent);
        if (imageMask)
        {
            d["ImageMask"] = new PdfBoolean(true);
        }
        else if (colorSpace is not null)
        {
            d["ColorSpace"] = colorSpace;
        }
        if (compress)
        {
            d["Filter"] = new PdfName("FlateDecode");
        }
        return image;
    }
}
