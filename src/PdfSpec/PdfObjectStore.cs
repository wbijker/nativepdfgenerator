using System.Globalization;
using System.Text;
using PdfSpec.Filters;
using PdfSpec.Objects;

namespace PdfSpec;

/// <summary>
/// The low-level object store: the set of indirect objects that make up a PDF
/// file, plus the logic to serialize the four file sections (ISO 32000-1 §7.5):
/// header (§7.5.2), body (§7.5.3), cross-reference table or stream (§7.5.4 /
/// §7.5.8), and trailer (§7.5.5).
/// </summary>
public sealed class PdfObjectStore
{
    private readonly List<PdfObject> _objects = new();

    public PdfReference? Root { get; set; }
    public PdfReference? Info { get; set; }

    /// <summary>
    /// When true (default), the writer uses PDF 1.5 object streams (§7.5.7)
    /// and a cross-reference stream (§7.5.8). Turn off to emit the classic
    /// uncompressed xref table for PDF 1.4 consumers.
    /// </summary>
    public static bool UseObjectStreams = true;

    public static int MaxObjectsPerStream = 100;

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

        var trailer = new PdfDictionary
        {
            { "Size", new PdfNumber(count + 1) },
            { "Root", Root! },
        };
        if (Info is not null) trailer.Add("Info", Info);

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
            pdfStream.Dictionary.Add("Type", new PdfName("ObjStm"));
            pdfStream.Dictionary.Add("N", new PdfNumber(len));
            pdfStream.Dictionary.Add("First", new PdfNumber(headerBytes.Length));
            pdfStream.Dictionary.Add("Filter", new PdfName("FlateDecode"));
            objstmStreams[b] = pdfStream;
        }

        int xrefStreamNum = objstmStartNum + batchCount;
        int totalObjs = xrefStreamNum;

        Write(stream, "%PDF-1.5\n");
        Write(stream, "%âãÏÓ\n");

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

        long xrefOffset = stream.Position;

        long maxField2 = System.Math.Max(xrefOffset, totalObjs);
        int field2Bytes = ByteWidth(maxField2);
        if (field2Bytes < 4) field2Bytes = 4;
        int field3Bytes = 2;

        int entries = totalObjs + 1;
        int entrySize = 1 + field2Bytes + field3Bytes;
        var xrefBuffer = new byte[entries * entrySize];
        int p = 0;

        xrefBuffer[p++] = 0;
        for (int j = 0; j < field2Bytes; j++) xrefBuffer[p++] = 0;
        xrefBuffer[p++] = 0xFF; xrefBuffer[p++] = 0xFF;

        for (int i = 1; i <= n; i++)
        {
            if (!isPackable[i])
            {
                xrefBuffer[p++] = 1;
                WriteBigEndian(xrefBuffer, p, streamOffsets[i], field2Bytes); p += field2Bytes;
                xrefBuffer[p++] = 0; xrefBuffer[p++] = 0;
            }
            else
            {
                var loc = compressedLocation[i];
                xrefBuffer[p++] = 2;
                WriteBigEndian(xrefBuffer, p, loc.ObjstmNum, field2Bytes); p += field2Bytes;
                WriteBigEndian(xrefBuffer, p, loc.Index, field3Bytes); p += field3Bytes;
            }
        }
        for (int b = 0; b < batchCount; b++)
        {
            int objNum = objstmStartNum + b;
            xrefBuffer[p++] = 1;
            WriteBigEndian(xrefBuffer, p, streamOffsets[objNum], field2Bytes); p += field2Bytes;
            xrefBuffer[p++] = 0; xrefBuffer[p++] = 0;
        }
        xrefBuffer[p++] = 1;
        WriteBigEndian(xrefBuffer, p, xrefOffset, field2Bytes); p += field2Bytes;
        xrefBuffer[p++] = 0; xrefBuffer[p++] = 0;

        var compressedXref = FlateFilter.Encode(xrefBuffer);
        var xref = new PdfStream(compressedXref);
        xref.Dictionary.Add("Type", new PdfName("XRef"));
        xref.Dictionary.Add("Size", new PdfNumber(entries));
        xref.Dictionary.Add("Root", Root!);
        if (Info is not null) xref.Dictionary.Add("Info", Info);
        xref.Dictionary.Add("W", new PdfArray(new PdfNumber(1), new PdfNumber(field2Bytes), new PdfNumber(field3Bytes)));
        xref.Dictionary.Add("Filter", new PdfName("FlateDecode"));

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
