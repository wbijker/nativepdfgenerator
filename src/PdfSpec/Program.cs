using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Text;

namespace PdfSpec;

internal static class Program
{
    private const double LabelX = 40;
    private const double DemoX = 130;

    public static void Main(string[] args)
    {
        var doc = new PdfDoc();
        doc.Info.Title = "PdfSpec Text Operators";
        doc.Info.Creator = "PdfSpec";
        doc.Info.Producer = "PdfSpec";
        // Doc-wide default — every Text block that doesn't call SetFont
        // gets Tf with this font auto-emitted at the start of BT.
        doc.SetDefaultFont(Standard14Font.Helvetica, 10);

        var page = doc.AddPage(PageSizes.A4);
        // page.SetDefaultFont(...) would override per page; cs.SetDefaultFont(...)
        // would emit a real Tf to gstate so blocks inherit via q/Q.
        var cs = page.Content;

        double y = PageSizes.A4.Height - 50;

        // Each demo is a Text block — buffered operators, auto-flushed
        // (wrapped in q BT … ET Q) the moment the next AddText / non-text
        // operator / serialization happens. That isolation keeps
        // Tc/Tw/Tz/TL/Tf/Ts/Tr/colour from leaking across rows. The Label
        // helper does the same for the operator name in the left gutter.
        void Label(string text)
        {
            cs.AddText()
                .SetFont(Standard14Font.Courier, 9)
                .SetGrayFill(0.25)
                .Show(LabelX, y, text);
        }

        // ===== Title =====
        cs.AddText()
            .SetFont(Standard14Font.HelveticaBold, 18)
            .Show(LabelX, y, "PDF Text Operators");
        y -= 20;

        cs.AddText()
            .SetFont(Standard14Font.Helvetica, 9)
            .Show(LabelX, y, "ISO 32000-1 §9.3 (text state) and §9.4 (text objects)");
        y -= 26;

        // ===== BT / ET =====
        // (No SetFont call — doc default Helvetica 10 auto-emitted.)
        Label("BT/ET");
        cs.AddText()
            .Show(DemoX, y, "Every demo opens BT and closes ET.");
        y -= 22;

        // ===== Tf — SetFont =====
        Label("Tf");
        cs.AddText()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Helvetica 10   ")
            .SetFont(Standard14Font.TimesItalic, 12)
            .ShowText("Times-Italic 12   ")
            .SetFont(Standard14Font.Courier, 10)
            .ShowText("Courier 10");
        y -= 22;

        // ===== Tj — ShowText =====
        Label("Tj");
        cs.AddText()
            .Show(DemoX, y, "ShowText: one literal string ending in Tj.");
        y -= 22;

        // ===== ' — NextLineShowText (consumes TL) =====
        Label("'");
        cs.AddText()
            .SetLeading(12)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Line 1 (Tj).")
            .NextLineShowText("Line 2 (' moves down by TL then shows).")
            .NextLineShowText("Line 3 (').");
        y -= 44;

        // ===== " — NextLineShowText with Tw / Tc =====
        Label("\"");
        cs.AddText()
            .SetLeading(12)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Default Tw/Tc.")
            .NextLineShowText(8, 2, "\" sets Tw=8 Tc=2 then shows.");
        y -= 32;

        // ===== TJ — ShowTextWithKerning =====
        Label("TJ");
        cs.AddText()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowTextWithKerning(
                "S", -200, "p", -200, "a", -200, "c", -200, "e", -200, "d",
                "    (negative number widens, positive tightens)");
        y -= 22;

        // ===== Td — MoveText =====
        Label("Td");
        cs.AddText()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("[Td origin]")
            .MoveText(70, 0).ShowText("[+70,0]")
            .MoveText(70, 0).ShowText("[+70,0]");
        y -= 22;

        // ===== TD — MoveTextSetLeading =====
        Label("TD");
        cs.AddText()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Top line.")
            .MoveTextSetLeading(0, -12).ShowText("TD(0,-12): moves and sets TL=12.")
            .NextLine().ShowText("T* now uses the leading TD set.");
        y -= 44;

        // ===== Tm — SetTextMatrix (rotated) =====
        Label("Tm");
        cs.AddText()
            .SetFont(Standard14Font.Helvetica, 11)
            .Show(PdfMatrix.Rotate(14, DemoX, y - 18), "Rotated 14° via Tm.");
        y -= 40;

        // ===== T* — NextLine (consumes TL) =====
        Label("T*");
        cs.AddText()
            .SetLeading(11)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Line A (T* advances by TL).")
            .NextLine().ShowText("Line B.")
            .NextLine().ShowText("Line C.");
        y -= 42;

        // ===== TL — SetLeading =====
        Label("TL");
        cs.AddText()
            .SetFont(Standard14Font.Helvetica, 9)
            .Show(DemoX, y, "TL sets the leading consumed by T*, TD, ' and \". (Demonstrated above.)");
        y -= 22;

        // ===== Tc — SetCharSpacing =====
        Label("Tc");
        cs.AddText()
            .SetCharSpacing(0)
            .Show(DemoX, y, "Tc=0  normal character spacing");
        y -= 14;
        cs.AddText()
            .SetCharSpacing(2)
            .Show(DemoX, y, "Tc=2  wider character spacing");
        y -= 22;

        // ===== Tw — SetWordSpacing =====
        Label("Tw");
        cs.AddText()
            .SetWordSpacing(0)
            .Show(DemoX, y, "Tw=0  normal word spacing between words");
        y -= 14;
        cs.AddText()
            .SetWordSpacing(8)
            .Show(DemoX, y, "Tw=8  wider word spacing between words");
        y -= 22;

        // ===== Tz — SetHorizontalScaling =====
        Label("Tz");
        cs.AddText()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .SetHorizontalScaling(100).ShowText("Tz=100  ")
            .SetHorizontalScaling(150).ShowText("Tz=150  ")
            .SetHorizontalScaling(60).ShowText("Tz=60");
        y -= 22;

        // ===== Tr — SetTextRenderMode (0..3) =====
        Label("Tr 0-3");
        cs.AddText()
            .SetRgbFill(0.10, 0.10, 0.10)
            .SetRgbStroke(0.86, 0.15, 0.15)
            .SetLineWidth(0.6)
            .SetFont(Standard14Font.HelveticaBold, 16)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y - 4)
            .SetTextRenderMode(TextRenderMode.Fill).ShowText("Fill ")
            .SetTextRenderMode(TextRenderMode.Stroke).ShowText("Stroke ")
            .SetTextRenderMode(TextRenderMode.FillStroke).ShowText("Fill+Stroke ")
            .SetTextRenderMode(TextRenderMode.Invisible).ShowText("Invisible");

