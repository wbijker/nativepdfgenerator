using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;

namespace PdfSpec.Samples;

/// <summary>
/// Sample 11 — text state operators: rendering modes (fill / stroke /
/// fill+stroke), character and word spacing, horizontal scaling, text
/// rise for sub/superscripts, leading + T*, manual TJ kerning, and
/// WinAnsiEncoding for accented Latin-1 characters.
/// </summary>
public sealed class Sample11_TextState : ISample
{
    public string FileName => "11-text-state.pdf";

    public void Build(string path)
    {
        var doc = new PdfDoc();
        var page = doc.AddPage(PageSizes.Letter);
        var c = page.Content;

        // Rendering modes: fill, stroke, fill+stroke.
        c.AddText(StandardFont.HelveticaBold, 30).SetTextMatrix(1, 0, 0, 1, 60, 730)
            .SetRgbFill(PdfColor.Rgb(0.85, 0.1, 0.1)).SetTextRenderMode(TextRenderMode.Fill)
            .ShowText("Fill mode (Tr 0)").Build();
        c.AddText(StandardFont.HelveticaBold, 30).SetTextMatrix(1, 0, 0, 1, 60, 690)
            .SetRgbStroke(PdfColor.Rgb(0.1, 0.1, 0.8)).SetLineWidth(0.7).SetTextRenderMode(TextRenderMode.Stroke)
            .ShowText("Stroke mode (Tr 1)").Build();
        c.AddText(StandardFont.HelveticaBold, 30).SetTextMatrix(1, 0, 0, 1, 60, 650)
            .SetRgbFill(PdfColor.Rgb(1, 0.8, 0)).SetRgbStroke(PdfColor.Rgb(0, 0, 0)).SetTextRenderMode(TextRenderMode.FillStroke)
            .ShowText("Fill + Stroke (Tr 2)").Build();

        c.SetRgbFill(PdfColor.Rgb(0, 0, 0));

        // Character spacing, word spacing, horizontal scaling.
        c.AddText(StandardFont.Helvetica, 15).SetTextMatrix(1, 0, 0, 1, 60, 600)
            .SetCharSpacing(0).SetWordSpacing(0).SetHorizontalScaling(100).ShowText("Normal: the quick brown fox").Build();
        c.AddText(StandardFont.Helvetica, 15).SetTextMatrix(1, 0, 0, 1, 60, 576)
            .SetCharSpacing(3).ShowText("Char spacing Tc 3: the quick brown fox").Build();
        c.AddText(StandardFont.Helvetica, 15).SetTextMatrix(1, 0, 0, 1, 60, 552)
            .SetCharSpacing(0).SetWordSpacing(8).ShowText("Word spacing Tw 8: the quick brown fox").Build();
        c.AddText(StandardFont.Helvetica, 15).SetTextMatrix(1, 0, 0, 1, 60, 528)
            .SetWordSpacing(0).SetHorizontalScaling(160).ShowText("Horizontal scaling Tz 160").Build();

        // Text rise for sub/superscripts.
        c.AddText(StandardFont.Helvetica, 18).SetTextMatrix(1, 0, 0, 1, 60, 488)
            .ShowText("Rise: H").SetTextRise(-4).SetFont(StandardFont.Helvetica, 12).ShowText("2")
            .SetTextRise(0).SetFont(StandardFont.Helvetica, 18).ShowText("O,  E = mc").SetTextRise(7).SetFont(StandardFont.Helvetica, 12).ShowText("2")
            .SetTextRise(0).Build();

        // Leading + T* for multiple lines.
        c.AddText(StandardFont.Helvetica, 15).SetLeading(20).SetTextMatrix(1, 0, 0, 1, 60, 448)
            .ShowText("Leading + T*: line one").NextLine().ShowText("line two").NextLine().ShowText("line three").Build();

        // Manual kerning: plain Tj vs TJ with adjustments.
        c.AddText(StandardFont.HelveticaBold, 38).SetTextMatrix(1, 0, 0, 1, 60, 350).ShowText("AWAY  (plain Tj)").Build();
        c.AddText(StandardFont.HelveticaBold, 38).SetTextMatrix(1, 0, 0, 1, 60, 300)
            .ShowTextWithKerning("A", 120, "W", 120, "A", 95, "Y", "  (kerned TJ)").Build();

        // WinAnsiEncoding: accented Latin-1 characters.
        c.AddText(StandardFont.Helvetica, 18).SetTextMatrix(1, 0, 0, 1, 60, 250)
            .ShowText("WinAnsi: Français, Español, Düsseldorf, café, naïve").Build();

        doc.Save(path);
    }
}
