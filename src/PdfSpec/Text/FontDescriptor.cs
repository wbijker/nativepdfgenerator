using PdfSpec.Objects;

namespace PdfSpec.Fonts;

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
            new PdfNumber((long)BBoxXMin), new PdfNumber((long)BBoxYMin),
            new PdfNumber((long)BBoxXMax), new PdfNumber((long)BBoxYMax));

        var d = new PdfDictionary();
        d.SetName("Type", "FontDescriptor");
        d.SetName("FontName", FontName);
        d.SetInteger("Flags", Flags);
        d.Add("FontBBox", bbox);
        d.SetNumber("ItalicAngle", ItalicAngle);
        d.SetInteger("Ascent", Ascent);
        d.SetInteger("Descent", Descent);
        d.SetInteger("CapHeight", CapHeight);
        d.SetInteger("StemV", StemV);
        d.Set(FontFileKey, FontFile);
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
