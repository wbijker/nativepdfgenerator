using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Structure;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 26 — set both the document information dictionary and an XMP
/// metadata stream with consistent values: title, author, subject,
/// keywords, creator/producer, creation and modification dates.
/// </summary>
public sealed class Sample26_Metadata : ISample
{
    public string FileName => "26-metadata.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);
        page.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
        page.Content.AddText().SetFont("F1", 22).Show(60, 740, "Document Metadata").Build()
            .AddText().SetFont("F1", 12).Show(60, 712, "Title/Author/Subject/Keywords in both the Info dict and XMP.").Build();

        var created = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
        const string title = "Developing with PdfSpec";
        const string author = "Willem";
        const string subject = "A demonstration of PDF metadata";
        const string keywords = "pdf, metadata, xmp, csharp";
        const string creator = "PdfSpec";
        const string producer = "PdfSpec (pure C#)";

        doc.SetDocumentInfo(title, author, subject, keywords, creator, producer, created, created);
        doc.SetXmpMetadata(XmpMetadata.Build(title, author, subject, keywords, creator, producer, created, created));

        doc.Save(path);
    }
}
