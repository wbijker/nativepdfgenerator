using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 02 — one page with "Hello, World!" drawn via the imperative
/// <see cref="Content.ContentStream.AddText"/> builder at 24pt Helvetica,
/// positioned with the standard text-matrix (Tm) operator.
/// </summary>
public sealed class Sample02_Hello : ISample
{
    public string FileName => "02-hello.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);

        page.Content
            .AddText(StandardFont.Helvetica, 24)
            .Show(72, 720, "Hello, World!")
            .Build();

        doc.Save(path);
    }
}
