using System.Globalization;
using System.Security.Cryptography;
using PdfSpec.Filters;
using PdfSpec.Objects;

namespace PdfSpec.Files;

/// <summary>
/// Builds the pieces for embedding a file inside a PDF (ISO 32000-1 §7.11): the
/// embedded file stream (Flate-compressed, with Params metadata) and the file
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
        d.SetName("Type", "EmbeddedFile");
        d.SetName("Subtype", mimeType);
        d.SetName("Filter", "FlateDecode");

        DateTimeOffset when = modified ?? DateTimeOffset.Now;
        var paramsDict = new PdfDictionary();
        paramsDict.SetInteger("Size", data.Length);
        paramsDict.Add("CheckSum", new PdfHexString(MD5.HashData(data)));
        paramsDict.SetString("CreationDate", PdfDate(when));
        paramsDict.SetString("ModDate", PdfDate(when));
        d.Add("Params", paramsDict);
        return stream;
    }

    /// <summary>
    /// A file specification dictionary referencing an embedded file stream via EF,
    /// writing both F and UF names for compatibility.
    /// </summary>
    public static PdfDictionary FileSpec(string fileName, PdfReference embeddedStream, string? description = null)
    {
        var spec = new PdfDictionary();
        spec.SetName("Type", "Filespec");
        spec.SetString("F", fileName);
        spec.SetString("UF", fileName);
        var ef = new PdfDictionary();
        ef.Add("F", embeddedStream);
        spec.Add("EF", ef);
        if (description is not null) spec.SetString("Desc", description);
        return spec;
    }

    /// <summary>A collection field dictionary for a portfolio schema (Chapter 8).</summary>
    public static PdfDictionary CollectionField(string subtype, string displayName, int order)
    {
        var d = new PdfDictionary();
        d.SetName("Type", "CollectionField");
        d.SetName("Subtype", subtype);
        d.SetString("N", displayName);
        d.SetInteger("O", order);
        return d;
    }

    /// <summary>Format a timestamp as a PDF date string: <c>D:YYYYMMDDHHmmSS+HH'mm'</c>.</summary>
    public static string PdfDate(DateTimeOffset when)
    {
        TimeSpan offset = when.Offset;
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{when:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'");
    }
}
