using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 04 — named destinations registered in a <c>/Dests</c> name
/// tree under the catalog's <c>/Names</c> dictionary. Each destination
/// is an explicit array <c>[page /Fit]</c> so a link can jump to it
/// by name.
/// </summary>
public sealed class Sample04_NameTree : ISample
{
    public string FileName => "04-name-tree.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();

        var intro = doc.AddPage(PageSizes.Letter);
        Label(intro, "Intro page (named destination: intro)");

        var summary = doc.AddPage(PageSizes.Letter);
        Label(summary, "Summary page (named destination: summary)");

        var dests = new PdfNameTree();
        dests.Add("intro", new PdfArray(intro.Reference, new PdfName("Fit")));
        dests.Add("summary", new PdfArray(summary.Reference, new PdfName("Fit")));

        doc.SetNameTree("Dests", dests.Build());

        doc.Save(path);
    }

    private static void Label(PdfPage page, string text) => page.Content
        .AddText(StandardFont.Helvetica, 18)
        .Show(72, 720, text)
        .Build();
}
