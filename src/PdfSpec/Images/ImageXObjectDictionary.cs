using PdfSpec.Objects;

namespace PdfSpec.Images;

/// <summary>
/// Typed builder for the <c>/Type /XObject /Subtype /Image</c> stream
/// dictionary (ISO 32000-1 §8.9.5). Holds the descriptive entries; appends
/// them onto an existing <see cref="PdfStream"/>'s dictionary via
/// <see cref="WriteTo"/>.
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
        d.Add("Type", new PdfName("XObject"));
        d.Add("Subtype", new PdfName("Image"));
        d.Add("Width", new PdfNumber(Width));
        d.Add("Height", new PdfNumber(Height));
        d.Add("BitsPerComponent", new PdfNumber(BitsPerComponent));

        if (ImageMask)
        {
            d.Add("ImageMask", new PdfBoolean(true));
        }
        else if (ColorSpace is not null)
        {
            d.Add("ColorSpace", ColorSpace);
        }
        if (Filter is not null) d.Add("Filter", Filter);
        if (Decode is not null) d.Add("Decode", Decode);
        if (SoftMask is { } smask) d.Add("SMask", smask);
        if (Mask is { } mask) d.Add("Mask", mask);
    }
}
