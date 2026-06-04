using PdfSpec.Filters;
using PdfSpec.Objects;

namespace PdfSpec.Text;

/// <summary>
/// Base for fonts whose program is embedded in the PDF (ISO 32000-1 §9.6/§9.8).
/// Embeds the (possibly subset) font program as a font-file stream, writes a
/// <see cref="FontDescriptor"/>, and a single-byte Widths array for codes
/// FirstChar..LastChar.
/// </summary>
public abstract class EmbeddedFont : Font
{
    protected abstract byte[] Program { get; }
    protected abstract string Subtype { get; }
    protected abstract string FontFileKey { get; }
    protected abstract string Encoding { get; }
    protected abstract int FirstCode { get; }
    protected abstract int LastCode { get; }
    protected abstract int[] CharWidths { get; }
    protected abstract FontDescriptor BuildDescriptor();

    internal override void Build(PdfObjectStore store, PdfDictionary fontDictionary)
    {
        var fontFile = new PdfStream(FlateFilter.Encode(Program));
        fontFile.Dictionary.SetInteger("Length1", Program.Length);
        fontFile.Dictionary.SetName("Filter", "FlateDecode");
        var fontFileRef = store.Add(fontFile);

        var descriptor = BuildDescriptor();
        descriptor.FontFileKey = FontFileKey;
        descriptor.FontFile = fontFileRef;
        var descriptorRef = store.Add(descriptor.Build());

        var widths = new PdfArray();
        foreach (int w in CharWidths)
        {
            widths.Add(new PdfNumber((long)w));
        }

        fontDictionary.SetName("Type", "Font");
        fontDictionary.SetName("Subtype", Subtype);
        fontDictionary.SetName("BaseFont", BaseFont);
        fontDictionary.SetInteger("FirstChar", FirstCode);
        fontDictionary.SetInteger("LastChar", LastCode);
        fontDictionary.Add("Widths", widths);
        fontDictionary.SetName("Encoding", Encoding);
        fontDictionary.Add("FontDescriptor", descriptorRef);
    }
}
