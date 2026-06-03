using System.Globalization;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document information dictionary (ISO 32000-1 §14.3.3), referenced from
/// the trailer's <c>/Info</c>. Holds the bibliographic-style metadata: title,
/// author, subject, keywords, creator, producer, creation/mod dates. Only
/// non-null fields are written to the underlying dictionary.
/// </summary>
public sealed class DocumentInfo
{
    internal PdfDictionary Dictionary { get; } = new();

    private string? _title, _author, _subject, _keywords, _creator, _producer;
    private DateTimeOffset? _creationDate, _modDate;

    public string? Title { get => _title; set => SetString("Title", ref _title, value); }
    public string? Author { get => _author; set => SetString("Author", ref _author, value); }
    public string? Subject { get => _subject; set => SetString("Subject", ref _subject, value); }
    public string? Keywords { get => _keywords; set => SetString("Keywords", ref _keywords, value); }
    public string? Creator { get => _creator; set => SetString("Creator", ref _creator, value); }
    public string? Producer { get => _producer; set => SetString("Producer", ref _producer, value); }

    public DateTimeOffset? CreationDate
    {
        get => _creationDate;
        set
        {
            _creationDate = value;
            if (value is null) Dictionary.Remove("CreationDate");
            else Dictionary["CreationDate"] = new PdfString(FormatDate(value.Value));
        }
    }

    public DateTimeOffset? ModDate
    {
        get => _modDate;
        set
        {
            _modDate = value;
            if (value is null) Dictionary.Remove("ModDate");
            else Dictionary["ModDate"] = new PdfString(FormatDate(value.Value));
        }
    }

    /// <summary>True if no entries have been set yet.</summary>
    internal bool IsEmpty => Dictionary.Entries.Count == 0;

    private void SetString(string key, ref string? field, string? value)
    {
        field = value;
        if (value is null) Dictionary.Remove(key);
        else Dictionary[key] = new PdfString(value);
    }

    private static string FormatDate(DateTimeOffset when)
    {
        TimeSpan offset = when.Offset;
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{when:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'");
    }
}
