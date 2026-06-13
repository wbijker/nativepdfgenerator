using System.Diagnostics;
using System.IO.Compression;

namespace PdfSpec.Filters;

/// <summary>
/// The FlateDecode filter: zlib-wrapped DEFLATE, produced directly by the .NET
/// BCL ZLibStream (RFC 1950), which is exactly what PDF's /FlateDecode expects.
/// </summary>
public static class FlateFilter
{
    public static byte[] Encode(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data, 0, data.Length);
        }
        var result = output.ToArray();
        return result;
    }
}
