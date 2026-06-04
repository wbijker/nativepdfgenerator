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
        var bbox = new PdfArray(
            new PdfNumber(BBoxXMin), new PdfNumber(BBoxYMin),
            new PdfNumber(BBoxXMax), new PdfNumber(BBoxYMax));

        var d = new PdfDictionary
        {
            { "Type", new PdfName("FontDescriptor") },
            { "FontName", new PdfName(FontName) },
            { "Flags", new PdfNumber(Flags) },
            { "FontBBox", bbox },
            { "ItalicAngle", new PdfNumber(ItalicAngle) },
            { "Ascent", new PdfNumber(Ascent) },
            { "Descent", new PdfNumber(Descent) },
            { "CapHeight", new PdfNumber(CapHeight) },
            { "StemV", new PdfNumber(StemV) },
        };
        if (FontFile is { } ff) d.Add(FontFileKey, ff);
        return d;
    }
}

/// <summary>Named bits of the FontDescriptor /Flags entry (ISO 32000-1 §9.8.2).</summary>
[Flags]
public enum FontDescriptorFlags
{
    None = 0,
    FixedPitch = 1 << 0,
    Serif = 1 << 1,
    Symbolic = 1 << 2,
    Script = 1 << 3,
    Nonsymbolic = 1 << 5,
    Italic = 1 << 6,
    AllCap = 1 << 16,
    SmallCap = 1 << 17,
    ForceBold = 1 << 18,
}