        cs.AddText()
            .SetFont(Standard14Font.HelveticaOblique, 8)
            .SetGrayFill(0.45)
            .Show(DemoX, y - 20, "(Tr=3 'Invisible' is emitted but not rendered.)");
        y -= 38;

        // ===== Tr=7 — Clip path = glyph silhouettes, image shines through =====
        // Tr=7 adds the glyph silhouettes to the clipping path when ET runs;
        // we want that clip to survive past ET so the image painted below is
        // clipped to "CLIP" — and then be discarded by the outer Q. So the
        // Text block uses NoSaveRestore() (auto-flushes as BT … ET only),
        // and the whole sequence is wrapped in a Push() scope.
        Label("Tr=7");
        var bgPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../background.png"));
        var bgImage = PdfSpec.Images.PdfImage.LoadFromFilePng(bgPath);

        var clip = cs.Push();
        clip.AddText()
            .NoSaveRestore()
            .SetFont(Standard14Font.HelveticaBold, 36)
            .SetTextRenderMode(TextRenderMode.Clip)
            .Show(DemoX, y - 28, "CLIP");
        clip.DrawImage(bgImage, DemoX, y - 34, 140, 38);
        clip.Flush();
        y -= 46;

        // ===== Ts — SetTextRise (sub/superscript) =====
        Label("Ts");
        cs.AddText()
            .SetFont(Standard14Font.Helvetica, 12)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("H")
            .SetFont(Standard14Font.Helvetica, 8).SetTextRise(-3).ShowText("2")
            .SetTextRise(0)
            .SetFont(Standard14Font.Helvetica, 12).ShowText("O      E = mc")
            .SetFont(Standard14Font.Helvetica, 8).SetTextRise(5).ShowText("2");
        y -= 26;

        // ===== Colour operators valid in text state =====
        Label("rg g k / RG G K");
        cs.AddText()
            .SetLineWidth(0.5)
            .SetFont(Standard14Font.HelveticaBold, 14)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .SetTextRenderMode(TextRenderMode.Fill)
            .SetRgbFill(0.86, 0.15, 0.15).ShowText("rg ")
            .SetGrayFill(0.40).ShowText("g ")
            .SetCmykFill(0.90, 0.50, 0.00, 0.00).ShowText("k    ")
            .SetTextRenderMode(TextRenderMode.Stroke)
            .SetRgbStroke(0.13, 0.31, 0.78).ShowText("RG ")
            .SetGrayStroke(0.30).ShowText("G ")
            .SetCmykStroke(0.00, 0.70, 0.90, 0.10).ShowText("K");
        y -= 28;

        // ===== Footer =====
        cs.AddText()
            .SetFont(Standard14Font.HelveticaOblique, 8)
            .SetGrayFill(0.55)
            .Show(LabelX, 35, "Generated by PdfSpec.");

        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}
