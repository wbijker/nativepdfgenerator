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
        fontFile.Dictionary.Add("Length1", new PdfNumber(Program.Length));
        fontFile.Dictionary.Add("Filter", new PdfName("FlateDecode"));
        var fontFileRef = store.Add(fontFile);

        var descriptor = BuildDescriptor();
        descriptor.FontFileKey = FontFileKey;
        descriptor.FontFile = fontFileRef;
        var descriptorRef = store.Add(descriptor.Build());

        var widths = new PdfArray();
        foreach (int w in CharWidths)
        {
            widths.Add(new PdfNumber(w));
        }

        fontDictionary.Add("Type", new PdfName("Font"));
        fontDictionary.Add("Subtype", new PdfName(Subtype));
        fontDictionary.Add("BaseFont", new PdfName(BaseFont));
        fontDictionary.Add("FirstChar", new PdfNumber(FirstCode));
        fontDictionary.Add("LastChar", new PdfNumber(LastCode));
        fontDictionary.Add("Widths", widths);
        fontDictionary.Add("Encoding", new PdfName(Encoding));
        fontDictionary.Add("FontDescriptor", descriptorRef);
    }
}
