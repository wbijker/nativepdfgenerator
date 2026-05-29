using System.Globalization;
using System.Security.Cryptography;
using CSharpPdf.Filters;
using CSharpPdf.Objects;

namespace CSharpPdf.Files;

/// <summary>
/// Builds the pieces for embedding a file inside a PDF (Chapter 8): the embedded
/// file stream (Flate-compressed, with Params metadata) and the file
/// specification dictionary that references it.
/// </summary>
public static class EmbeddedFile
{
    /// <summary>
    /// An embedded file stream holding <paramref name="data"/>, tagged with its
    /// MIME type and carrying Size, MD5 CheckSum, and timestamps in Params.
    /// </summary>
    public static PdfStream Stream(byte[] data, string mimeType, DateTimeOffset? modified = null)
    {
        var stream = new PdfStream(FlateFilter.Encode(data));
        var d = stream.Dictionary;
        d["Type"] = new PdfName("EmbeddedFile");
        d["Subtype"] = new PdfName(mimeType); // e.g. "text/plain" -> /text#2Fplain
        d["Filter"] = new PdfName("FlateDecode");

        DateTimeOffset when = modified ?? DateTimeOffset.Now;
        d["Params"] = new PdfDictionary
        {
            ["Size"] = new PdfNumber((long)data.Length),
            ["CheckSum"] = new PdfHexString(MD5.HashData(data)),
            ["CreationDate"] = new PdfString(PdfDate(when)),
            ["ModDate"] = new PdfString(PdfDate(when)),
        };
        return stream;
    }

    /// <summary>
    /// A file specification dictionary referencing an embedded file stream via EF,
    /// writing both F and UF names for compatibility.
    /// </summary>
    public static PdfDictionary FileSpec(string fileName, PdfReference embeddedStream, string? description = null)
    {
        var spec = new PdfDictionary
        {
            ["Type"] = new PdfName("Filespec"),
            ["F"] = new PdfString(fileName),
            ["UF"] = new PdfString(fileName),
            ["EF"] = new PdfDictionary { ["F"] = embeddedStream },
        };
        if (description is not null)
        {
            spec["Desc"] = new PdfString(description);
        }
        return spec;
    }

    /// <summary>
    /// A collection field dictionary for a portfolio schema (Chapter 8). Subtype
    /// is S (text string), D (date), or N (number); O is the display order.
    /// </summary>
    public static PdfDictionary CollectionField(string subtype, string displayName, int order) => new()
    {
        ["Type"] = new PdfName("CollectionField"),
        ["Subtype"] = new PdfName(subtype),
        ["N"] = new PdfString(displayName),
        ["O"] = new PdfNumber(order),
    };

    /// <summary>Format a timestamp as a PDF date string: D:YYYYMMDDHHmmSS+HH'mm'.</summary>
    public static string PdfDate(DateTimeOffset when)
    {
        TimeSpan offset = when.Offset;
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{when:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'");
    }
}
