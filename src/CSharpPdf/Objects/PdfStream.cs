using System.Text;

namespace CSharpPdf.Objects;

/// <summary>
/// A PDF stream object: a dictionary followed by a sequence of raw bytes.
/// The <c>/Length</c> entry is filled in automatically on write.
/// </summary>
public sealed class PdfStream : PdfObject
{
    public PdfDictionary Dictionary { get; } = new();
    public byte[] Data { get; set; }

    public PdfStream(byte[] data) => Data = data;

    public PdfStream(string text) => Data = Encoding.Latin1.GetBytes(text);

    public override void Write(Stream stream)
    {
        Dictionary["Length"] = new PdfNumber((long)Data.Length);
        Dictionary.Write(stream);
        Emit(stream, "\nstream\n");
        stream.Write(Data, 0, Data.Length);
        Emit(stream, "\nendstream");
    }
}
