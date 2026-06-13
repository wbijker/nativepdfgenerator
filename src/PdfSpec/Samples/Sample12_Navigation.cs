using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Navigation;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 12 — navigation: destinations, actions, link annotations,
/// named destinations, and an OpenAction across a 3-page document.
/// Page 1 hosts four link buttons (GoTo Fit, GoTo named, URI, GoToR
/// remote); pages 2 and 3 each carry a back link. Uses the
/// <see cref="PdfAction"/> factory builders that return raw action
/// dictionaries — the imperative path that pairs cleanly with
/// <c>PdfPage.AddLinkAnnotation</c>.
/// </summary>
public sealed class Sample12_Navigation : ISample
{
    public string FileName => "12-navigation.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var p1 = doc.AddPage(PageSizes.Letter);
        var p2 = doc.AddPage(PageSizes.Letter);
        var p3 = doc.AddPage(PageSizes.Letter);
        foreach (var p in new[] { p1, p2, p3 })
        {
            p.AddFont("F1", doc.AddObject(StandardFonts.Create(StandardFonts.Helvetica)));
        }

        p1.Content.AddText(StandardFont.Helvetica, 24).Show(60, 740, "Navigation — Page 1").Build();
        LinkButton(p1, 60, 680, 240, 28, "GoTo page 3 (Fit)", PdfAction.GoTo(new PdfArray(p3.Reference, new PdfName("Fit"))));
        LinkButton(p1, 60, 640, 240, 28, "Named destination: chapter-3", PdfAction.GoToNamed("chapter-3"));
        LinkButton(p1, 60, 600, 240, 28, "Open oreilly.com (URI)", PdfAction.Uri("https://www.oreilly.com"));
        LinkButton(p1, 60, 560, 240, 28, "Open Chapter2.pdf (GoToR)", PdfAction.GoToRemote("Chapter2.pdf", 0));

        p2.Content.AddText(StandardFont.Helvetica, 24).Show(60, 740, "Navigation — Page 2").Build();
        LinkButton(p2, 60, 680, 240, 28, "Back to page 1 top (XYZ)",
            PdfAction.GoTo(new PdfArray(p1.Reference, new PdfName("XYZ"), new PdfNumber(0L), new PdfNumber(792L), PdfNull.Instance)));

        p3.Content.AddText(StandardFont.Helvetica, 24).Show(60, 740, "Navigation — Page 3 (target)").Build();
        LinkButton(p3, 60, 680, 240, 28, "Back to page 1 (Fit)", PdfAction.GoTo(new PdfArray(p1.Reference, new PdfName("Fit"))));

        doc.AddNamedDestination("chapter-3", new PdfArray(p3.Reference, new PdfName("Fit")));
        doc.SetOpenAction(PdfAction.GoTo(new PdfArray(p1.Reference, new PdfName("Fit"))));

        doc.Save(path);
    }

    private static void LinkButton(PdfPage page, double x, double y, double w, double h, string label, PdfDictionary action)
    {
        var c = page.Content;
        c.Save().SetRgbStroke(PdfColor.Rgb(0.2, 0.3, 0.7)).SetRgbFill(PdfColor.Rgb(0.90, 0.93, 1.0)).SetLineWidth(1)
            .Rectangle(x, y, w, h).FillStroke().Restore();
        c.Save().SetRgbFill(PdfColor.Rgb(0.1, 0.2, 0.6))
            .AddText(StandardFont.Helvetica, 12).Show(x + 10, y + h / 2 - 4, label).Build()
            .Restore();
        page.AddLinkAnnotation(new PdfRectangle(x, y, x + w, y + h), action);
    }
}
