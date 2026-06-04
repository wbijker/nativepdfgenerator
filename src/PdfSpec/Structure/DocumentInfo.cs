using System.Globalization;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// The document information dictionary (ISO 32000-1 §14.3.3), referenced from
/// the trailer's <c>/Info</c>. Holds typed fields for the bibliographic
/// metadata; emits the dictionary fresh at write time. Only non-null fields
/// are written.
/// </summary>
public sealed class DocumentInfo : PdfObject
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public string? Creator { get; set; }
    public string? Producer { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? ModDate { get; set; }

    internal bool IsEmpty =>
        Title is null && Author is null && Subject is null && Keywords is null
        && Creator is null && Producer is null && CreationDate is null && ModDate is null;

    public override void Write(Stream stream) => Build().Write(stream);

    private PdfDictionary Build()
    {
        var d = new PdfDictionary();
        if (Title is not null) d.Add("Title", new PdfString(Title));
        if (Author is not null) d.Add("Author", new PdfString(Author));
        if (Subject is not null) d.Add("Subject", new PdfString(Subject));
        if (Keywords is not null) d.Add("Keywords", new PdfString(Keywords));
        if (Creator is not null) d.Add("Creator", new PdfString(Creator));
        if (Producer is not null) d.Add("Producer", new PdfString(Producer));
        if (CreationDate is { } cd) d.Add("CreationDate", new PdfString(FormatDate(cd)));
        if (ModDate is { } md) d.Add("ModDate", new PdfString(FormatDate(md)));
        return d;
    }

    private static string FormatDate(DateTimeOffset when)
    {
        TimeSpan offset = when.Offset;
        char sign = offset < TimeSpan.Zero ? '-' : '+';
        return string.Create(CultureInfo.InvariantCulture,
            $"D:{when:yyyyMMddHHmmss}{sign}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'");
    }
}
