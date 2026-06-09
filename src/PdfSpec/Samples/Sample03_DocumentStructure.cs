using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 03 — page tree, attribute inheritance, viewer preferences,
/// rotation, and the UserUnit key. Three pages: one inherits its size
/// from the page-tree root, one doubles its UserUnit, and one overrides
/// the inherited size with A4 and rotates 90° clockwise.
/// </summary>
public sealed class Sample03_DocumentStructure : ISample
{
    public string FileName => "03-document-structure.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();

        doc.SetPageLayout("SinglePage");
        doc.SetPageMode("UseThumbs");
        doc.SetDisplayDocTitle(true);

        doc.SetDefaultMediaBox(PageSizes.Letter);

        var p1 = doc.AddPage();
        Label(p1, "Page 1: inherits Letter MediaBox from the page tree");

        var p2 = doc.AddPage();
        p2.SetUserUnit(2.0);
        Label(p2, "Page 2: UserUnit 2.0 (144 units/inch)");

        var p3 = doc.AddPage(PageSizes.A4);
        p3.SetRotation(90);
        Label(p3, "Page 3: A4 override, rotated 90 degrees");

        doc.Save(path);
    }

    private static void Label(PdfPage page, string text) => page.Content
        .AddText()
        .SetFont(StandardFont.Helvetica, 18)
        .Show(72, 720, text)
        .Build();
}
