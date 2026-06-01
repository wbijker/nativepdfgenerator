using System.Globalization;
// using System.Security.Cryptography; // re-enable with the /ID file identifier below
using System.Text;
using CSharpPdf.Objects;

namespace CSharpPdf;

/// <summary>
/// The low-level object store: the set of indirect objects that make up a PDF
/// file, plus the logic to serialize the four file sections (ISO 32000-1 §7.5):
/// header (§7.5.2), body (§7.5.3), classic cross-reference table (§7.5.4), and
/// trailer (§7.5.5). This is the file/syntax layer; <see cref="PdfDoc"/>
/// sits on top and manages document structure.
/// </summary>
public sealed class PdfObjectStore
{
    private readonly List<PdfObject> _objects = new();

    /// <summary>The document catalog (root) reference, written into the trailer.</summary>
    public PdfReference? Root { get; set; }

    /// <summary>The document information dictionary reference (trailer /Info), if any.</summary>
    public PdfReference? Info { get; set; }

    /// <summary>Register an indirect object and return a reference to it.</summary>
    public PdfReference Add(PdfObject obj)
    {
        _objects.Add(obj);
        return new PdfReference(_objects.Count, 0);
    }

    public void Save(string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        Save(stream);
    }

    public void Save(Stream stream)
    {
        if (Root is null)
        {
            throw new InvalidOperationException("PdfObjectStore.Root must be set before saving.");
        }

        // Header. The binary marker comment (bytes > 127) tells tools the file
        // contains binary data and should be treated as such.
        Write(stream, "%PDF-1.7\n");
        Write(stream, "%âãÏÓ\n");

        // Body: one indirect object per registered object. Record byte offsets
        // so the cross-reference table can point at each one.
        int count = _objects.Count;
        long[] offsets = new long[count + 1];
        for (int i = 1; i <= count; i++)
        {
            offsets[i] = stream.Position;
            Write(stream, $"{i} 0 obj\n");
            _objects[i - 1].Write(stream);
            Write(stream, "\nendobj\n");
        }

        // Cross-reference table.
        long xrefOffset = stream.Position;
        Write(stream, "xref\n");
        Write(stream, $"0 {count + 1}\n");
        Write(stream, "0000000000 65535 f \n");
        for (int i = 1; i <= count; i++)
        {
            Write(stream, offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        }

        // Trailer.
        var trailer = new PdfDictionary();
        trailer["Size"] = new PdfNumber(count + 1);
        trailer["Root"] = Root;
        if (Info is not null)
        {
            trailer["Info"] = Info;
        }

        // File identifier (spec 14.4): two byte strings, equal for a freshly
        // written file. MD5 of a unique seed keeps it likely-unique (the spec does
        // not require reproducibility). Disabled for now because the random seed
        // makes output non-deterministic (every run rewrites all sample PDFs).
        // byte[] id = MD5.HashData(Encoding.UTF8.GetBytes(
        //     $"{DateTime.UtcNow.Ticks}-{Guid.NewGuid()}-{count}"));
        // trailer["ID"] = new PdfArray(new PdfHexString(id), new PdfHexString(id));

        Write(stream, "trailer\n");
        trailer.Write(stream);
        Write(stream, "\nstartxref\n");
        Write(stream, xrefOffset.ToString(CultureInfo.InvariantCulture) + "\n");
        Write(stream, "%%EOF\n");
    }

    private static void Write(Stream stream, string text)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
