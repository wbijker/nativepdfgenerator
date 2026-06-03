using PdfSpec.Objects;

namespace PdfSpec.Text;

/// <summary>
/// A Font Descriptor dictionary (ISO 32000-1 §9.8) — the per-font metric and
/// flags block sitting between a Font dictionary and its embedded font program.
/// </summary>
public sealed class FontDescriptor
{
    public string FontName { get; set; } = string.Empty;
    public int Flags { get; set; }
    public int BBoxXMin { get; set; }
    public int BBoxYMin { get; set; }
    public int BBoxXMax { get; set; }
    public int BBoxYMax { get; set; }
    public double ItalicAngle { get; set; }
    public int Ascent { get; set; }
    public int Descent { get; set; }
    public int CapHeight { get; set; }
    public int StemV { get; set; }

    /// <summary>The embedded font program reference, keyed by subclass (FontFile/FontFile2/FontFile3).</summary>
    public string FontFileKey { get; set; } = "FontFile2";
    public PdfReference? FontFile { get; set; }

    public PdfDictionary Build()
    {
        var d = new PdfDictionary
        {
            ["Type"] = new PdfName("FontDescriptor"),
            ["FontName"] = new PdfName(FontName),
            ["Flags"] = new PdfNumber(Flags),
            ["FontBBox"] = new PdfArray(
                new PdfNumber(BBoxXMin), new PdfNumber(BBoxYMin),
                new PdfNumber(BBoxXMax), new PdfNumber(BBoxYMax)),
            ["ItalicAngle"] = new PdfNumber(ItalicAngle),
            ["Ascent"] = new PdfNumber(Ascent),
            ["Descent"] = new PdfNumber(Descent),
            ["CapHeight"] = new PdfNumber(CapHeight),
            ["StemV"] = new PdfNumber(StemV),
        };
        if (FontFile is { } ff) d[FontFileKey] = ff;
        return d;
    }
}

/// <summary>Named bits of the FontDescriptor /Flags entry (ISO 32000-1 §9.8.2).</summary>
[Flags]
public enum FontDescriptorFlags
{
    None = 0,
    FixedPitch = 1 << 0,    // bit 1
    Serif = 1 << 1,         // bit 2
    Symbolic = 1 << 2,      // bit 3
    Script = 1 << 3,        // bit 4
    Nonsymbolic = 1 << 5,   // bit 6
    Italic = 1 << 6,        // bit 7
    AllCap = 1 << 16,       // bit 17
    SmallCap = 1 << 17,     // bit 18
    ForceBold = 1 << 18,    // bit 19
}
