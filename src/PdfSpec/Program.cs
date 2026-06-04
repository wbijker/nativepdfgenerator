using System.IO.Compression;
using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Fonts;

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
        var cs = page.Content;

        double y = PageSizes.A4.Height - 50;

        // Text no longer auto-wraps in q/Q — each block is wrapped explicitly
        // with cs.Save()/cs.Restore() to keep Tc/Tw/Tz/TL/Tf/Ts/Tr/colour
        // state from leaking across rows. (Tr=7 below is the deliberate
        // exception: its clip is meant to leak past ET to the image.)
        void Label(string text)
        {
            cs.Save();
            cs.AddText(new Text()
                .SetFont(cs.UseFont(Standard14Font.Courier), 9)
                .SetGrayFill(0.25)
                .Show(LabelX, y, text));
            cs.Restore();
        }

        // ===== Title =====
        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.HelveticaBold), 18)
            .Show(LabelX, y, "PDF Text Operators"));
        cs.Restore();
        y -= 20;

        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 9)
            .Show(LabelX, y, "ISO 32000-1 §9.3 (text state) and §9.4 (text objects)"));
        cs.Restore();
        y -= 26;

        // ===== BT / ET =====
        // (No SetFont call — doc default Helvetica 10 auto-emitted.)
        Label("BT/ET");
        cs.Save();
        cs.AddText(new Text()
            .Show(DemoX, y, "Every demo opens BT and closes ET."));
        cs.Restore();
        y -= 22;

        // ===== Tf — SetFont =====
        Label("Tf");
        cs.Save();
        cs.AddText(new Text()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Helvetica 10   ")
            .SetFont(cs.UseFont(Standard14Font.TimesItalic), 12)
            .ShowText("Times-Italic 12   ")
            .SetFont(cs.UseFont(Standard14Font.Courier), 10)
            .ShowText("Courier 10"));
        cs.Restore();
        y -= 22;

        // ===== Tj — ShowText =====
        Label("Tj");
        cs.Save();
        cs.AddText(new Text()
            .Show(DemoX, y, "ShowText: one literal string ending in Tj."));
        cs.Restore();
        y -= 22;

        // ===== ' — NextLineShowText (consumes TL) =====
        Label("'");
        cs.Save();
        cs.AddText(new Text()
            .SetLeading(12)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Line 1 (Tj).")
            .NextLineShowText("Line 2 (' moves down by TL then shows).")
            .NextLineShowText("Line 3 (')."));
        cs.Restore();
        y -= 44;

        // ===== " — NextLineShowText with Tw / Tc =====
        Label("\"");
        cs.Save();
        cs.AddText(new Text()
            .SetLeading(12)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Default Tw/Tc.")
            .NextLineShowText(8, 2, "\" sets Tw=8 Tc=2 then shows."));
        cs.Restore();
        y -= 32;

        // ===== TJ — ShowTextWithKerning =====
        Label("TJ");
        cs.Save();
        cs.AddText(new Text()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowTextWithKerning(
                "S", -200, "p", -200, "a", -200, "c", -200, "e", -200, "d",
                "    (negative number widens, positive tightens)"));
        cs.Restore();
        y -= 22;

        // ===== Td — MoveText =====
        Label("Td");
        cs.Save();
        cs.AddText(new Text()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("[Td origin]")
            .MoveText(70, 0).ShowText("[+70,0]")
            .MoveText(70, 0).ShowText("[+70,0]"));
        cs.Restore();
        y -= 22;

        // ===== TD — MoveTextSetLeading =====
        Label("TD");
        cs.Save();
        cs.AddText(new Text()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Top line.")
            .MoveTextSetLeading(0, -12).ShowText("TD(0,-12): moves and sets TL=12.")
            .NextLine().ShowText("T* now uses the leading TD set."));
        cs.Restore();
        y -= 44;

        // ===== Tm — SetTextMatrix (rotated) =====
        Label("Tm");
        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 11)
            .Show(PdfMatrix.Rotate(14, DemoX, y - 18), "Rotated 14° via Tm."));
        cs.Restore();
        y -= 40;

        // ===== T* — NextLine (consumes TL) =====
        Label("T*");
        cs.Save();
        cs.AddText(new Text()
            .SetLeading(11)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("Line A (T* advances by TL).")
            .NextLine().ShowText("Line B.")
            .NextLine().ShowText("Line C."));
        cs.Restore();
        y -= 42;

        // ===== TL — SetLeading =====
        Label("TL");
        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 9)
            .Show(DemoX, y, "TL sets the leading consumed by T*, TD, ' and \". (Demonstrated above.)"));
        cs.Restore();
        y -= 22;

        // ===== Tc — SetCharSpacing =====
        Label("Tc");
        cs.Save();
        cs.AddText(new Text()
            .SetCharSpacing(0)
            .Show(DemoX, y, "Tc=0  normal character spacing"));
        cs.Restore();
        y -= 14;
        cs.Save();
        cs.AddText(new Text()
            .SetCharSpacing(2)
            .Show(DemoX, y, "Tc=2  wider character spacing"));
        cs.Restore();
        y -= 22;

        // ===== Tw — SetWordSpacing =====
        Label("Tw");
        cs.Save();
        cs.AddText(new Text()
            .SetWordSpacing(0)
            .Show(DemoX, y, "Tw=0  normal word spacing between words"));
        cs.Restore();
        y -= 14;
        cs.Save();
        cs.AddText(new Text()
            .SetWordSpacing(8)
            .Show(DemoX, y, "Tw=8  wider word spacing between words"));
        cs.Restore();
        y -= 22;

        // ===== Tz — SetHorizontalScaling =====
        Label("Tz");
        cs.Save();
        cs.AddText(new Text()
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .SetHorizontalScaling(100).ShowText("Tz=100  ")
            .SetHorizontalScaling(150).ShowText("Tz=150  ")
            .SetHorizontalScaling(60).ShowText("Tz=60"));
        cs.Restore();
        y -= 22;

        // ===== Tr — SetTextRenderMode (0..3) =====
        Label("Tr 0-3");
        cs.Save();
        cs.AddText(new Text()
            .SetRgbFill(PdfColor.Rgb(0.10, 0.10, 0.10))
            .SetRgbStroke(PdfColor.Rgb(0.86, 0.15, 0.15))
            .SetLineWidth(0.6)
            .SetFont(cs.UseFont(Standard14Font.HelveticaBold), 16)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y - 4)
            .SetTextRenderMode(TextRenderMode.Fill).ShowText("Fill ")
            .SetTextRenderMode(TextRenderMode.Stroke).ShowText("Stroke ")
            .SetTextRenderMode(TextRenderMode.FillStroke).ShowText("Fill+Stroke ")
            .SetTextRenderMode(TextRenderMode.Invisible).ShowText("Invisible"));
        cs.Restore();

        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.HelveticaOblique), 8)
            .SetGrayFill(0.45)
            .Show(DemoX, y - 20, "(Tr=3 'Invisible' is emitted but not rendered.)"));
        cs.Restore();
        y -= 38;

        // ===== Tr=7 — Clip path = glyph silhouettes, image shines through =====
        // Tr=7 adds the glyph silhouettes to the clipping path when ET runs;
        // we want that clip to survive past ET so the image painted below is
        // clipped to "CLIP" — and then be discarded by the outer Q from
        // cs.Restore(). AddText emits a plain BT … ET (no inner q/Q), so
        // the clip naturally lives until the outer Restore.
        Label("Tr=7");
        var bgPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../background.png"));
        var bgImage = PdfSpec.Images.PdfImage.LoadFromFilePng(bgPath);

        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.HelveticaBold), 36)
            .SetTextRenderMode(TextRenderMode.Clip)
            .Show(DemoX, y - 28, "CLIP"));
        cs.DrawImage(bgImage, DemoX, y - 34, 140, 38);
        cs.Restore();
        y -= 46;

        // ===== Ts — SetTextRise (sub/superscript) =====
        Label("Ts");
        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 12)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .ShowText("H")
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 8).SetTextRise(-3).ShowText("2")
            .SetTextRise(0)
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 12).ShowText("O      E = mc")
            .SetFont(cs.UseFont(Standard14Font.Helvetica), 8).SetTextRise(5).ShowText("2"));
        cs.Restore();
        y -= 26;

        // ===== Colour operators valid in text state =====
        Label("rg g k / RG G K");
        cs.Save();
        cs.AddText(new Text()
            .SetLineWidth(0.5)
            .SetFont(cs.UseFont(Standard14Font.HelveticaBold), 14)
            .SetTextMatrix(1, 0, 0, 1, DemoX, y)
            .SetTextRenderMode(TextRenderMode.Fill)
            .SetRgbFill(PdfColor.Rgb(0.86, 0.15, 0.15)).ShowText("rg ")
            .SetGrayFill(0.40).ShowText("g ")
            .SetCmykFill(PdfColor.Cmyk(0.90, 0.50, 0.00, 0.00)).ShowText("k    ")
            .SetTextRenderMode(TextRenderMode.Stroke)
            .SetRgbStroke(PdfColor.Rgb(0.13, 0.31, 0.78)).ShowText("RG ")
            .SetGrayStroke(0.30).ShowText("G ")
            .SetCmykStroke(PdfColor.Cmyk(0.00, 0.70, 0.90, 0.10)).ShowText("K"));
        cs.Restore();
        y -= 28;

        // ===== Footer =====
        cs.Save();
        cs.AddText(new Text()
            .SetFont(cs.UseFont(Standard14Font.HelveticaOblique), 8)
            .SetGrayFill(0.55)
            .Show(LabelX, 35, "Generated by PdfSpec. Generate on " + DateTime.Now.ToLongTimeString()));
        cs.Restore();

        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}
