using System.Text;

namespace PdfSpec.Objects;

/// <summary>
/// A PDF stream object (ISO 32000-1 §7.3.8): a dictionary followed by a sequence
/// of raw bytes delimited by <c>stream</c>/<c>endstream</c>. The <c>/Length</c>
/// entry is filled in automatically on write.
/// </summary>
public sealed class PdfStream : PdfObject
{
    public PdfDictionary Dictionary { get; } = new();
    public byte[] Data { get; set; }

    public PdfStream(byte[] data) => Data = data;

    public PdfStream(string text) => Data = Encoding.Latin1.GetBytes(text);

    public override void Write(Stream stream)
    {
        Dictionary.Add("Length", new PdfNumber((long)Data.Length));
        Dictionary.Write(stream);
        Emit(stream, "\nstream\n");
        stream.Write(Data, 0, Data.Length);
        Emit(stream, "\nendstream");
    }
}
