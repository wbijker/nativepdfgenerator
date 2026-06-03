using System.Globalization;
using System.Text;
using CSharpPdf.Filters;
using CSharpPdf.Objects;

namespace CSharpPdf;

/// <summary>
/// The low-level object store: the set of indirect objects that make up a PDF
/// file, plus the logic to serialize the four file sections (ISO 32000-1 §7.5):
/// header (§7.5.2), body (§7.5.3), cross-reference table or stream (§7.5.4 /
/// §7.5.8), and trailer (§7.5.5). This is the file/syntax layer;
/// <see cref="PdfDoc"/> sits on top and manages document structure.
/// </summary>
public sealed class PdfObjectStore
{
    private readonly List<PdfObject> _objects = new();

    /// <summary>The document catalog (root) reference, written into the trailer.</summary>
    public PdfReference? Root { get; set; }

    /// <summary>The document information dictionary reference (trailer /Info), if any.</summary>
    public PdfReference? Info { get; set; }

    /// <summary>
    /// When true (default), the writer uses PDF 1.5 object streams (§7.5.7)
    /// and a cross-reference stream (§7.5.8). Compressible objects are packed
    /// into Flate-compressed object streams; the xref table itself is a
    /// compressed stream. Typical reduction on top of compressed content
    /// streams is another 20-30%. Turn off to emit the classic uncompressed
    /// xref table for PDF 1.4 consumers.
    /// </summary>
    public static bool UseObjectStreams = true;

