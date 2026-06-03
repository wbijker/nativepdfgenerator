using PdfSpec.Objects;

namespace PdfSpec.Images;

/// <summary>
/// Typed builder for the <c>/Type /XObject /Subtype /Image</c> stream dictionary
/// (ISO 32000-1 §8.9.5). Holds the descriptive entries (width, height, colour
/// space, bits per component, filter, masks) and emits the dictionary onto an
/// owning <see cref="PdfStream"/>.
/// </summary>
public sealed class ImageXObjectDictionary
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int BitsPerComponent { get; set; }
    public bool ImageMask { get; set; }
    public PdfName? ColorSpace { get; set; }
    public PdfName? Filter { get; set; }
    public PdfArray? Decode { get; set; }
    public PdfReference? SoftMask { get; set; }
    public PdfObject? Mask { get; set; }

    public void WriteTo(PdfDictionary d)
    {
        d["Type"] = new PdfName("XObject");
        d["Subtype"] = new PdfName("Image");
        d["Width"] = new PdfNumber(Width);
        d["Height"] = new PdfNumber(Height);
        d["BitsPerComponent"] = new PdfNumber(BitsPerComponent);

        if (ImageMask)
        {
            d["ImageMask"] = new PdfBoolean(true);
        }
        else if (ColorSpace is not null)
        {
            d["ColorSpace"] = ColorSpace;
        }
        if (Filter is not null) d["Filter"] = Filter;
        if (Decode is not null) d["Decode"] = Decode;
        if (SoftMask is { } smask) d["SMask"] = smask;
        if (Mask is { } mask) d["Mask"] = mask;
    }
}
