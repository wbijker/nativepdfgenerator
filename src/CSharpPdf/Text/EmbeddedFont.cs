using CSharpPdf.Filters;
using CSharpPdf.Objects;

namespace CSharpPdf.Text;

/// <summary>
/// Base for fonts whose program is embedded in the PDF (ISO 32000-1 §9.6/§9.8).
/// Implements the common "simple font" build: embed the (subset-or-whole) program
/// as a font-file stream, write a FontDescriptor, and a single-byte Widths array
/// for codes FirstChar..LastChar. Subclasses supply the program, metrics, and the
/// subtype/font-file-key pair (e.g. TrueType + FontFile2, Type1C + FontFile3).
/// </summary>
public abstract class EmbeddedFont : Font
{
    protected abstract byte[] Program { get; }
    protected abstract string Subtype { get; }     // e.g. "TrueType"
    protected abstract string FontFileKey { get; } // e.g. "FontFile2"
    protected abstract string Encoding { get; }    // e.g. "WinAnsiEncoding"
    protected abstract int FirstCode { get; }
    protected abstract int LastCode { get; }
    protected abstract int[] CharWidths { get; }   // one entry per code FirstCode..LastCode, 1000-unit space
    protected abstract PdfDictionary BuildDescriptor();

    internal override void Build(PdfObjectStore store, PdfDictionary fontDictionary)
    {
        // Embed the font program verbatim (Flate-compressed) with its uncompressed
        // length in Length1 (required for FontFile2 / TrueType programs).
        var fontFile = new PdfStream(FlateFilter.Encode(Program));
        fontFile.Dictionary["Length1"] = new PdfNumber(Program.Length);
        fontFile.Dictionary["Filter"] = new PdfName("FlateDecode");
        var fontFileRef = store.Add(fontFile);

        var descriptor = BuildDescriptor();
        descriptor[FontFileKey] = fontFileRef;
        var descriptorRef = store.Add(descriptor);

        var widths = new PdfArray();
        foreach (int w in CharWidths)
        {
            widths.Add(new PdfNumber(w));
        }

        fontDictionary["Type"] = new PdfName("Font");
        fontDictionary["Subtype"] = new PdfName(Subtype);
        fontDictionary["BaseFont"] = new PdfName(BaseFont);
        fontDictionary["FirstChar"] = new PdfNumber(FirstCode);
        fontDictionary["LastChar"] = new PdfNumber(LastCode);
        fontDictionary["Widths"] = widths;
        fontDictionary["Encoding"] = new PdfName(Encoding);
        fontDictionary["FontDescriptor"] = descriptorRef;
    }
}
