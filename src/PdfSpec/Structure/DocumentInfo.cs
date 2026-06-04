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

    public string? Title { set => SetOrRemove("Title", value); }
    public string? Author { set => SetOrRemove("Author", value); }
    public string? Subject { set => SetOrRemove("Subject", value); }
    public string? Keywords { set => SetOrRemove("Keywords", value); }
    public string? Creator { set => SetOrRemove("Creator", value); }
    public string? Producer { set => SetOrRemove("Producer", value); }

    public DateTimeOffset? CreationDate
    {
        set
        {
            if (value is null) _dictionary.Remove("CreationDate");
            else _dictionary.Add("CreationDate", new PdfString(FormatDate(value.Value)));
        }
    }

    public DateTimeOffset? ModDate
    {
        set
        {
            if (value is null) _dictionary.Remove("ModDate");
            else _dictionary.Add("ModDate", new PdfString(FormatDate(value.Value)));
        }
    }

    internal bool IsEmpty => _dictionary.Entries.Count == 0;

    public override void Write(Stream stream) => _dictionary.Write(stream);

    private void SetOrRemove(string key, string? value)
    {
        if (value is null) _dictionary.Remove(key);
        else _dictionary.Add(key, new PdfString(value));
    }

    private static string FormatDate(DateTimeOffset when)
    {
        TimeSpan offset = when.Offset;
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{when:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'");
    }
}
