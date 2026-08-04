using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 01 — minimal valid PDF: catalog → page tree → one blank US
/// Letter page. The smallest output the writer can produce.
/// </summary>
public sealed class Sample01_Blank : ISample
{
    public string FileName => "01-blank.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        doc.AddPage(PageSizes.Letter);
        doc.Save(path);
    }
}
