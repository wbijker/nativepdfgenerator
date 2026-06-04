using System.Globalization;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document information dictionary (ISO 32000-1 §14.3.3), referenced from
/// the trailer's <c>/Info</c>. Wraps a single <see cref="PdfDictionary"/>
/// that's mutated in place as properties are set; <see cref="Write"/>
/// delegates to it directly — no per-save allocation.
/// </summary>
public sealed class DocumentInfo : PdfObject
{
    private readonly PdfDictionary _dictionary = new();

    public string? Title { set => _dictionary.SetString("Title", value); }
    public string? Author { set => _dictionary.SetString("Author", value); }
    public string? Subject { set => _dictionary.SetString("Subject", value); }
    public string? Keywords { set => _dictionary.SetString("Keywords", value); }
    public string? Creator { set => _dictionary.SetString("Creator", value); }
    public string? Producer { set => _dictionary.SetString("Producer", value); }

    public DateTimeOffset? CreationDate { set => _dictionary.SetString("CreationDate", value is null ? null : FormatDate(value.Value)); }
    public DateTimeOffset? ModDate { set => _dictionary.SetString("ModDate", value is null ? null : FormatDate(value.Value)); }

    internal bool IsEmpty => _dictionary.Entries.Count == 0;

    public override void Write(Stream stream) => _dictionary.Write(stream);

    private static string FormatDate(DateTimeOffset when)
    {
        TimeSpan offset = when.Offset;
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{when:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'");
    }
}
