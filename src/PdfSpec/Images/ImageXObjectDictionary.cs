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
        d.SetName("Type", "XObject");
        d.SetName("Subtype", "Image");
        d.SetInteger("Width", Width);
        d.SetInteger("Height", Height);
        d.SetInteger("BitsPerComponent", BitsPerComponent);

        if (ImageMask)
        {
            d.SetBoolean("ImageMask", true);
        }
        else
        {
            d.Set("ColorSpace", ColorSpace);
        }
        d.Set("Filter", Filter);
        d.Set("Decode", Decode);
        d.Set("SMask", SoftMask);
        d.Set("Mask", Mask);
    }
}