    /// <summary>Max indirect objects packed into one object stream. 100 is a conservative balance between compression ratio and per-objstm size.</summary>
    public static int MaxObjectsPerStream = 100;

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
        if (UseObjectStreams) SaveWithObjectStreams(stream);
        else SaveClassic(stream);
    }

    // ============ Classic save (PDF 1.4 style) ============

    private void SaveClassic(Stream stream)
    {
        Write(stream, "%PDF-1.7\n");
        Write(stream, "%âãÏÓ\n");

        int count = _objects.Count;
        long[] offsets = new long[count + 1];
        for (int i = 1; i <= count; i++)
        {
            offsets[i] = stream.Position;
            Write(stream, $"{i} 0 obj\n");
            _objects[i - 1].Write(stream);
            Write(stream, "\nendobj\n");
        }

        long xrefOffset = stream.Position;
        Write(stream, "xref\n");
        Write(stream, $"0 {count + 1}\n");
        Write(stream, "0000000000 65535 f \n");
        for (int i = 1; i <= count; i++)
        {
            Write(stream, offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        }

        var trailer = new PdfDictionary();
        trailer["Size"] = new PdfNumber(count + 1);
        trailer["Root"] = Root;
        if (Info is not null) trailer["Info"] = Info;

        Write(stream, "trailer\n");
        trailer.Write(stream);
        Write(stream, "\nstartxref\n");
        Write(stream, xrefOffset.ToString(CultureInfo.InvariantCulture) + "\n");
        Write(stream, "%%EOF\n");
    }

    // ============ Object streams + xref stream (PDF 1.5+) ============

    private void SaveWithObjectStreams(Stream stream)
    {
        int n = _objects.Count;

        // Streams can't go inside object streams; everything else can.
        // Encryption-related objects would also be excluded, but we don't have any.
        var isPackable = new bool[n + 1];
        var packable = new List<int>();
        for (int i = 1; i <= n; i++)
        {
            if (_objects[i - 1] is PdfStream)
            {
                isPackable[i] = false;
            }
            else
            {
                isPackable[i] = true;
                packable.Add(i);
            }
        }

        // Pack into batches → one ObjStm per batch.
        int perStream = System.Math.Max(1, MaxObjectsPerStream);
        int batchCount = (packable.Count + perStream - 1) / perStream;

        var objstmStartNum = n + 1;
        var objstmStreams = new PdfStream[batchCount];
        var compressedLocation = new (int ObjstmNum, int Index)[n + 1];

        for (int b = 0; b < batchCount; b++)
        {
            int start = b * perStream;
            int len = System.Math.Min(perStream, packable.Count - start);
            int objstmNum = objstmStartNum + b;

            // Serialize each packed object's bytes (no obj/endobj wrapping)
            // and remember its offset within the body block.
            var body = new MemoryStream();
            var offsets = new long[len];
            for (int j = 0; j < len; j++)
            {
                int objNum = packable[start + j];
                offsets[j] = body.Position;
                _objects[objNum - 1].Write(body);
                body.WriteByte((byte)'\n');
                compressedLocation[objNum] = (objstmNum, j);
            }
            var bodyBytes = body.ToArray();

            // Header: "objnum offset objnum offset ..." (whitespace-separated).
            var header = new StringBuilder();
            for (int j = 0; j < len; j++)
            {
                if (j > 0) header.Append(' ');
                header.Append(packable[start + j]).Append(' ').Append(offsets[j]);
            }
            header.Append('\n');
            var headerBytes = Encoding.Latin1.GetBytes(header.ToString());

            var combined = new byte[headerBytes.Length + bodyBytes.Length];
            System.Buffer.BlockCopy(headerBytes, 0, combined, 0, headerBytes.Length);
            System.Buffer.BlockCopy(bodyBytes, 0, combined, headerBytes.Length, bodyBytes.Length);

            var compressed = FlateFilter.Encode(combined);
            var pdfStream = new PdfStream(compressed);
            pdfStream.Dictionary["Type"] = new PdfName("ObjStm");
            pdfStream.Dictionary["N"] = new PdfNumber(len);
            pdfStream.Dictionary["First"] = new PdfNumber(headerBytes.Length);
            pdfStream.Dictionary["Filter"] = new PdfName("FlateDecode");
            objstmStreams[b] = pdfStream;
        }

        int xrefStreamNum = objstmStartNum + batchCount;
        int totalObjs = xrefStreamNum; // last assigned object number

        // ---- Header ----
        Write(stream, "%PDF-1.5\n"); // object streams require ≥1.5
        Write(stream, "%âãÏÓ\n");

        // ---- Body: write streams (non-packable originals) + objstm streams ----
        var streamOffsets = new long[totalObjs + 1];
        for (int i = 1; i <= n; i++)
        {
            if (!isPackable[i])
            {
                streamOffsets[i] = stream.Position;
                Write(stream, $"{i} 0 obj\n");
                _objects[i - 1].Write(stream);
                Write(stream, "\nendobj\n");
            }
        }
        for (int b = 0; b < batchCount; b++)
        {
            int objNum = objstmStartNum + b;
            streamOffsets[objNum] = stream.Position;
            Write(stream, $"{objNum} 0 obj\n");
            objstmStreams[b].Write(stream);
            Write(stream, "\nendobj\n");
        }

        // ---- Cross-reference stream ----
        long xrefOffset = stream.Position;

        // Pick field widths. Field 1 = type (always 1 byte).
        // Field 2 = offset (for type 1) or objstm-number (for type 2).
        // Field 3 = generation (for type 1) or index within objstm (for type 2).
        long maxField2 = System.Math.Max(xrefOffset, totalObjs);
        int field2Bytes = ByteWidth(maxField2);
        if (field2Bytes < 4) field2Bytes = 4; // common minimum
        int field3Bytes = 2; // up to 65535 — covers our batch sizes

        int entries = totalObjs + 1; // entries 0..totalObjs
        int entrySize = 1 + field2Bytes + field3Bytes;
        var xrefBuffer = new byte[entries * entrySize];
        int p = 0;

        // Entry 0 — free, head of free list.
        xrefBuffer[p++] = 0;
        for (int j = 0; j < field2Bytes; j++) xrefBuffer[p++] = 0;
        xrefBuffer[p++] = 0xFF; xrefBuffer[p++] = 0xFF;

        // Original objects 1..n
        for (int i = 1; i <= n; i++)
        {
            if (!isPackable[i])
            {
                xrefBuffer[p++] = 1; // in-use
                WriteBigEndian(xrefBuffer, p, streamOffsets[i], field2Bytes); p += field2Bytes;
                xrefBuffer[p++] = 0; xrefBuffer[p++] = 0; // gen 0
            }
            else
            {
                var loc = compressedLocation[i];
                xrefBuffer[p++] = 2; // compressed
                WriteBigEndian(xrefBuffer, p, loc.ObjstmNum, field2Bytes); p += field2Bytes;
                WriteBigEndian(xrefBuffer, p, loc.Index, field3Bytes); p += field3Bytes;
            }
        }
        // Object-stream streams n+1..n+batchCount
        for (int b = 0; b < batchCount; b++)
        {
            int objNum = objstmStartNum + b;
            xrefBuffer[p++] = 1;
            WriteBigEndian(xrefBuffer, p, streamOffsets[objNum], field2Bytes); p += field2Bytes;
            xrefBuffer[p++] = 0; xrefBuffer[p++] = 0;
        }
        // The xref stream's own entry
        xrefBuffer[p++] = 1;
        WriteBigEndian(xrefBuffer, p, xrefOffset, field2Bytes); p += field2Bytes;
        xrefBuffer[p++] = 0; xrefBuffer[p++] = 0;

        var compressedXref = FlateFilter.Encode(xrefBuffer);
        var xref = new PdfStream(compressedXref);
        xref.Dictionary["Type"] = new PdfName("XRef");
        xref.Dictionary["Size"] = new PdfNumber(entries);
        xref.Dictionary["Root"] = Root;
        if (Info is not null) xref.Dictionary["Info"] = Info;
        xref.Dictionary["W"] = new PdfArray(new PdfNumber(1), new PdfNumber(field2Bytes), new PdfNumber(field3Bytes));
        xref.Dictionary["Filter"] = new PdfName("FlateDecode");

        Write(stream, $"{xrefStreamNum} 0 obj\n");
        xref.Write(stream);
        Write(stream, "\nendobj\n");

        Write(stream, "startxref\n");
        Write(stream, xrefOffset.ToString(CultureInfo.InvariantCulture) + "\n");
        Write(stream, "%%EOF\n");
    }

    private static int ByteWidth(long value)
    {
        int b = 1;
        long t = value;
        while (t > 0xFF) { t >>= 8; b++; }
        return b;
    }

    private static void WriteBigEndian(byte[] buf, int pos, long value, int width)
    {
        for (int k = width - 1; k >= 0; k--)
        {
            buf[pos + k] = (byte)(value & 0xFF);
            value >>= 8;
        }
    }

    private static void Write(Stream stream, string text)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
