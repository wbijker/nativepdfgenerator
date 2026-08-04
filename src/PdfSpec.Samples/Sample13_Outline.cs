using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Navigation;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 13 — a bookmark hierarchy with an open root branch
/// ("Document") containing a closed sub-branch (Section 2 →
/// Subsection 1), plus a top-level "Summary". Mirrors the
/// five-visible-items example from "Developing with PDF".
/// </summary>
public sealed class Sample13_Outline : ISample
{
    public string FileName => "13-outline.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        doc.SetPageMode("UseOutlines");

        var page1 = doc.AddPage(PageSizes.Letter);
        var page2 = doc.AddPage(PageSizes.Letter);
        var page3 = doc.AddPage(PageSizes.Letter);
        foreach (var p in new[] { page1, page2, page3 })
        {
            p.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.HelveticaBold)));
        }

        page1.Content
            .AddText(StandardFont.HelveticaBold, 22).Show(60, 760, "Document").Build()
            .AddText(StandardFont.HelveticaBold, 16).Show(60, 701, "Section 1").Build()
            .AddText(StandardFont.HelveticaBold, 16).Show(60, 600, "Section 2").Build()
            .AddText(StandardFont.HelveticaBold, 14).Show(80, 560, "Subsection 1").Build();
        page2.Content.AddText(StandardFont.HelveticaBold, 16).Show(60, 500, "Section 3").Build();
        page3.Content.AddText(StandardFont.HelveticaBold, 22).Show(60, 700, "Summary").Build();

        var document = new PdfOutlineItem("Document", Xyz(page1.Reference, 0, 792));
        document.AddChild("Section 1", Xyz(page1.Reference, null, 701));
        var section2 = document.AddChild("Section 2", Xyz(page1.Reference, null, 600));
        section2.Open = false;
        section2.AddChild("Subsection 1", Xyz(page1.Reference, null, 560));
        document.AddChild("Section 3", Xyz(page2.Reference, null, 500));
        var summary = new PdfOutlineItem("Summary", Xyz(page3.Reference, null, 700));

        doc.SetOutline(new[] { document, summary });

        doc.Save(path);
    }

    /// <summary>An XYZ destination array — [page /XYZ left top null].</summary>
    private static PdfArray Xyz(PdfReference page, double? left, double? top)
    {
        PdfObject Maybe(double? v) => v is double d ? new PdfNumber(d) : PdfNull.Instance;
        return new PdfArray(page, new PdfName("XYZ"), Maybe(left), Maybe(top), PdfNull.Instance);
    }
}
